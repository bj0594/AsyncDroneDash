namespace AsyncDroneDash.Project;

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
