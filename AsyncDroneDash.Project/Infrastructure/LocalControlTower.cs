using System.Net;
using System.Text;

namespace AsyncDroneDash.Project;

public sealed class LocalControlTower : IAsyncDisposable
{
    public const string Prefix = "http://localhost:8080/";

    private static readonly IReadOnlyDictionary<string, int> RouteMaximums =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["Alpha"] = 3,
            ["Beta"] = 5,
            ["Gamma"] = 2
        };

    private readonly HttpListener _listener = new();
    private readonly object _handlerLock = new();
    private readonly List<Task> _handlerTasks = [];
    private Task? _listenTask;
    private bool _started;

    public void Start()
    {
        if (_started)
        {
            throw new InvalidOperationException("The control tower is already running.");
        }

        _listener.Prefixes.Add(Prefix);
        _listener.Start();
        _started = true;
        _listenTask = ListenLoopAsync();

        Console.WriteLine($"[Tower] Listening on {Prefix}");
    }

    public async Task StopAsync()
    {
        if (!_started)
        {
            return;
        }

        _listener.Stop();

        if (_listenTask is not null)
        {
            try
            {
                await _listenTask.ConfigureAwait(false);
            }
            catch (HttpListenerException)
            {
                // Expected when Stop interrupts GetContextAsync.
            }
            catch (ObjectDisposedException)
            {
                // Expected if disposal happens during shutdown.
            }
        }

        Task[] handlers;
        lock (_handlerLock)
        {
            handlers = _handlerTasks.ToArray();
            _handlerTasks.Clear();
        }

        if (handlers.Length > 0)
        {
            await Task.WhenAll(handlers).ConfigureAwait(false);
        }

        _started = false;
        Console.WriteLine("[Tower] Stopped.");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _listener.Close();
    }

    private async Task ListenLoopAsync()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;

            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (HttpListenerException) when (!_listener.IsListening)
            {
                break;
            }
            catch (ObjectDisposedException) when (!_listener.IsListening)
            {
                break;
            }

            var handlerTask = HandleRequestSafelyAsync(context);

            lock (_handlerLock)
            {
                _handlerTasks.Add(handlerTask);
            }
        }
    }

    private async Task HandleRequestSafelyAsync(HttpListenerContext context)
    {
        try
        {
            await HandleRequestAsync(context).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Console.WriteLine(
                $"[Tower] Handler failure: {exception.Message}");

            try
            {
                await WriteJsonAsync(
                        context.Response,
                        HttpStatusCode.InternalServerError,
                        "{\"error\":\"internal-server-error\"}")
                    .ConfigureAwait(false);
            }
            catch
            {
                // The client will observe the connection-level failure.
            }
        }
        finally
        {
            context.Response.Close();
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var rawUrl = context.Request.RawUrl ?? "/";
        var path = context.Request.Url?.AbsolutePath ?? "/";

        Console.WriteLine(
            $"[Tower] START {context.Request.HttpMethod} {rawUrl}");

        await Task.Delay(GetResponseDelayMs(path)).ConfigureAwait(false);

        switch (path)
        {
            case "/route":
                await HandleRouteAsync(context).ConfigureAwait(false);
                break;

            case "/weather":
                await WriteJsonAsync(
                        context.Response,
                        HttpStatusCode.OK,
                        "{\"condition\":\"storm\"}")
                    .ConfigureAwait(false);
                break;

            case "/restrictions":
                await WriteJsonAsync(
                        context.Response,
                        HttpStatusCode.OK,
                        "{\"maxCheckpoints\":2}")
                    .ConfigureAwait(false);
                break;

            default:
                await WriteJsonAsync(
                        context.Response,
                        HttpStatusCode.NotFound,
                        "{\"error\":\"not-found\"}")
                    .ConfigureAwait(false);
                break;
        }

        Console.WriteLine(
            $"[Tower] COMPLETE {context.Request.HttpMethod} {rawUrl}");
    }

    private static async Task HandleRouteAsync(HttpListenerContext context)
    {
        // RawUrl is intentionally read here because it is the assignment's
        // selected learning point for inspecting request URL/query data.
        var rawUrl = context.Request.RawUrl ?? string.Empty;
        var droneName = context.Request.QueryString["drone"];

        Console.WriteLine($"[Tower] ROUTE RawUrl = {rawUrl}");

        if (string.IsNullOrWhiteSpace(droneName))
        {
            await WriteJsonAsync(
                    context.Response,
                    HttpStatusCode.BadRequest,
                    "{\"error\":\"missing-drone\"}")
                .ConfigureAwait(false);
            return;
        }

        if (!RouteMaximums.TryGetValue(droneName, out var maxCheckpoints))
        {
            await WriteJsonAsync(
                    context.Response,
                    HttpStatusCode.NotFound,
                    "{\"error\":\"unknown-drone\"}")
                .ConfigureAwait(false);
            return;
        }

        await WriteJsonAsync(
                context.Response,
                HttpStatusCode.OK,
                $"{{\"maxCheckpoints\":{maxCheckpoints}}}")
            .ConfigureAwait(false);
    }

    private static int GetResponseDelayMs(string path)
    {
        var baseDelay = path switch
        {
            "/route" => 150,
            "/weather" => 250,
            "/restrictions" => 350,
            _ => 50
        };

        return baseDelay + Random.Shared.Next(0, 101);
    }

    private static async Task WriteJsonAsync(
        HttpListenerResponse response,
        HttpStatusCode statusCode,
        string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);

        response.StatusCode = (int)statusCode;
        response.ContentType = "application/json; charset=utf-8";
        response.ContentLength64 = bytes.Length;

        await response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
    }
}
