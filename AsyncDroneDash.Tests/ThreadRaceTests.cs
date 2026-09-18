using AsyncDroneDash.Project;
using Xunit;

namespace AsyncDroneDash.Tests;

public class ThreadRaceTests
{
    [Fact]
    public void ThreadRace_MultipleDrones_ShouldUseSeparateThreadsAndComplete()
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
        var race = new ThreadRace(drones, report);
        race.Run();

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
        Action<FlightEvent> report = events.Add;

        // Act
        var race = new ThreadRace(drones, report);
        race.Run();

        // Assert
        Assert.All(
            drones,
            drone =>
            {
                var droneEvents = events
                    .Where(e => e.DroneName == drone.Name)
                    .ToList();

                Assert.NotEmpty(droneEvents);
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
        Action<FlightEvent> report = events.Add;

        // Act
        var race = new ThreadRace(drones, report);
        race.Run();

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
                .Select(e => e.Checkpoint)
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

        using var runningGate = new ManualResetEventSlim(false);

        var runningDrones = 0;

        var race = new ThreadRace(
            drones,
            report: _ => { },
            onDroneRunning: () =>
            {
                if (Interlocked.Increment(ref runningDrones) >= 2)
                {
                    runningGate.Set();
                }

                runningGate.Wait();
            });

        // Act
        var raceTask = Task.Run(() => race.Run());

        // Assert
        Assert.True(
            runningGate.Wait(TimeSpan.FromSeconds(1)),
            "Expected at least two drone threads to enter the running phase.");

        raceTask.Wait();
        Assert.Equal(2, Volatile.Read(ref runningDrones));
    }
}