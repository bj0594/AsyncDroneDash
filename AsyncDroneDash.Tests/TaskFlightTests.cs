using Xunit;

namespace AsyncDroneDash.Tests;

public class TaskFlightTests
{
    [Fact]
    public async Task TaskFlight_SuccessfulDrone_ShouldCompleteTask()
    {
        // Arrange
        var drone = new DroneModel
        {
            Name = "Alpha",
            MaxCheckpoints = 3,
            DelayMs = 0
        };

        var events = new List<FlightEvent>();
        Action<FlightEvent> report = events.Add;

        // Act
        Task task = TaskFlight.RunAsync(drone, report);

        // Assert
        await task;

        Assert.True(task.IsCompletedSuccessfully);

        Assert.Equal(
            FlightEventType.Completed,
            events.Last().Type);
    }

    [Fact]
    public async Task TaskFlight_WhenAll_ShouldWaitForAllDrones()
    {
        // Arrange
        var drones = new[]
        {
            new DroneModel
            {
                Name = "Alpha",
                MaxCheckpoints = 3,
                DelayMs = 0
            },
            new DroneModel
            {
                Name = "Beta",
                MaxCheckpoints = 3,
                DelayMs = 0
            }
        };

        var events = new List<FlightEvent>();
        Action<FlightEvent> report = events.Add;

        var tasks = drones
            .Select(drone => TaskFlight.RunAsync(drone, report))
            .ToArray();

        // Act
        await Task.WhenAll(tasks);

        // Assert
        Assert.All(tasks, task =>
            Assert.True(task.IsCompletedSuccessfully));

        Assert.All(drones, drone =>
        {
            var droneEvents = events
                .Where(e => e.DroneName == drone.Name)
                .ToList();

            Assert.Equal(
                FlightEventType.Completed,
                droneEvents.Last().Type);
        });
    }

    [Fact]
    public async Task TaskFlight_SimulatedFailure_ShouldFaultAfterCheckpointOne()
    {
        // Arrange
        var drone = new DroneModel
        {
            Name = "Alpha",
            MaxCheckpoints = 3,
            DelayMs = 0
        };

        var events = new List<FlightEvent>();
        Action<FlightEvent> report = events.Add;

        // Act
        Task task = TaskFlight.RunAsync(
            drone,
            report,
            simulateFailure: true);

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await task);

        Assert.Equal(
            "Simulated drone failure.",
            exception.Message);

        var droneEvents = events
            .Where(e => e.DroneName == drone.Name)
            .ToList();

        var checkpointOneIndex = droneEvents.FindIndex(
            e =>
                e.Type == FlightEventType.CheckpointReached &&
                e.Checkpoint == 1);

        var faultedIndex = droneEvents.FindIndex(
            e => e.Type == FlightEventType.Faulted);

        Assert.True(checkpointOneIndex >= 0);
        Assert.True(faultedIndex > checkpointOneIndex);

        var faultedEvent = droneEvents[faultedIndex];

        Assert.Equal(
            FlightEventType.Faulted,
            faultedEvent.Type);

        Assert.Equal(
            "Alpha",
            faultedEvent.DroneName);
    }

    [Fact]
    public async Task TaskFlight_Failure_ShouldReachOrchestration()
    {
        // Arrange
        var drones = new[]
        {
            new DroneModel
            {
                Name = "Alpha",
                MaxCheckpoints = 3,
                DelayMs = 0
            },
            new DroneModel
            {
                Name = "Beta",
                MaxCheckpoints = 3,
                DelayMs = 0
            }
        };

        var events = new List<FlightEvent>();
        Action<FlightEvent> report = events.Add;

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () =>
            {
                await TaskFlightOrchestrator.RunAsync(
                    drones,
                    report,
                    failureDroneName: "Alpha");
            });

        // Assert
        Assert.Equal(
            "Simulated drone failure.",
            exception.Message);
    }

    [Fact]
    public async Task TaskFlight_FaultedTask_ShouldExposeExpectedException()
    {
        // Arrange
        var drone = new DroneModel
        {
            Name = "Alpha",
            MaxCheckpoints = 3,
            DelayMs = 0
        };

        var events = new List<FlightEvent>();
        Action<FlightEvent> report = events.Add;

        // Act
        Task task = TaskFlight.RunAsync(
            drone,
            report,
            simulateFailure: true);

        try
        {
            await task;
        }
        catch (InvalidOperationException)
        {
            // Expected failure.
        }

        // Assert
        Assert.True(task.IsFaulted);
        Assert.NotNull(task.Exception);

        Assert.Contains(
            task.Exception!.InnerExceptions,
            exception =>
                exception is InvalidOperationException &&
                exception.Message == "Simulated drone failure.");
    }

    [Fact]
    public async Task TaskFlight_WhenAll_WithFailure_ShouldRepresentCombinedFailure()
    {
        // Arrange
        var drones = new[]
        {
            new DroneModel
            {
                Name = "Alpha",
                MaxCheckpoints = 3,
                DelayMs = 0
            },
            new DroneModel
            {
                Name = "Beta",
                MaxCheckpoints = 3,
                DelayMs = 0
            }
        };

        var events = new List<FlightEvent>();
        Action<FlightEvent> report = events.Add;

        // Act
        Task combinedTask = TaskFlightOrchestrator.RunAsync(
            drones,
            report,
            failureDroneName: "Alpha");

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await combinedTask);

        var alphaEvents = events
            .Where(e => e.DroneName == "Alpha")
            .ToList();

        var betaEvents = events
            .Where(e => e.DroneName == "Beta")
            .ToList();

        Assert.Contains(
            alphaEvents,
            e =>
                e.Type == FlightEventType.Faulted &&
                e.DroneName == "Alpha");

        Assert.Equal(
            FlightEventType.Completed,
            betaEvents.Last().Type);

        Assert.True(combinedTask.IsFaulted);
        Assert.False(combinedTask.Status == TaskStatus.RanToCompletion);
    }
}