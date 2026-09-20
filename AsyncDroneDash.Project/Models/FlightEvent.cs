namespace AsyncDroneDash.Project;

// Represents a lifecycle event emitted during a drone flight.
public enum FlightEventType
{
    Started,
    CheckpointReached,
    Completed,
    Faulted
}

public sealed record FlightEvent(
    string DroneName,
    FlightEventType Type,
    int? Checkpoint = null,
    Exception? Exception = null);