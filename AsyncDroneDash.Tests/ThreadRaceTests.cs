using AsyncDroneDash.Project;
using Xunit;

namespace AsyncDroneDash.Tests;

public class ThreadRaceTests
{
    [Fact]
    public void ThreadRace_MultipleDrones_ShouldComplete()
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
        ThreadRace.RunWithJoin(drones, report);

        // Assert
        Assert.All(
            drones,
            drone =>
            {
                var droneEvents = events
                    .Where(e => e.DroneName == drone.Name)
                    .ToList();

                Assert.Contains(
                    droneEvents,
                    e => e.Type == FlightEventType.Started);

                Assert.Contains(
                    droneEvents,
                    e => e.Type == FlightEventType.Completed);
            });
    }

    [Fact]
    public void ThreadRace_WithJoin_ShouldWaitForAllDrones()
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
        using var runningGate = new ManualResetEventSlim(false);
        var startedDrones = 0;
        var allStarted = new ManualResetEventSlim(false);

        Action<FlightEvent> report = flightEvent =>
        {
            lock (events)
            {
                events.Add(flightEvent);
            }

            if (flightEvent.Type == FlightEventType.Started)
            {
                if (Interlocked.Increment(ref startedDrones) == drones.Length)
                {
                    allStarted.Set();
                }

                runningGate.Wait();
            }
        };

        // Act
        var raceTask = Task.Run(() => ThreadRace.RunWithJoin(drones, report));

        try
        {
            // Assert
            Assert.True(
                allStarted.Wait(TimeSpan.FromSeconds(1)),
                "Expected all participating drone threads to reach the running phase.");

            Assert.False(
                raceTask.IsCompleted,
                "RunWithJoin must not return while participating threads are still running.");
        }
        finally
        {
            runningGate.Set();
        }

        Assert.True(
            raceTask.Wait(TimeSpan.FromSeconds(1)),
            "ThreadRace.RunWithJoin did not complete after the gate was released.");

        List<FlightEvent> capturedEvents;
        lock (events)
        {
            capturedEvents = events.ToList();
        }

        Assert.All(
            drones,
            drone =>
            {
                var droneEvents = capturedEvents
                    .Where(e => e.DroneName == drone.Name)
                    .ToList();

                Assert.Equal(
                    FlightEventType.Completed,
                    droneEvents.Last().Type);
            });
    }

    [Fact]
    public void ThreadRace_EachDrone_ShouldReportItsOwnProgress()
    {
        // Arrange
        var drones = new[]
        {
            new DroneModel
            {
                Name = "Alpha",
                MaxCheckpoints = 1,
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
        ThreadRace.RunWithJoin(drones, report);

        // Assert
        foreach (var drone in drones)
        {
            var droneEvents = events
                .Where(e => e.DroneName == drone.Name)
                .ToList();

            Assert.Equal(
                FlightEventType.Started,
                droneEvents.First().Type);

            Assert.Equal(
                FlightEventType.Completed,
                droneEvents.Last().Type);

            var checkpoints = droneEvents
                .Where(e => e.Type == FlightEventType.CheckpointReached)
                .Select(e => e.Checkpoint!.Value)
                .ToList();

            Assert.Equal(
                Enumerable.Range(0, drone.MaxCheckpoints + 1),
                checkpoints);
        }
    }

    [Fact]
    public void ThreadRace_MultipleDrones_ShouldEnterRunningPhaseConcurrently()
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
        using var runningGate = new ManualResetEventSlim(false);
        var startedDrones = 0;
        var bothStarted = new ManualResetEventSlim(false);

        Action<FlightEvent> report = flightEvent =>
        {
            lock (events)
            {
                events.Add(flightEvent);
            }

            if (flightEvent.Type == FlightEventType.Started)
            {
                if (Interlocked.Increment(ref startedDrones) == drones.Length)
                {
                    bothStarted.Set();
                }

                runningGate.Wait();
            }
        };

        // Act
        var raceTask = Task.Run(() => ThreadRace.RunWithJoin(drones, report));

        try
        {
            // Assert
            Assert.True(
                bothStarted.Wait(TimeSpan.FromSeconds(1)),
                "Expected all participating drones to enter the running phase before progress continued.");

            Assert.False(raceTask.IsCompleted);
        }
        finally
        {
            runningGate.Set();
        }

        Assert.True(
            raceTask.Wait(TimeSpan.FromSeconds(1)),
            "ThreadRace.RunWithJoin did not complete after the gate was released.");

        Assert.Equal(drones.Length, Volatile.Read(ref startedDrones));
    }
}