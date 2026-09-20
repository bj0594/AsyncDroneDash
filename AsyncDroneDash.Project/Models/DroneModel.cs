namespace AsyncDroneDash.Project;

public sealed class DroneModel
{
    public string Name { get; set; } = string.Empty;

    public int MaxCheckpoints { get; set; }

    public int DelayMs { get; set; }
}
