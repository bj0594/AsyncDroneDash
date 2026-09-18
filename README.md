# Async Drone Dash

Async Drone Dash is a C# console application that demonstrates three execution models for a simple drone-flight simulation:

- Part A — `Thread` + `Join`
- Part B — `Task` + `TaskCompletionSource`
- Part C — `async`/`await`
- Part D — optional local control-tower HTTP service with `HttpClient`

The project deliberately keeps the simulation small so that concurrency, task coordination, failure propagation, and asynchronous HTTP behaviour remain visible.

## Scope

Parts A–C form the mandatory MVP.

Part D is optional in the assignment but is currently included in this project's final target scope.

Bonus features such as cancellation, retry/backoff, `IAsyncEnumerable`, and drone registration are not required.

## Requirements

- .NET 10 SDK
- Git
- `dotnet` CLI

## Project structure

```text
Async Drone Dash/
├── Planning/
│   ├── 01-requirements.md
│   ├── 02-scope-and-success.md
│   ├── 03-domain-and-rules.md
│   ├── 04-design-and-traceability.md
│   └── 05-behaviour-design.md
├── AsyncDroneDash.Project/
│   ├── Program.cs
│   └── AsyncDroneDash.Project.csproj
├── AsyncDroneDash.Tests/
│   ├── TestPlan.md
│   └── AsyncDroneDash.Tests.csproj
├── AsyncDroneDash.slnx
├── README.md
└── reflection.md
```

`TestPlan.md` belongs to the test project because it documents the test project's verification plan.

## Build

From the repository root:

```powershell
dotnet build
```

## Run

```powershell
dotnet run --project .\AsyncDroneDash.Project
```

The application provides a menu for Parts A–D.

## Run automated tests

```powershell
dotnet test
```

Automated tests cover deterministic observable behaviour. Requirements involving specific implementation mechanisms or intentionally non-deterministic console behaviour also use implementation inspection or manual verification.

## Part A — Thread Race

Part A demonstrates:

- at least two drones on separate `Thread` instances;
- checkpoint progression and delay;
- `Join` waiting for all drones;
- a deliberate no-`Join` demonstration;
- non-deterministic/interleaved console output.

Manual verification should compare the normal and no-Join runs and observe how console output can interleave.

## Part B — Task + TaskCompletionSource

Part B demonstrates:

- task-based drone flights;
- one `TaskCompletionSource` per drone;
- `Task.WhenAll`;
- a deterministic simulated failure;
- failure propagation;
- `Task.Exception`.

The failure uses:

```text
InvalidOperationException("Simulated drone failure.")
```

## Part C — Async/Await

Part C demonstrates:

- async drone-flight methods;
- `await Task.Delay`;
- multiple overlapping async flights;
- `await Task.WhenAll`;
- orchestration-level `try/catch`;
- comparison with Part B.

The async path must not use `.Wait()` or `.Result`.

## Part D — Local Control Tower

The project uses the local `HttpListener` option supplied by the assignment.

The client uses one reusable `HttpClient` and asynchronous HTTP APIs.

Endpoints:

```text
GET /route?drone=Navn
GET /weather
GET /restrictions
```

Response models and mappings are documented in `Planning/03-domain-and-rules.md`.

The local service can vary response time to simulate slow network conditions.

### Starting Part D

The local service startup procedure will be documented here once the Part D host/startup implementation is finalized.

### Testing Part D

Part D verification uses a controllable HTTP boundary for automated tests. Manual execution demonstrates the real local service, HTTP logging, and sequential versus concurrent requests.

## Reflection

`reflection.md` is completed after implementation and contains the required observations and comparisons, especially the differences between Thread/Join, Task/TCS, and async/await.
