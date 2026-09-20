using System.Net;
using System.Text.Json;

namespace AsyncDroneDash.Project;

// Handles HTTP communication with the local Control Tower and maps responses to domain data.
public sealed class ControlTowerClient
{
    private static readonly string[] SupportedWeatherConditions =
    ["clear", "wind", "storm"];

    private readonly HttpClient _httpClient;

    public ControlTowerClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<RouteData> GetRouteAsync(string droneName)
    {
        ValidateDroneName(droneName);

        var encodedName = Uri.EscapeDataString(droneName);
        var path = $"route?drone={encodedName}";
        var json = await GetJsonAsync(path, "route", routeNotFoundIsNotFound: true)
            .ConfigureAwait(false);

        return ParseRoute(json);
    }

    public async Task<WeatherData> GetWeatherAsync()
    {
        var json = await GetJsonAsync(
                "weather",
                "weather",
                routeNotFoundIsNotFound: false)
            .ConfigureAwait(false);

        return ParseWeather(json);
    }

    public async Task<RestrictionData?> GetRestrictionsAsync()
    {
        var json = await GetJsonAsync(
                "restrictions",
                "restrictions",
                routeNotFoundIsNotFound: false)
            .ConfigureAwait(false);

        return ParseRestrictions(json);
    }

    private async Task<string> GetJsonAsync(
        string path,
        string operation,
        bool routeNotFoundIsNotFound)
    {
        Console.WriteLine($"[HTTP] START {operation}: {path}");

        try
        {
            using var response = await _httpClient.GetAsync(path).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                if (routeNotFoundIsNotFound &&
                    response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new ControlTowerException(
                        ControlTowerErrorKind.NotFound,
                        $"Control tower could not find the requested route: {path}.");
                }

                throw new ControlTowerException(
                    ControlTowerErrorKind.RequestFailed,
                    $"Control tower returned HTTP {(int)response.StatusCode} for {operation}: {response.ReasonPhrase ?? "Unknown error"}.");
            }

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Console.WriteLine($"[HTTP] COMPLETE {operation}: {path}");
            return json;
        }
        catch (ControlTowerException exception)
        {
            Console.WriteLine(
                $"[HTTP] FAILED {operation}: {exception.Kind} - {exception.Message}");
            throw;
        }
        catch (OperationCanceledException exception)
        {
            var timeoutException = new ControlTowerException(
                ControlTowerErrorKind.Timeout,
                $"The control tower request timed out while requesting {operation}.",
                exception);

            Console.WriteLine(
                $"[HTTP] FAILED {operation}: {timeoutException.Kind} - {timeoutException.Message}");
            throw timeoutException;
        }
        catch (HttpRequestException exception)
        {
            var requestException = new ControlTowerException(
                ControlTowerErrorKind.RequestFailed,
                $"The control tower request for {operation} failed.",
                exception);

            Console.WriteLine(
                $"[HTTP] FAILED {operation}: {requestException.Kind} - {requestException.Message}");
            throw requestException;
        }
        catch (Exception exception) when (exception is IOException)
        {
            var requestException = new ControlTowerException(
                ControlTowerErrorKind.RequestFailed,
                $"The control tower request for {operation} failed.",
                exception);

            Console.WriteLine(
                $"[HTTP] FAILED {operation}: {requestException.Kind} - {requestException.Message}");
            throw requestException;
        }
    }

    private static RouteData ParseRoute(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object ||
                !TryGetProperty(root, "maxCheckpoints", out var property) ||
                property.ValueKind != JsonValueKind.Number ||
                !property.TryGetInt32(out var maxCheckpoints) ||
                maxCheckpoints < 0)
            {
                throw InvalidResponse("Route response must contain a non-negative integer maxCheckpoints.");
            }

            return new RouteData(maxCheckpoints);
        }
        catch (JsonException exception)
        {
            throw InvalidResponse(
                "Route response was not valid JSON.",
                exception);
        }
        catch (ControlTowerException)
        {
            throw;
        }
        catch (Exception exception) when (exception is InvalidOperationException)
        {
            throw InvalidResponse(
                "Route response did not match the expected JSON contract.",
                exception);
        }
    }

    private static WeatherData ParseWeather(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object ||
                !TryGetProperty(root, "condition", out var property) ||
                property.ValueKind != JsonValueKind.String)
            {
                throw InvalidResponse(
                    "Weather response must contain a string condition.");
            }

            var condition = property.GetString();

            if (string.IsNullOrWhiteSpace(condition) ||
                !SupportedWeatherConditions.Contains(
                    condition,
                    StringComparer.Ordinal))
            {
                throw InvalidResponse(
                    "Weather condition must be clear, wind, or storm.");
            }

            return new WeatherData(condition);
        }
        catch (JsonException exception)
        {
            throw InvalidResponse(
                "Weather response was not valid JSON.",
                exception);
        }
        catch (ControlTowerException)
        {
            throw;
        }
        catch (Exception exception) when (exception is InvalidOperationException)
        {
            throw InvalidResponse(
                "Weather response did not match the expected JSON contract.",
                exception);
        }
    }

    private static RestrictionData? ParseRestrictions(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object ||
                !TryGetProperty(root, "maxCheckpoints", out var property))
            {
                throw InvalidResponse(
                    "Restriction response must contain maxCheckpoints.");
            }

            if (property.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (property.ValueKind != JsonValueKind.Number ||
                !property.TryGetInt32(out var maxCheckpoints) ||
                maxCheckpoints < 0)
            {
                throw InvalidResponse(
                    "Restriction maxCheckpoints must be null or a non-negative integer.");
            }

            return new RestrictionData(maxCheckpoints);
        }
        catch (JsonException exception)
        {
            throw InvalidResponse(
                "Restriction response was not valid JSON.",
                exception);
        }
        catch (ControlTowerException)
        {
            throw;
        }
        catch (Exception exception) when (exception is InvalidOperationException)
        {
            throw InvalidResponse(
                "Restriction response did not match the expected JSON contract.",
                exception);
        }
    }

    // Keeps JSON property matching case-insensitive without relying on a serializer configuration.
    private static bool TryGetProperty(
        JsonElement objectElement,
        string propertyName,
        out JsonElement value)
    {
        foreach (var property in objectElement.EnumerateObject())
        {
            if (string.Equals(
                    property.Name,
                    propertyName,
                    StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static void ValidateDroneName(string droneName)
    {
        if (string.IsNullOrWhiteSpace(droneName))
        {
            throw new ArgumentException(
                "Drone name cannot be null, empty, or whitespace.",
                nameof(droneName));
        }
    }

    private static ControlTowerException InvalidResponse(
        string message,
        Exception? innerException = null)
    {
        return new ControlTowerException(
            ControlTowerErrorKind.InvalidResponse,
            message,
            innerException);
    }
}