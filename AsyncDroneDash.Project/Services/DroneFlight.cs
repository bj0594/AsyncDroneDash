namespace AsyncDroneDash.Project;

// Executes the core drone flight sequence in blocking and asynchronous forms.
public sealed class DroneFlight
{
    public void Run(DroneModel drone, Action<FlightEvent> report)
    {
        ValidateInputs(drone, report);

        report(new FlightEvent(drone.Name, FlightEventType.Started));

        for (var checkpoint = 0; checkpoint <= drone.MaxCheckpoints; checkpoint++)
        {
            if (checkpoint > 0)
            {
                Thread.Sleep(drone.DelayMs);
            }

            report(
                new FlightEvent(
                    drone.Name,
                    FlightEventType.CheckpointReached,
                    checkpoint));
        }

        report(new FlightEvent(drone.Name, FlightEventType.Completed));
    }

    public async Task RunAsync(
        DroneModel drone,
        Action<FlightEvent> report)
    {
        ValidateInputs(drone, report);

        report(new FlightEvent(drone.Name, FlightEventType.Started));

        for (var checkpoint = 0; checkpoint <= drone.MaxCheckpoints; checkpoint++)
        {
            if (checkpoint > 0)
            {
                await Task.Delay(drone.DelayMs).ConfigureAwait(false);
            }

            report(
                new FlightEvent(
                    drone.Name,
                    FlightEventType.CheckpointReached,
                    checkpoint));
        }

        report(new FlightEvent(drone.Name, FlightEventType.Completed));
    }

    private static void ValidateInputs(
        DroneModel drone,
        Action<FlightEvent> report)
    {
        ArgumentNullException.ThrowIfNull(drone);
        ArgumentNullException.ThrowIfNull(report);

        if (string.IsNullOrWhiteSpace(drone.Name))
        {
            throw new ArgumentException(
                "Drone name cannot be null, empty, or whitespace.",
                nameof(drone));
        }

        if (drone.MaxCheckpoints < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(drone),
                drone.MaxCheckpoints,
                "MaxCheckpoints cannot be negative.");
        }

        if (drone.DelayMs < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(drone),
                drone.DelayMs,
                "DelayMs cannot be negative.");
        }
    }
}