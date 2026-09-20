# Async Drone Dash

Async Drone Dash is a C# console application that demonstrates three execution models for a simple drone-flight simulation:

- Part A — `Thread` + `Join`

- Part B — `Task` + `TaskCompletionSource`

- Part C — `async`/`await`

- Part D — optional local control-tower HTTP service with `HttpClient`

The project keeps the simulation small so that concurrency, task coordination, failure propagation, and asynchronous HTTP behaviour remain visible.

## Scope

Parts A–C form the mandatory MVP.

Part D is optional in the assignment but is included in this project's final target scope.

Parts A–C always take priority over Part D; Part D must not jeopardize completion of the mandatory MVP.

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
│   ├── DroneModel.cs
│   ├── FlightEvent.cs
│   ├── DroneFlight.cs
│   ├── ThreadRace.cs
│   ├── TaskFlightRunner.cs
│   ├── AsyncFlightRunner.cs
│   ├── RouteData.cs
│   ├── WeatherData.cs
│   ├── RestrictionData.cs
│   ├── ControlTowerErrorKind.cs
│   ├── ControlTowerException.cs
│   ├── ControlTowerClient.cs
│   ├── ControlTowerOrchestrator.cs
│   ├── LocalControlTower.cs
│   └── AsyncDroneDash.Project.csproj
├── AsyncDroneDash.Tests/
│   ├── TestPlan.md
│   ├── DroneModelAndFlightTests.cs
│   ├── ThreadRaceTests.cs
│   ├── TaskFlightTests.cs
│   ├── AsyncFlightTests.cs
│   ├── ControlTowerTests.cs
│   └── AsyncDroneDash.Tests.csproj
├── AsyncDroneDash.slnx
├── global.json
├── README.md
└── reflection.md
```

There is one README, in the repository root. The test verification plan is kept with the test project as `TestPlan.md`.

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

The test project contains the automated verification described in `AsyncDroneDash.Tests/TestPlan.md`. The production implementation follows the locked planning contracts; final runtime verification must be performed in a .NET 10 environment.

## Part A — Thread Race

Part A demonstrates:

- at least two drones on separate `Thread` instances;

- checkpoint progression and delay;

- `Join` waiting for all drones;

- a deliberate no-`Join` demonstration;

- non-deterministic/interleaved console output.

Manual verification compares the normal and no-Join runs and observes how console output can interleave.

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

### Testing Part B

1. Select Part B from the application menu.

2. Run the normal multi-drone scenario and observe `Task.WhenAll` completion.

3. Run the failure scenario.

4. The selected failure drone reports checkpoint `1`, produces a `Faulted` `FlightEvent`, and then triggers the simulated `InvalidOperationException`.

5. Observe failure propagation and the `Task.Exception` information.

## Part C — Async/Await

Part C demonstrates:

- async drone-flight methods;

- `await Task.Delay`;

- multiple overlapping async flights;

- `await Task.WhenAll`;

- orchestration-level `try/catch`;

- comparison with Part B.

The async path must not use `.Wait()` or `.Result`.

### Testing Part C

1. Select Part C from the application menu.

2. Run the normal multi-drone scenario and observe overlapping async progress.

3. Run the failure scenario.

4. The selected failure drone reports checkpoint `1` and then produces the same simulated `InvalidOperationException` used by Part B.

5. Observe the `Faulted` `FlightEvent`, orchestration-level `try/catch` handling, propagation of the original failure, and user-facing error handling. Compare the flow with Part B.

## Part D — Local Control Tower

The project uses the local `HttpListener` option supplied by the assignment because it is explicitly offered as a learning alternative.

The client uses one reusable `HttpClient` and asynchronous HTTP APIs. The Part D orchestration compares sequential and concurrent loading of the independent route, weather, and restriction requests.

Endpoints:

```text
GET /route?drone=Navn

GET /weather

GET /restrictions
```

The route handler reads the requested drone name from the request URL/query data, including the `RawUrl` learning point from the assignment.

Response models and mappings are documented in `Planning/03-domain-and-rules.md`.

The local service uses `http://localhost:8080/` and starts inside the demonstration application process.

### Starting Part D

Select Part D from the application menu. The application starts the local control tower automatically and then consumes it through `HttpClient`.

On Windows, `HttpListener` uses the HTTP.sys infrastructure. If `HttpListener.Start()` reports `Access Denied`, the URL may need a URL ACL reservation. Run an elevated terminal and reserve the selected URL for the current Windows user, for example:

```powershell
netsh http add urlacl url=http://localhost:8080/ user="$env:USERDOMAIN\$env:USERNAME" listen=yes
```

Check existing reservations with:

```powershell
netsh http show urlacl url=http://localhost:8080/
```

Remove the reservation later with:

```powershell
netsh http delete urlacl url=http://localhost:8080/
```

The project does not use HTTPS for the local demonstration.

### Testing Part D

Automated HTTP tests use a controllable HTTP boundary so they do not depend on a live external network service.

The automated verification also covers:

- unknown route/drone handling;
- non-success HTTP responses;
- request timeout handling;
- malformed or invalid responses.

Manual Part D execution demonstrates:

- route/weather/restriction data retrieval;

- request lifecycle logging;

- variable response time;

- sequential versus concurrent requests and their relative execution.

The relative execution time is an observation for the reflection, not a correctness threshold for automated tests.

## Reflection

`reflection.md` is completed after implementation and answers the five reflection questions from the assignment, including the differences between Thread/Join, Task/TCS, and async/await. For Part D it also records what was learned from asynchronous HTTP, timeout/error handling, and sequential versus concurrent calls.