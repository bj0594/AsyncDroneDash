namespace AsyncDroneDash.Project;

// Error categories used by the Control Tower client and orchestrator.
public enum ControlTowerErrorKind
{
    RequestFailed,
    NotFound,
    Timeout,
    InvalidResponse
}