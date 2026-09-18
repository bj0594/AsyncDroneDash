# Async Drone Dash

Async Drone Dash is a C# console application that simulates multiple delivery drones and demonstrates different approaches to concurrent and asynchronous execution.

The project compares:

- Part A — `Thread` + `Join`
- Part B — `Task` + `TaskCompletionSource`
- Part C — `async`/`await`
- Part D — optional asynchronous HTTP communication with a control-tower API

The main purpose is to make the differences between these execution models observable through a small and deliberately simple simulation.

---

## Project status

Parts A–C are the required MVP.

Part D is optional but is currently planned for implementation.

The project is developed using TDD. Behaviour planning and test design are kept separately from the production implementation.

---

## Requirements

- .NET 10 SDK
- C# / .NET
- A terminal capable of running the `dotnet` CLI

---

## Project structure

    Async Drone Dash/
    ├── Planning/
    │   ├── 01-requirements.md
    │   ├── 02-scope-and-success.md
    │   ├── 03-domain-and-rules.md
    │   ├── 04-design-and-traceability.md
    │   ├── 05-behaviour-design.md
    │   └── TestPlan.md
    │
    ├── AsyncDroneDash.Project/
    │   ├── README.md
    │   ├── Program.cs
    │   └── AsyncDroneDash.Project.csproj
    │
    ├── AsyncDroneDash.Tests/
    │   └── AsyncDroneDash.Tests.csproj
    │
    ├── AsyncDroneDash.slnx
    └── reflection.md

`Planning/` contains the project's planning, design and test-planning documents.

`AsyncDroneDash.Project/` contains the production console application.

`AsyncDroneDash.Tests/` contains the automated test project.

`reflection.md` contains the project's required reflections and observations.

---

## Running the application

From the repository root:

    dotnet run --project .\AsyncDroneDash.Project

The application provides a menu for Parts A–D.

---

## Running the tests

From the repository root:

    dotnet test

The automated tests cover behaviours that can be verified deterministically.

Some assignment requirements are better verified through manual observation or implementation inspection, particularly requirements involving specific concurrency mechanisms and non-deterministic console output.

---

## Part A — Thread Race

Part A demonstrates concurrent drone execution using `Thread`.

It includes:

- at least two concurrent drones;
- one `Thread` per drone;
- checkpoint progression;
- configurable delay between checkpoint steps;
- start, checkpoint and completion logging;
- `Thread.Join` to wait for all drone threads;
- a demonstration without `Join`;
- observable concurrent/interleaved console output.

To test Part A:

1. Run Part A from the application menu.
2. Observe the normal execution with `Join`.
3. Observe the no-`Join` demonstration.
4. Compare the order and timing of the console output.

The exact ordering of concurrent output is not deterministic.

---

## Part B — Task + TaskCompletionSource

Part B demonstrates task-based execution with explicit completion signalling.

It includes:

- a `Task` for each drone operation;
- one `TaskCompletionSource` per drone;
- multiple concurrent drone operations;
- `Task.WhenAll`;
- a deterministic failure scenario;
- task-based failure propagation;
- observation of `Task.Exception`.

To test Part B:

1. Run Part B from the application menu.
2. Observe successful task completion.
3. Observe the configured failure scenario.
4. Confirm that the failure reaches the orchestration layer.
5. Observe the task exception information.

---

## Part C — Async/Await

Part C demonstrates asynchronous programming directly.

It includes:

- asynchronous drone-flight methods;
- `await Task.Delay` between checkpoint steps;
- multiple concurrent async flights;
- `await Task.WhenAll`;
- orchestration-level `try/catch`;
- no synchronous `.Wait()` or `.Result` in the async execution path.

To test Part C:

1. Run Part C from the application menu.
2. Observe multiple drones progressing concurrently.
3. Observe successful completion.
4. Observe the configured failure behaviour.
5. Compare the structure with Part B.

---

## Part D — Control Tower API

Part D is optional.

The planned implementation may provide:

- route data;
- weather data;
- temporary restrictions;
- asynchronous HTTP communication using `HttpClient`;
- data that affects drone simulation parameters;
- timeout and HTTP failure handling;
- HTTP request logging;
- comparison of sequential and concurrent HTTP calls.

The final API source and local-service setup will be documented here if Part D is implemented using a local HTTP service.

---

## Drone model

The shared drone model is:

| Property | Type | Description |
|---|---|---|
| `Name` | `string` | Identifies the drone |
| `MaxCheckpoints` | `int` | Highest checkpoint the drone should reach |
| `DelayMs` | `int` | Delay in milliseconds between checkpoint steps |

Checkpoint progression is interpreted inclusively.

For example:

    MaxCheckpoints = 3

    0 → 1 → 2 → 3

---

## Testing strategy

The project does not attempt to force every requirement into an automated unit test.

Testing uses the most appropriate verification method for each behaviour:

- unit tests for deterministic core behaviour;
- component/integration tests where orchestration or HTTP behaviour requires multiple components;
- manual observation for intentionally non-deterministic concurrency demonstrations;
- implementation inspection for explicit technology requirements such as `Thread`, `Join`, `TaskCompletionSource`, `Task.Delay`, and `await Task.WhenAll`.

The detailed test matrix and test inventory are maintained in:

`Planning/TestPlan.md`

---

## Documentation

The repository contains:

- `Planning/` — project planning, design and test planning
- `README.md` — project usage and verification information
- `reflection.md` — required observations and reflections

The planning documents describe decisions made before implementation. They are not a substitute for the production code or tests.

---

## Scope

The MVP focuses on Parts A–C and the required project deliverables.

Optional functionality such as Part D, cancellation, retry/backoff and `IAsyncEnumerable` is deferred until the mandatory functionality is working.

---

## Learning objective

The project is intentionally small so that the execution models themselves remain visible.

The comparison focuses on:

- direct thread management;
- explicit task completion and failure propagation;
- asynchronous control flow;
- coordination of independent operations;
- differences in boilerplate, readability and maintainability.