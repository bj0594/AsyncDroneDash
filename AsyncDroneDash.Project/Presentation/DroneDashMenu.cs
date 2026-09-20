using System.Diagnostics;
using System.Net;

namespace AsyncDroneDash.Project.Presentation;

public sealed class DroneDashMenu
{
    public async Task RunAsync()
    {
        while (true)
        {
            Console.Clear();
            PrintMenu();

            var key = Console.ReadKey(intercept: true).Key;

            if (key == ConsoleKey.Q)
            {
                return;
            }

            if (!TryGetOperation(key, out var operation))
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Unknown selection. Press any key to continue.");
                Console.ReadKey(intercept: true);
                continue;
            }

            Console.WriteLine();
            Console.WriteLine();

            try
            {
                await operation();
            }
            catch (Exception exception)
            {
                Console.WriteLine(
                    $"[Application] Error: {exception.GetType().Name}: {exception.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to return to the operations menu.");
            Console.ReadKey(intercept: true);
        }
    }

    private static void PrintMenu()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("           ASYNC DRONE DASH");
        Console.WriteLine("          FLIGHT OPERATIONS");
        Console.WriteLine("========================================");
        Console.WriteLine();
        Console.WriteLine("[1] Threaded Flight");
        Console.WriteLine("[2] Task-based Flight");
        Console.WriteLine("[3] Asynchronous Flight");
        Console.WriteLine("[4] Control Tower");
        Console.WriteLine("[Q] Exit");
        Console.WriteLine();
        Console.Write("Select an operation: ");
    }

    private static bool TryGetOperation(
        ConsoleKey key,
        out Func<Task> operation)
    {
        operation = key switch
        {
            ConsoleKey.D1 or ConsoleKey.NumPad1 => RunThreadedFlightAsync,
            ConsoleKey.D2 or ConsoleKey.NumPad2 => RunTaskFlightAsync,
            ConsoleKey.D3 or ConsoleKey.NumPad3 => RunAsyncFlightAsync,
            ConsoleKey.D4 or ConsoleKey.NumPad4 => RunControlTowerAsync,
            _ => null!
        };

        return operation is not null;
    }

    private static Task RunThreadedFlightAsync()
    {
        Console.WriteLine("THREADED FLIGHT");
        Console.WriteLine("===============");

        var drones = CreateDemoDrones(delayMs: 150, maxCheckpoints: 3);

        Console.WriteLine();
        Console.WriteLine("Joined flight:");
        ThreadRace.RunWithJoin(drones, ReportFlightEvent);
        Console.WriteLine("All drones have finished.");

        Console.WriteLine();
        Console.WriteLine("Unjoined flight:");

        ThreadRace.RunWithoutJoin(
            CreateDemoDrones(delayMs: 200, maxCheckpoints: 4),
            ReportFlightEvent);

        Console.WriteLine(
            "The main thread continues without waiting for the drone threads.");
        Console.WriteLine(
            "Drone output may continue before you return to the operations menu.");

        return Task.CompletedTask;
    }

    private static async Task RunTaskFlightAsync()
    {
        Console.WriteLine("TASK-BASED FLIGHT");
        Console.WriteLine("=================");

        var drones = CreateDemoDrones(delayMs: 100, maxCheckpoints: 3);

        Console.WriteLine();
        Console.WriteLine("Standard flight:");

        await TaskFlightRunner.RunAsync(
            drones,
            failureDroneName: null,
            ReportFlightEvent);

        Console.WriteLine("All drone tasks have completed.");

        Console.WriteLine();
        Console.WriteLine("Failure handling:");

        Task failureTask = TaskFlightRunner.RunAsync(
            CreateDemoDrones(delayMs: 100, maxCheckpoints: 3),
            failureDroneName: "Alpha",
            ReportFlightEvent);

        try
        {
            await failureTask;
        }
        catch (InvalidOperationException exception)
        {
            Console.WriteLine(
                $"Orchestration received the expected error: {exception.Message}");
        }

        if (failureTask.IsFaulted && failureTask.Exception is not null)
        {
            Console.WriteLine("Task.Exception observed:");

            foreach (var exception in failureTask.Exception.Flatten().InnerExceptions)
            {
                Console.WriteLine(
                    $"- {exception.GetType().Name}: {exception.Message}");
            }
        }
    }

    private static async Task RunAsyncFlightAsync()
    {
        Console.WriteLine("ASYNCHRONOUS FLIGHT");
        Console.WriteLine("==================");

        Console.WriteLine();
        Console.WriteLine("Standard flight:");

        await AsyncFlightRunner.RunAsync(
            CreateDemoDrones(delayMs: 100, maxCheckpoints: 3),
            failureDroneName: null,
            ReportFlightEvent);

        Console.WriteLine("All asynchronous flights have completed.");

        Console.WriteLine();
        Console.WriteLine("Failure handling:");

        try
        {
            await AsyncFlightRunner.RunAsync(
                CreateDemoDrones(delayMs: 100, maxCheckpoints: 3),
                failureDroneName: "Alpha",
                ReportFlightEvent);
        }
        catch (InvalidOperationException exception)
        {
            Console.WriteLine(
                $"Orchestration caught the expected error: {exception.Message}");
        }
    }

    private static async Task RunControlTowerAsync()
    {
        Console.WriteLine("CONTROL TOWER");
        Console.WriteLine("=============");

        await using var tower = new LocalControlTower();

        try
        {
            tower.Start();
        }
        catch (HttpListenerException exception)
        {
            Console.WriteLine(
                "The Control Tower could not start. This may be caused by a URL ACL/access issue for localhost:8080.");
            Console.WriteLine($"Detail: {exception.Message}");
            return;
        }

        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(LocalControlTower.Prefix),
            Timeout = TimeSpan.FromSeconds(3)
        };

        var client = new ControlTowerClient(httpClient);
        var orchestrator = new ControlTowerOrchestrator(client);
        var drone = new DroneModel
        {
            Name = "Alpha",
            MaxCheckpoints = 10,
            DelayMs = 100
        };

        var sequentialWatch = Stopwatch.StartNew();
        var sequential = await orchestrator.LoadSequentialAsync(drone);
        sequentialWatch.Stop();

        Console.WriteLine();
        Console.WriteLine(
            $"Sequential configuration: MaxCheckpoints={sequential.MaxCheckpoints}, DelayMs={sequential.DelayMs}, time={sequentialWatch.ElapsedMilliseconds} ms");

        var concurrentWatch = Stopwatch.StartNew();
        var concurrent = await orchestrator.LoadConcurrentAsync(drone);
        concurrentWatch.Stop();

        Console.WriteLine();
        Console.WriteLine(
            $"Concurrent configuration: MaxCheckpoints={concurrent.MaxCheckpoints}, DelayMs={concurrent.DelayMs}, time={concurrentWatch.ElapsedMilliseconds} ms");

        Console.WriteLine();
        Console.WriteLine("Running an async flight with the Control Tower configuration:");

        await AsyncFlightRunner.RunAsync(
            new[] { concurrent },
            failureDroneName: null,
            ReportFlightEvent);

        Console.WriteLine("The Control Tower operation is complete.");
    }

    private static DroneModel[] CreateDemoDrones(int delayMs, int maxCheckpoints)
    {
        return
        [
            new DroneModel
            {
                Name = "Alpha",
                MaxCheckpoints = maxCheckpoints,
                DelayMs = delayMs
            },
            new DroneModel
            {
                Name = "Beta",
                MaxCheckpoints = maxCheckpoints,
                DelayMs = delayMs
            }
        ];
    }

    private static void ReportFlightEvent(FlightEvent flightEvent)
    {
        var details = flightEvent.Type switch
        {
            FlightEventType.Started => "start",
            FlightEventType.CheckpointReached =>
                $"checkpoint {flightEvent.Checkpoint}",
            FlightEventType.Completed => "completed",
            FlightEventType.Faulted =>
                $"ERROR: {flightEvent.Exception?.Message}",
            _ => flightEvent.Type.ToString()
        };

        Console.WriteLine(
            $"[{DateTime.Now:HH:mm:ss.fff}] {flightEvent.DroneName}: {details}");
    }
}