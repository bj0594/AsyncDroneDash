namespace AsyncDroneDash.Project;

// Coordinates thread-based drone flights through TaskCompletionSource and Task.WhenAll.
public static class TaskFlightRunner
{
    public static async Task RunAsync(
        IReadOnlyList<DroneModel> drones,
        string? failureDroneName,
        Action<FlightEvent> report)
    {
        ArgumentNullException.ThrowIfNull(drones);
        ArgumentNullException.ThrowIfNull(report);

        var completionTasks = new List<Task>(drones.Count);

        foreach (var drone in drones)
        {
            // Exposes completion and failure from the thread as a Task for orchestration.
            var completion = new TaskCompletionSource<object?>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            completionTasks.Add(completion.Task);

            _ = Task.Run(() => RunDrone(
                drone,
                failureDroneName,
                report,
                completion));
        }

        await Task.WhenAll(completionTasks).ConfigureAwait(false);
    }

    private static void RunDrone(
        DroneModel drone,
        string? failureDroneName,
        Action<FlightEvent> report,
        TaskCompletionSource<object?> completion)
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

            flight.Run(drone, scenarioReport);
            completion.TrySetResult(null);
        }
        catch (Exception exception)
        {
            ReportFaultSafely(
                report,
                new FlightEvent(
                    drone.Name,
                    FlightEventType.Faulted,
                    Exception: exception));

            completion.TrySetException(exception);
        }
    }

    private static void ReportFaultSafely(
        Action<FlightEvent> report,
        FlightEvent faultedEvent)
    {
        try
        {
            report(faultedEvent);
        }
        catch
        {
            // The operation must still fault through its Task even if
            // the observation callback itself fails while reporting Faulted.
        }
    }

    private static InvalidOperationException CreateSimulatedFailure()
    {
        return new InvalidOperationException("Simulated drone failure.");
    }
}