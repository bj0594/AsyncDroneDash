using System.Collections.Concurrent;

namespace AsyncDroneDash.Project;

public static class AsyncFlightRunner
{
    public static async Task RunAsync(
        IReadOnlyList<DroneModel> drones,
        string? failureDroneName,
        Action<FlightEvent> report)
    {
        ArgumentNullException.ThrowIfNull(drones);
        ArgumentNullException.ThrowIfNull(report);

        var faultedDrones = new ConcurrentDictionary<string, byte>();
        var flights = new List<Task>(drones.Count);

        foreach (var drone in drones)
        {
            flights.Add(Task.Run(() => RunDroneAsync(
                drone,
                failureDroneName,
                report,
                faultedDrones)));
        }

        try
        {
            await Task.WhenAll(flights).ConfigureAwait(false);
        }
        catch
        {
            if (failureDroneName is not null &&
                !faultedDrones.ContainsKey(failureDroneName))
            {
                var failingDrone = drones.FirstOrDefault(drone =>
                    string.Equals(
                        drone.Name,
                        failureDroneName,
                        StringComparison.Ordinal));

                if (failingDrone is not null)
                {
                    try
                    {
                        report(
                            new FlightEvent(
                                failingDrone.Name,
                                FlightEventType.Faulted,
                                Exception: new InvalidOperationException(
                                    "Simulated drone failure.")));
                    }
                    catch
                    {
                        // Preserve the original orchestration failure.
                    }
                }
            }

            throw;
        }
    }

    private static async Task RunDroneAsync(
        DroneModel drone,
        string? failureDroneName,
        Action<FlightEvent> report,
        ConcurrentDictionary<string, byte> faultedDrones)
    {
        try
        {
            var flight = new DroneFlight();

            Action<FlightEvent> scenarioReport = flightEvent =>
            {
                report(flightEvent);

                if (string.Equals(
                        failureDroneName,
                        drone.Name,
                        StringComparison.Ordinal) &&
                    flightEvent.Type == FlightEventType.CheckpointReached &&
                    flightEvent.Checkpoint == 1)
                {
                    throw CreateSimulatedFailure();
                }
            };

            await flight.RunAsync(drone, scenarioReport).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            faultedDrones.TryAdd(drone.Name, 0);

            try
            {
                report(
                    new FlightEvent(
                        drone.Name,
                        FlightEventType.Faulted,
                        Exception: exception));
            }
            catch
            {
                // Preserve the flight failure if reporting itself fails.
            }

            throw;
        }
    }

    private static InvalidOperationException CreateSimulatedFailure()
    {
        return new InvalidOperationException("Simulated drone failure.");
    }
}
