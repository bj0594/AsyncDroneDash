namespace AsyncDroneDash.Project;

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
