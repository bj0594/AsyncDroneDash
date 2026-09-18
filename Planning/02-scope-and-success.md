# Async Drone Dash — Scope and Success

## 1. Scope strategy

The project has two scope levels:

1. **MVP** — all mandatory assignment requirements.
2. **Final target** — MVP plus the optional Part D, which this project intends to implement.

Part D must not jeopardize completion of the mandatory MVP.

---

# 2. MVP scope

## IN SCOPE

### Core

- Runnable C# console application.
- Drone model with `Name`, `MaxCheckpoints`, and `DelayMs`.
- Checkpoint progression from `0` to `MaxCheckpoints`.
- Configured delay between checkpoint steps.
- Start, checkpoint, and completion reporting.

### Part A

- At least two concurrent drones.
- Separate `Thread` per drone.
- `Thread.Join` in the normal run.
- No-`Join` demonstration.
- Observable non-deterministic/interleaved console output.

### Part B

- `Task`-based drone flights.
- One `TaskCompletionSource` per drone.
- At least two drones.
- `Task.WhenAll`.
- Deterministic failure scenario.
- Failure propagation.
- `Task.Exception` observation.

### Part C

- Async drone-flight method.
- `await Task.Delay`.
- Multiple overlapping async flights.
- `await Task.WhenAll`.
- Orchestration-level `try/catch`.
- Comparison with Part B.

### Delivery

- GitHub repository.
- Runnable application.
- Menu for Parts A–D.
- Root `README.md`.
- `reflection.md`.

---

# 3. Final target scope

Part D is optional in the assignment but is currently selected as a project target.

## IN SCOPE — FINAL TARGET

Everything in the MVP, plus:

- local `HttpListener` control tower;
- reusable `HttpClient` client;
- asynchronous HTTP requests;
- `/route?drone=Navn`;
- `/weather`;
- `/restrictions`;
- route/weather/restriction data mapping;
- HTTP failure handling;
- timeout handling;
- `ControlTowerException` with explicit error categories;
- HTTP request lifecycle logging;
- sequential versus concurrent HTTP comparison;
- variable local response time.

The target response contracts and mappings are defined in `03-domain-and-rules.md` and `04-design-and-traceability.md`.

---

# 4. OUT OF SCOPE

Unless explicitly promoted later:

- database/persistence;
- authentication;
- physical flight simulation;
- geography or route optimization;
- packages/customers/delivery management;
- battery/fuel simulation;
- mandatory cancellation;
- retry/backoff;
- `IAsyncEnumerable`;
- drone registration;
- unrelated features;
- additional architectural layers without a concrete responsibility.

---

# 5. MVP Definition of Done

The MVP is complete when:

- `R1–R21` are satisfied;
- `R22–R25` delivery requirements are satisfied;
- the application builds and runs;
- the menu works;
- Parts A–C can be demonstrated;
- required implementation mechanisms are present;
- relevant automated tests pass;
- required inspections pass;
- required manual demonstrations pass;
- README and reflection requirements are satisfied;
- the project is in GitHub.

Part D is not required for MVP completion.

---

# 6. Final project success

The final project is successful when the MVP Definition of Done is satisfied and all selected Part D requirements `PD1–PD12` are implemented and verified. The bonus `PD13` is not required.

If Part D cannot be completed without putting the mandatory submission at risk, the project remains a valid MVP and Part D is omitted from the final submission.

---

# 7. Acceptance criteria

## Core

### AC-CORE-1 — Runnable project

`R1`

The project builds and launches as a C# console application.

### AC-CORE-2 — Drone model

`R2`

A drone is represented by `Name`, `MaxCheckpoints`, and `DelayMs`.

### AC-CORE-3 — Checkpoint progression

`R3`

For `MaxCheckpoints = 3`, a successful flight reports `0 → 1 → 2 → 3` in ascending order.

### AC-CORE-4 — Checkpoint delay

`R4`

The configured `DelayMs` is applied between checkpoint steps.

### AC-CORE-5 — Flight reporting

`R5`

A successful flight reports start, every checkpoint, and completion.

## Part A

### AC-A1 — Concurrent Threads

`R6`

At least two drones execute using separate `Thread` instances.

### AC-A2 — Join waits for all drones

`R7`

The normal Part A run does not report overall completion until all required drone threads have finished.

### AC-A3 — No-Join behaviour

`R8`

The no-`Join` demonstration allows main-thread continuation before all drone work has completed.

### AC-A4 — Concurrent output

`R9`

Concurrent drone output can be observed as interleaved or otherwise non-deterministic.

## Part B

### AC-B1 — Task-based flight

`R10`

Each drone flight is represented by a `Task`.

### AC-B2 — Individual TCS

`R11`

Each participating drone has one `TaskCompletionSource`.

### AC-B3 — Multiple task coordination

`R12`

At least two drone tasks are coordinated with `Task.WhenAll`.

### AC-B4 — Failure

`R13`

A defined drone failure scenario causes the affected operation to fault with `InvalidOperationException("Simulated drone failure.")`.

### AC-B5 — Failure propagation

`R14`

The failure reaches orchestration through the Task/TCS model.

### AC-B6 — Task.Exception

`R15`

The faulted task exposes an `AggregateException` through `Task.Exception` containing the simulated failure.

## Part C

### AC-C1 — Async flight

`R16`

A drone flight is implemented as an asynchronous operation.

### AC-C2 — Async checkpoint delay

`R17`

Checkpoint delays use `await Task.Delay`.

### AC-C3 — Multiple async flights

`R18`

At least two async drone flights can make overlapping progress.

### AC-C4 — Async Task.WhenAll

`R19`

The orchestration uses `await Task.WhenAll` to coordinate multiple async flights.

### AC-C5 — Async error handling

`R20`

Async failure reaches orchestration and is handled with `try/catch`.

### AC-C6 — Comparison

`R21`

The project compares Part B and Part C regarding boilerplate, complexity, readability, and maintainability.

## Delivery

### AC-DLV-1 — Menu

`R22`

The application provides access to Parts A–D.

### AC-DLV-2 — GitHub

`R23`

The project exists in a GitHub repository.

### AC-DLV-3 — README

`R24`

The root README contains prerequisites, build/run instructions, testing instructions, and local HTTP startup instructions when applicable.

### AC-DLV-4 — Reflection

`R25`

`reflection.md` contains the required observations, short answers, and relevant thoughts.

## Assignment edge cases

### AC-EDGE-1 — Negative MaxCheckpoints

`E1`

A negative value is rejected with `ArgumentOutOfRangeException`.

### AC-EDGE-2 — Negative DelayMs

`E2`

A negative value is rejected with `ArgumentOutOfRangeException`.

### AC-EDGE-3 — Missing/blank drone name

`E3`

A null, empty, or whitespace-only drone name is rejected with `ArgumentException`.

### AC-EDGE-4 — Unknown drone

`E4`

An unknown Part D route name is reported through `ControlTowerException.NotFound`.

### AC-EDGE-5 — Control-tower failure

`E5`

HTTP failure and timeout produce the documented `ControlTowerException` category when Part D is active.

## Conditional Part D

### AC-D1 — Route information

`PD2`

`/route?drone=Navn` returns valid route information.

### AC-D2 — Weather information

`PD3`

`/weather` returns valid weather information.

### AC-D3 — Async HTTP consumption

`PD4`

The client consumes the control tower asynchronously through one reusable `HttpClient`.

### AC-D4 — Simulation effect

`PD5`

Retrieved route, weather, and restriction data produce the documented final simulation values.

### AC-D5 — HTTP failure

`PD6`

Non-success or connection failures produce `ControlTowerException.RequestFailed` or `NotFound` as appropriate.

### AC-D6 — Timeout

`PD7`

Timeout produces `ControlTowerException.Timeout`.

### AC-D7 — Non-blocking HTTP

`PD8`

The client and local server use asynchronous request flow without synchronous blocking.

### AC-D8 — Restrictions

`PD9`

Restrictions can reduce, but never increase, the route checkpoint maximum.

### AC-D9 — HTTP lifecycle logging

`PD10`

Each HTTP call exposes start and completion/failure logging.

### AC-D10 — Sequential/concurrent comparison

`PD11`

Sequential and concurrent request modes return equivalent functional data.

### AC-D11 — Variable response time

`PD12`

The local server can produce deliberately varied response times for demonstration purposes.

---

# 8. Scope protection

- A–C always take priority over Part D.
- Bonus features never become completion prerequisites.
- New scope requires corresponding requirements, behaviours, and verification.
- Technical curiosity alone is not sufficient reason to expand scope.
