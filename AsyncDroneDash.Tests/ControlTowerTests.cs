using AsyncDroneDash.Project;
using System.Net;
using System.Net.Http;
using System.Text;
using Xunit;

namespace AsyncDroneDash.Tests;

public class ControlTowerTests
{
    [Theory]
    [InlineData("Alpha", 3)]
    [InlineData("Beta", 5)]
    [InlineData("Gamma", 2)]
    public async Task ControlTower_ShouldReturnRouteData(
        string droneName,
        int expectedMaxCheckpoints)
    {
        // Arrange
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal(
                $"/route?drone={droneName}",
                request.RequestUri!.PathAndQuery);

            return JsonResponse(
                $$"""{"maxCheckpoints":{{expectedMaxCheckpoints}}}""");
        });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8080/")
        };

        var client = new ControlTowerClient(httpClient);

        // Act
        var result = await client.GetRouteAsync(droneName);

        // Assert
        Assert.Equal(expectedMaxCheckpoints, result.MaxCheckpoints);
    }

    [Theory]
    [InlineData("clear")]
    [InlineData("wind")]
    [InlineData("storm")]
    public async Task ControlTower_ShouldReturnWeatherData(
        string expectedCondition)
    {
        // Arrange
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal(
                "/weather",
                request.RequestUri!.PathAndQuery);

            return JsonResponse(
                $$"""{"condition":"{{expectedCondition}}"}""");
        });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8080/")
        };

        var client = new ControlTowerClient(httpClient);

        // Act
        var result = await client.GetWeatherAsync();

        // Assert
        Assert.Equal(expectedCondition, result.Condition);
    }

    [Fact]
    public async Task ControlTower_ShouldReturnRestrictions()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal(
                "/restrictions",
                request.RequestUri!.PathAndQuery);

            return JsonResponse("""{"maxCheckpoints":2}""");
        });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8080/")
        };

        var client = new ControlTowerClient(httpClient);

        // Act
        var result = await client.GetRestrictionsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result!.MaxCheckpoints);
    }

    [Fact]
    public async Task ControlTower_ShouldReturnNullRestriction_WhenNoRestrictionIsActive()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(_ =>
            JsonResponse("null"));

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8080/")
        };

        var client = new ControlTowerClient(httpClient);

        // Act
        var result = await client.GetRestrictionsAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ControlTower_RestrictionEqualToRouteMaximum_ShouldPreserveRouteMaximum()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/route" =>
                    JsonResponse("""{"maxCheckpoints":3}"""),

                "/weather" =>
                    JsonResponse("""{"condition":"clear"}"""),

                "/restrictions" =>
                    JsonResponse("""{"maxCheckpoints":3}"""),

                _ =>
                    new HttpResponseMessage(HttpStatusCode.NotFound)
            };
        });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8080/")
        };

        var client = new ControlTowerClient(httpClient);
        var orchestrator = new ControlTowerOrchestrator(client);

        var drone = new DroneModel
        {
            Name = "Alpha",
            MaxCheckpoints = 10,
            DelayMs = 100
        };

        // Act
        var result = await orchestrator.LoadSequentialAsync(drone);

        // Assert
        Assert.Equal(3, result.MaxCheckpoints);
    }

    [Fact]
    public async Task ControlTower_Data_ShouldProduceFinalSimulationConfiguration()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/route" =>
                    JsonResponse("""{"maxCheckpoints":3}"""),

                "/weather" =>
                    JsonResponse("""{"condition":"storm"}"""),

                "/restrictions" =>
                    JsonResponse("""{"maxCheckpoints":2}"""),

                _ =>
                    new HttpResponseMessage(HttpStatusCode.NotFound)
            };
        });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8080/")
        };

        var client = new ControlTowerClient(httpClient);
        var orchestrator = new ControlTowerOrchestrator(client);

        var drone = new DroneModel
        {
            Name = "Alpha",
            MaxCheckpoints = 10,
            DelayMs = 100
        };

        // Act
        var result = await orchestrator.LoadSequentialAsync(drone);

        // Assert
        Assert.Equal("Alpha", result.Name);
        Assert.Equal(2, result.MaxCheckpoints);
        Assert.Equal(600, result.DelayMs);
    }

    [Fact]
    public async Task ControlTower_ConnectionFailure_ShouldProduceRequestFailed()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(
            (_, _) =>
                throw new HttpRequestException("Simulated connection failure."));

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8080/")
        };

        var client = new ControlTowerClient(httpClient);

        // Act
        var exception = await Assert.ThrowsAsync<ControlTowerException>(
            () => client.GetWeatherAsync());

        // Assert
        Assert.Equal(
            ControlTowerErrorKind.RequestFailed,
            exception.Kind);
    }

    [Fact]
    public async Task ControlTower_NonSuccessResponse_ShouldProduceRequestFailed()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8080/")
        };

        var client = new ControlTowerClient(httpClient);

        // Act
        var exception = await Assert.ThrowsAsync<ControlTowerException>(
            () => client.GetWeatherAsync());

        // Assert
        Assert.Equal(
            ControlTowerErrorKind.RequestFailed,
            exception.Kind);
    }

    [Fact]
    public async Task ControlTower_Timeout_ShouldProduceTimeout()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(
            async (_, cancellationToken) =>
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(5),
                    cancellationToken);

                return JsonResponse(
                    """{"condition":"clear"}""");
            });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8080/"),
            Timeout = TimeSpan.FromMilliseconds(50)
        };

        var client = new ControlTowerClient(httpClient);

        // Act
        var exception = await Assert.ThrowsAsync<ControlTowerException>(
            () => client.GetWeatherAsync());

        // Assert
        Assert.Equal(
            ControlTowerErrorKind.Timeout,
            exception.Kind);
    }

    [Theory]
    [InlineData("")]
    [InlineData("""{"condition":}""")]
    [InlineData("""{"wrongProperty":"clear"}""")]
    [InlineData("""{"condition":"hurricane"}""")]
    public async Task ControlTower_InvalidWeatherResponse_ShouldProduceInvalidResponse(
        string responseBody)
    {
        // Arrange
        var handler = new StubHttpMessageHandler(_ =>
            JsonResponse(responseBody));

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8080/")
        };

        var client = new ControlTowerClient(httpClient);

        // Act
        var exception = await Assert.ThrowsAsync<ControlTowerException>(
            () => client.GetWeatherAsync());

        // Assert
        Assert.Equal(
            ControlTowerErrorKind.InvalidResponse,
            exception.Kind);
    }

    [Theory]
    [InlineData("""{}""")]
    [InlineData("""{"maxCheckpoints":-1}""")]
    public async Task ControlTower_InvalidRouteResponse_ShouldProduceInvalidResponse(
        string responseBody)
    {
        // Arrange
        var handler = new StubHttpMessageHandler(_ =>
            JsonResponse(responseBody));

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8080/")
        };

        var client = new ControlTowerClient(httpClient);

        // Act
        var exception = await Assert.ThrowsAsync<ControlTowerException>(
            () => client.GetRouteAsync("Alpha"));

        // Assert
        Assert.Equal(
            ControlTowerErrorKind.InvalidResponse,
            exception.Kind);
    }

    [Theory]
    [InlineData("""{}""")]
    [InlineData("""{"maxCheckpoints":-1}""")]
    public async Task ControlTower_InvalidRestrictionResponse_ShouldProduceInvalidResponse(
        string responseBody)
    {
        // Arrange
        var handler = new StubHttpMessageHandler(_ =>
            JsonResponse(responseBody));

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8080/")
        };

        var client = new ControlTowerClient(httpClient);

        // Act
        var exception = await Assert.ThrowsAsync<ControlTowerException>(
            () => client.GetRestrictionsAsync());

        // Assert
        Assert.Equal(
            ControlTowerErrorKind.InvalidResponse,
            exception.Kind);
    }

    [Fact]
    public async Task ControlTower_UnknownDrone_ShouldProduceNotFound()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Contains(
                "Unknown",
                request.RequestUri!.Query);

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8080/")
        };

        var client = new ControlTowerClient(httpClient);

        // Act
        var exception = await Assert.ThrowsAsync<ControlTowerException>(
            () => client.GetRouteAsync("Unknown"));

        // Assert
        Assert.Equal(
            ControlTowerErrorKind.NotFound,
            exception.Kind);
    }

    [Fact]
    public async Task ControlTower_SequentialAndConcurrentResults_ShouldMatch()
    {
        // Arrange
        var sequentialHandler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/route" =>
                    JsonResponse("""{"maxCheckpoints":3}"""),

                "/weather" =>
                    JsonResponse("""{"condition":"storm"}"""),

                "/restrictions" =>
                    JsonResponse("""{"maxCheckpoints":2}"""),

                _ =>
                    new HttpResponseMessage(HttpStatusCode.NotFound)
            };
        });

        using var sequentialHttpClient = new HttpClient(sequentialHandler)
        {
            BaseAddress = new Uri("http://localhost:8080/")
        };

        var sequentialClient = new ControlTowerClient(sequentialHttpClient);
        var sequentialOrchestrator = new ControlTowerOrchestrator(sequentialClient);

        var drone = new DroneModel
        {
            Name = "Alpha",
            MaxCheckpoints = 10,
            DelayMs = 100
        };

        // Act
        var sequential =
            await sequentialOrchestrator.LoadSequentialAsync(drone);

        var routeStarted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var weatherStarted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var restrictionsStarted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseRequests = new ManualResetEventSlim(false);

        var concurrentHandler = new StubHttpMessageHandler(
            async (request, _) =>
            {
                switch (request.RequestUri!.AbsolutePath)
                {
                    case "/route":
                        routeStarted.TrySetResult(true);
                        break;
                    case "/weather":
                        weatherStarted.TrySetResult(true);
                        break;
                    case "/restrictions":
                        restrictionsStarted.TrySetResult(true);
                        break;
                }

                releaseRequests.Wait();

                return request.RequestUri.AbsolutePath switch
                {
                    "/route" =>
                        JsonResponse("""{"maxCheckpoints":3}"""),

                    "/weather" =>
                        JsonResponse("""{"condition":"storm"}"""),

                    "/restrictions" =>
                        JsonResponse("""{"maxCheckpoints":2}"""),

                    _ =>
                        new HttpResponseMessage(HttpStatusCode.NotFound)
                };
            });

        using var concurrentHttpClient = new HttpClient(concurrentHandler)
        {
            BaseAddress = new Uri("http://localhost:8080/")
        };

        var concurrentClient = new ControlTowerClient(concurrentHttpClient);
        var concurrentOrchestrator = new ControlTowerOrchestrator(concurrentClient);

        var concurrentTask = concurrentOrchestrator.LoadConcurrentAsync(drone);

        try
        {
            await Task.WhenAll(
                routeStarted.Task,
                weatherStarted.Task,
                restrictionsStarted.Task)
                .WaitAsync(TimeSpan.FromSeconds(1));

            // Assert
            Assert.False(concurrentTask.IsCompleted);
        }
        finally
        {
            releaseRequests.Set();
        }

        var concurrent = await concurrentTask;

        Assert.Equal(
            sequential.Name,
            concurrent.Name);

        Assert.Equal(
            sequential.MaxCheckpoints,
            concurrent.MaxCheckpoints);

        Assert.Equal(
            sequential.DelayMs,
            concurrent.DelayMs);
    }

    private static HttpResponseMessage JsonResponse(string content)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                content,
                Encoding.UTF8,
                "application/json")
        };
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<
            HttpRequestMessage,
            CancellationToken,
            Task<HttpResponseMessage>> _handler;

        public StubHttpMessageHandler(
            Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = (request, _) =>
                Task.FromResult(handler(request));
        }

        public StubHttpMessageHandler(
            Func<
                HttpRequestMessage,
                CancellationToken,
                Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }
}