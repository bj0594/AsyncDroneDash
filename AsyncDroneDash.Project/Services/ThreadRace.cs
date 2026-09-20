using System.Collections.Concurrent;

namespace AsyncDroneDash.Project;

// Demonstrates coordinating drone threads with and without Thread.Join.
public static class ThreadRace
{
    public static void RunWithJoin(
        IReadOnlyList<DroneModel> drones,
        Action<FlightEvent> report)
    {
        ArgumentNullException.ThrowIfNull(drones);
        ArgumentNullException.ThrowIfNull(report);

        var exceptions = new ConcurrentQueue<Exception>();
        var threads = CreateThreads(drones, report, exceptions);

        foreach (var thread in threads)
        {
            thread.Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        if (exceptions.TryDequeue(out var exception))
        {
            throw exception;
        }
    }

    public static void RunWithoutJoin(
        IReadOnlyList<DroneModel> drones,
        Action<FlightEvent> report)
    {
        ArgumentNullException.ThrowIfNull(drones);
        ArgumentNullException.ThrowIfNull(report);

        foreach (var drone in drones)
        {
            var localDrone = drone;
            var thread = new Thread(() =>
            {
                try
                {
                    new DroneFlight().Run(localDrone, report);
                }
                catch (Exception exception)
                {
                    report(
                        new FlightEvent(
                            localDrone.Name,
                            FlightEventType.Faulted,
                            Exception: exception));
                }
            })
            {
                IsBackground = false,
                Name = $"Drone-{localDrone.Name}"
            };

            thread.Start();
        }
    }

    private static List<Thread> CreateThreads(
        IReadOnlyList<DroneModel> drones,
        Action<FlightEvent> report,
        ConcurrentQueue<Exception> exceptions)
    {
        var threads = new List<Thread>(drones.Count);

        foreach (var drone in drones)
        {
            var localDrone = drone;
            var thread = new Thread(() =>
            {
                try
                {
                    new DroneFlight().Run(localDrone, report);
                }
                catch (Exception exception)
                {
                    exceptions.Enqueue(exception);
                }
            })
            {
                IsBackground = false,
                Name = $"Drone-{localDrone.Name}"
            };

            threads.Add(thread);
        }

        return threads;
    }
}