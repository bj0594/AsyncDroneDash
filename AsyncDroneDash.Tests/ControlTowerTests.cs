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
    [InlineData("""{"maxCheckpoints":-1}""")]
    public async Task ControlTower_InvalidResponse_ShouldProduceInvalidResponse(
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
        var sequential =
            await orchestrator.LoadSequentialAsync(drone);

        var concurrent =
            await orchestrator.LoadConcurrentAsync(drone);

        // Assert
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