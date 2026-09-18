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
        Action<FlightEvent> report = events.Add;

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
        Action<FlightEvent> report = events.Add;

        // Act
        await AsyncFlightRunner.RunAsync(
            new[] { alpha, beta },
            failureDroneName: null,
            report);

        // Assert
        var alphaEvents = events
            .Where(e => e.DroneName == "Alpha")
            .ToList();

        var betaEvents = events
            .Where(e => e.DroneName == "Beta")
            .ToList();

        Assert.NotEmpty(alphaEvents);
        Assert.NotEmpty(betaEvents);

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

        Assert.Contains(
            events,
            e => e.DroneName == "Alpha" &&
                 e.Type == FlightEventType.CheckpointReached);

        Assert.Contains(
            events,
            e => e.DroneName == "Beta" &&
                 e.Type == FlightEventType.CheckpointReached);
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
        Action<FlightEvent> report = events.Add;

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
        Action<FlightEvent> report = events.Add;

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