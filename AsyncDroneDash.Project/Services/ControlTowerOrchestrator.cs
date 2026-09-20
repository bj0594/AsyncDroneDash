namespace AsyncDroneDash.Project;

public sealed class ControlTowerOrchestrator
{
    private readonly ControlTowerClient _controlTowerClient;

    public ControlTowerOrchestrator(ControlTowerClient controlTowerClient)
    {
        _controlTowerClient = controlTowerClient
            ?? throw new ArgumentNullException(nameof(controlTowerClient));
    }

    public async Task<DroneModel> LoadSequentialAsync(DroneModel drone)
    {
        ArgumentNullException.ThrowIfNull(drone);

        ValidateDrone(drone);

        var route = await _controlTowerClient
            .GetRouteAsync(drone.Name)
            .ConfigureAwait(false);

        var weather = await _controlTowerClient
            .GetWeatherAsync()
            .ConfigureAwait(false);

        var restriction = await _controlTowerClient
            .GetRestrictionsAsync()
            .ConfigureAwait(false);

        return BuildFinalConfiguration(
            drone,
            route,
            weather,
            restriction);
    }

    public async Task<DroneModel> LoadConcurrentAsync(DroneModel drone)
    {
        ArgumentNullException.ThrowIfNull(drone);

        ValidateDrone(drone);

        Task<RouteData> routeTask = _controlTowerClient.GetRouteAsync(drone.Name);
        Task<WeatherData> weatherTask = _controlTowerClient.GetWeatherAsync();
        Task<RestrictionData?> restrictionTask =
            _controlTowerClient.GetRestrictionsAsync();

        await Task.WhenAll(
                routeTask,
                weatherTask,
                restrictionTask)
            .ConfigureAwait(false);

        var route = await routeTask.ConfigureAwait(false);
        var weather = await weatherTask.ConfigureAwait(false);
        var restriction = await restrictionTask.ConfigureAwait(false);

        return BuildFinalConfiguration(
            drone,
            route,
            weather,
            restriction);
    }

    private static DroneModel BuildFinalConfiguration(
        DroneModel drone,
        RouteData route,
        WeatherData weather,
        RestrictionData? restriction)
    {
        var finalMaxCheckpoints = route.MaxCheckpoints;

        if (restriction?.MaxCheckpoints is int restrictionMaximum)
        {
            finalMaxCheckpoints = Math.Min(
                finalMaxCheckpoints,
                restrictionMaximum);
        }

        var delayAdjustmentMs = weather.Condition switch
        {
            "clear" => 0,
            "wind" => 250,
            "storm" => 500,
            _ => throw new ControlTowerException(
                ControlTowerErrorKind.InvalidResponse,
                $"Unsupported weather condition '{weather.Condition}'.")
        };

        var finalDelayLong = (long)drone.DelayMs + delayAdjustmentMs;
        if (finalDelayLong > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(drone),
                "The weather adjustment would exceed the supported DelayMs range.");
        }

        return new DroneModel
        {
            Name = drone.Name,
            MaxCheckpoints = finalMaxCheckpoints,
            DelayMs = (int)finalDelayLong
        };
    }

    private static void ValidateDrone(DroneModel drone)
    {
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
