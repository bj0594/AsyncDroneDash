using AsyncDroneDash.Project;
using Xunit;

namespace AsyncDroneDash.Tests;

public class AsyncFlightTests
{
    [Fact]
    public async Task AsyncFlight_ValidDrone_ShouldComplete()
    {
        // Arrange
        var drone = new DroneModel
        {
            Name = "Alpha",
            MaxCheckpoints = 1,
            DelayMs = 0
        };

        var events = new List<FlightEvent>();
        Action<FlightEvent> report = flightEvent =>
        {
            lock (events)
            {
                events.Add(flightEvent);
            }
        };

        // Act
        await AsyncFlightRunner.RunAsync(
            new[] { drone },
            failureDroneName: null,
            report);

        // Assert
        Assert.NotEmpty(events);
        Assert.Equal(FlightEventType.Started, events.First().Type);
        Assert.Equal(FlightEventType.Completed, events.Last().Type);
        Assert.All(events, flightEvent =>
            Assert.Equal("Alpha", flightEvent.DroneName));
    }

    [Fact]
    public async Task AsyncFlight_MultipleDrones_ShouldMakeOverlappingProgress()
    {
        // Arrange
        var alpha = new DroneModel
        {
            Name = "Alpha",
            MaxCheckpoints = 2,
            DelayMs = 0
        };

        var beta = new DroneModel
        {
            Name = "Beta",
            MaxCheckpoints = 2,
            DelayMs = 0
        };

        var events = new List<FlightEvent>();
        var alphaStarted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var betaStarted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var allowProgress = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        Action<FlightEvent> report = flightEvent =>
        {
            lock (events)
            {
                events.Add(flightEvent);
            }

            if (flightEvent.DroneName == "Alpha" &&
                flightEvent.Type == FlightEventType.Started)
            {
                alphaStarted.TrySetResult(true);
            }

            if (flightEvent.DroneName == "Beta" &&
                flightEvent.Type == FlightEventType.Started)
            {
                betaStarted.TrySetResult(true);
            }

            if (flightEvent.Type == FlightEventType.Started)
            {
                _ = WaitForBothDronesAsync(
                    alphaStarted.Task,
                    betaStarted.Task,
                    allowProgress);
            }
        };

        async Task WaitForBothDronesAsync(
            Task alphaTask,
            Task betaTask,
            TaskCompletionSource<bool> gate)
        {
            await Task.WhenAll(alphaTask, betaTask);
            gate.TrySetResult(true);
        }

        // Act
        var task = AsyncFlightRunner.RunAsync(
            new[] { alpha, beta },
            failureDroneName: null,
            report);

        await allowProgress.Task;
        await task;

        // Assert
        List<FlightEvent> capturedEvents;
        lock (events)
        {
            capturedEvents = events.ToList();
        }

        var alphaEvents = capturedEvents
            .Where(e => e.DroneName == "Alpha")
            .ToList();

        var betaEvents = capturedEvents
            .Where(e => e.DroneName == "Beta")
            .ToList();

        Assert.Equal(
            new[]
            {
                FlightEventType.Started,
                FlightEventType.CheckpointReached,
                FlightEventType.CheckpointReached,
                FlightEventType.Completed
            },
            alphaEvents.Select(e => e.Type));

        Assert.Equal(
            new[]
            {
                FlightEventType.Started,
                FlightEventType.CheckpointReached,
                FlightEventType.CheckpointReached,
                FlightEventType.Completed
            },
            betaEvents.Select(e => e.Type));

        var firstCheckpointIndex = capturedEvents.FindIndex(
            e => e.Type == FlightEventType.CheckpointReached);

        Assert.True(firstCheckpointIndex >= 0);

        Assert.Contains(
            capturedEvents.Take(firstCheckpointIndex),
            e => e.DroneName == "Alpha" &&
                 e.Type == FlightEventType.Started);

        Assert.Contains(
            capturedEvents.Take(firstCheckpointIndex),
            e => e.DroneName == "Beta" &&
                 e.Type == FlightEventType.Started);
    }

    [Fact]
    public async Task AsyncFlight_ShouldAwaitTaskWhenAll()
    {
        // Arrange
        var alpha = new DroneModel
        {
            Name = "Alpha",
            MaxCheckpoints = 1,
            DelayMs = 0
        };

        var beta = new DroneModel
        {
            Name = "Beta",
            MaxCheckpoints = 1,
            DelayMs = 0
        };

        var events = new List<FlightEvent>();
        Action<FlightEvent> report = flightEvent =>
        {
            lock (events)
            {
                events.Add(flightEvent);
            }
        };

        // Act
        var task = AsyncFlightRunner.RunAsync(
            new[] { alpha, beta },
            failureDroneName: null,
            report);

        await task;

        // Assert
        Assert.True(task.IsCompletedSuccessfully);

        Assert.Contains(
            events,
            e => e.DroneName == "Alpha" &&
                 e.Type == FlightEventType.Completed);

        Assert.Contains(
            events,
            e => e.DroneName == "Beta" &&
                 e.Type == FlightEventType.Completed);
    }

    [Fact]
    public async Task AsyncFlight_Failure_ShouldReachOrchestrationBoundary()
    {
        // Arrange
        var alpha = new DroneModel
        {
            Name = "Alpha",
            MaxCheckpoints = 2,
            DelayMs = 0
        };

        var beta = new DroneModel
        {
            Name = "Beta",
            MaxCheckpoints = 2,
            DelayMs = 0
        };

        var events = new List<FlightEvent>();
        Action<FlightEvent> report = flightEvent =>
        {
            lock (events)
            {
                events.Add(flightEvent);
            }
        };

        // Act
        var act = () => AsyncFlightRunner.RunAsync(
            new[] { alpha, beta },
            failureDroneName: "Alpha",
            report);

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);

        Assert.Equal(
            "Simulated drone failure.",
            exception.Message);

        var failureEvent = Assert.Single(
            events.Where(e =>
                e.DroneName == "Alpha" &&
                e.Type == FlightEventType.Faulted));

        Assert.Equal("Alpha", failureEvent.DroneName);

        Assert.Contains(
            events,
            e => e.DroneName == "Alpha" &&
                 e.Type == FlightEventType.CheckpointReached &&
                 e.Checkpoint == 1);

        Assert.DoesNotContain(
            events,
            e => e.DroneName == "Alpha" &&
                 e.Type == FlightEventType.Completed);
    }
}