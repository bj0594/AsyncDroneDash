using System.Diagnostics;
using System.Net;

namespace AsyncDroneDash.Project;

public static class Program
{
    public static async Task Main()
    {
        while (true)
        {
            PrintMenu();
            var choice = Console.ReadLine()?.Trim();

            Console.WriteLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        RunPartA();
                        break;

                    case "2":
                        await RunPartBAsync();
                        break;

                    case "3":
                        await RunPartCAsync();
                        break;

                    case "4":
                        await RunPartDAsync();
                        break;

                    case "0":
                        return;

                    default:
                        Console.WriteLine("Invalid choice.");
                        break;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(
                    $"[Application] Error: {exception.GetType().Name}: {exception.Message}");
            }

            if (choice != "0")
            {
                Console.WriteLine();
                Console.WriteLine("Press Enter to return to the menu.");
                Console.ReadLine();
                Console.WriteLine();
            }
        }
    }

    private static void PrintMenu()
    {
        Console.WriteLine("================================");
        Console.WriteLine("       ASYNC DRONE DASH");
        Console.WriteLine("================================");
        Console.WriteLine("1 - Part A: Thread + Join");
        Console.WriteLine("2 - Part B: Task + TaskCompletionSource");
        Console.WriteLine("3 - Part C: async/await");
        Console.WriteLine("4 - Part D: Control Tower HTTP");
        Console.WriteLine("0 - Exit");
        Console.Write("Choice: ");
    }

    private static void RunPartA()
    {
        Console.WriteLine("--- Part A: Thread Race ---");

        var drones = CreateDemoDrones(delayMs: 150, maxCheckpoints: 3);

        Console.WriteLine();
        Console.WriteLine("With Join:");
        ThreadRace.RunWithJoin(drones, ReportFlightEvent);
        Console.WriteLine("All drones are finished after Join.");

        Console.WriteLine();
        Console.WriteLine("Without Join:");

        ThreadRace.RunWithoutJoin(
            CreateDemoDrones(delayMs: 200, maxCheckpoints: 4),
            ReportFlightEvent);

        Console.WriteLine(
            "The main thread continues immediately. Drone threads continue independently of the main thread.");
    }

    private static async Task RunPartBAsync()
    {
        Console.WriteLine("--- Part B: Task + TaskCompletionSource ---");

        var drones = CreateDemoDrones(delayMs: 100, maxCheckpoints: 3);

        Console.WriteLine();
        Console.WriteLine("Normal run:");
        await TaskFlightRunner.RunAsync(
            drones,
            failureDroneName: null,
            ReportFlightEvent);
        Console.WriteLine("Task.WhenAll has completed for all drones.");

        Console.WriteLine();
        Console.WriteLine("Failure scenario:");

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
            Console.WriteLine("Task.Exception:");
            Console.WriteLine(failureTask.Exception);
        }
    }

    private static async Task RunPartCAsync()
    {
        Console.WriteLine("--- Part C: async/await ---");

        Console.WriteLine();
        Console.WriteLine("Normal run:");

        await AsyncFlightRunner.RunAsync(
            CreateDemoDrones(delayMs: 100, maxCheckpoints: 3),
            failureDroneName: null,
            ReportFlightEvent);

        Console.WriteLine("await Task.WhenAll has completed for all drones.");

        Console.WriteLine();
        Console.WriteLine("Failure scenario:");

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

    private static async Task RunPartDAsync()
    {
        Console.WriteLine("--- Part D: Control Tower HTTP ---");

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
        Console.WriteLine("Running an async flight with the control tower's final configuration:");

        await AsyncFlightRunner.RunAsync(
            new[] { concurrent },
            failureDroneName: null,
            ReportFlightEvent);

        Console.WriteLine("The Part D demonstration is complete. The Control Tower will now be stopped.");
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
