using AsyncDroneDash.Project;
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
        Action<FlightEvent> report = flightEvent =>
        {
            lock (events)
            {
                events.Add(flightEvent);
            }
        };

        // Act
        Task task = TaskFlightRunner.RunAsync(
            new[] { drone },
            failureDroneName: null,
            report);

        // Assert
        await task;

        Assert.True(task.IsCompletedSuccessfully);

        FlightEvent lastEvent;
        lock (events)
        {
            lastEvent = events.Last();
        }

        Assert.Equal(
            FlightEventType.Completed,
            lastEvent.Type);
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
        using var releaseCompleted = new ManualResetEventSlim(false);
        var betaCompleted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        Action<FlightEvent> report = flightEvent =>
        {
            lock (events)
            {
                events.Add(flightEvent);
            }

            if (flightEvent.DroneName == "Beta" &&
                flightEvent.Type == FlightEventType.Completed)
            {
                betaCompleted.TrySetResult(true);
                releaseCompleted.Wait();
            }
        };

        // Act
        Task combinedTask = TaskFlightRunner.RunAsync(
            drones,
            failureDroneName: null,
            report);

        try
        {
            await betaCompleted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            // Assert
            Assert.False(
                combinedTask.IsCompleted,
                "Combined task must not complete while a participating drone is still finishing.");
        }
        finally
        {
            releaseCompleted.Set();
        }

        await combinedTask.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(combinedTask.IsCompletedSuccessfully);

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
        Action<FlightEvent> report = flightEvent =>
        {
            lock (events)
            {
                events.Add(flightEvent);
            }
        };

        // Act
        Task task = TaskFlightRunner.RunAsync(
            new[] { drone },
            failureDroneName: drone.Name,
            report);

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await task);

        Assert.Equal(
            "Simulated drone failure.",
            exception.Message);

        List<FlightEvent> droneEvents;
        lock (events)
        {
            droneEvents = events
                .Where(e => e.DroneName == drone.Name)
                .ToList();
        }

        var checkpointOneIndex = droneEvents.FindIndex(
            e =>
                e.Type == FlightEventType.CheckpointReached &&
                e.Checkpoint == 1);

        var faultedIndex = droneEvents.FindIndex(
            e => e.Type == FlightEventType.Faulted);

        Assert.True(checkpointOneIndex >= 0);
        Assert.Equal(checkpointOneIndex + 1, faultedIndex);

        var faultedEvent = droneEvents[faultedIndex];

        Assert.Equal(
            FlightEventType.Faulted,
            faultedEvent.Type);

        Assert.Equal(
            "Alpha",
            faultedEvent.DroneName);

        var faultException = Assert.IsType<InvalidOperationException>(
            faultedEvent.Exception);

        Assert.Equal(
            "Simulated drone failure.",
            faultException.Message);
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
        Action<FlightEvent> report = flightEvent =>
        {
            lock (events)
            {
                events.Add(flightEvent);
            }
        };

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () =>
            {
                await TaskFlightRunner.RunAsync(
                    drones,
                    failureDroneName: "Alpha",
                    report);
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
        Action<FlightEvent> report = flightEvent =>
        {
            lock (events)
            {
                events.Add(flightEvent);
            }
        };

        // Act
        Task task = TaskFlightRunner.RunAsync(
            new[] { drone },
            failureDroneName: drone.Name,
            report);

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
        Action<FlightEvent> report = flightEvent =>
        {
            lock (events)
            {
                events.Add(flightEvent);
            }
        };

        // Act
        Task combinedTask = TaskFlightRunner.RunAsync(
            drones,
            failureDroneName: "Alpha",
            report);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await combinedTask);

        List<FlightEvent> alphaEvents;
        List<FlightEvent> betaEvents;
        lock (events)
        {
            alphaEvents = events
                .Where(e => e.DroneName == "Alpha")
                .ToList();

            betaEvents = events
                .Where(e => e.DroneName == "Beta")
                .ToList();
        }

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