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

Automated tests cover deterministic observable behaviour. Requirements involving specific implementation mechanisms or intentionally non-deterministic console behaviour also use implementation inspection or manual verification.

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

The project uses the local `HttpListener` option supplied by the assignment because it is explicitly offered as a learning alternative.

The client uses one reusable `HttpClient` and asynchronous HTTP APIs.

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

Manual Part D execution demonstrates:

- route/weather/restriction data retrieval;
- request lifecycle logging;
- variable response time;
- sequential versus concurrent requests and their relative execution.

The relative execution time is an observation for the reflection, not a correctness threshold for automated tests.

## Reflection

`reflection.md` is completed after implementation and answers the five reflection questions from the assignment, including the differences between Thread/Join, Task/TCS, and async/await. For Part D it also records what was learned from asynchronous HTTP, timeout/error handling, and sequential versus concurrent calls.
