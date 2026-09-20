namespace AsyncDroneDash.Project;

// Represents a typed error from the Control Tower communication layer.
public sealed class ControlTowerException : Exception
{
    public ControlTowerException(
        ControlTowerErrorKind kind,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Kind = kind;
    }

    public ControlTowerErrorKind Kind { get; }
}