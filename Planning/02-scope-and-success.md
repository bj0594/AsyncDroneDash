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
- Multiple concurrent async flights.
- `await Task.WhenAll`.
- Orchestration-level `try/catch`.
- Comparison with Part B.

### Delivery

- GitHub repository.
- Runnable application.
- Menu for Parts A–D.
- `README.md`.
- `reflection.md`.

---

# 3. Final target scope

Part D is optional in the assignment but is currently selected as a project target.

## IN SCOPE — FINAL TARGET

Everything in the MVP, plus:

- local control-tower HTTP service;
- asynchronous `HttpClient` consumption;
- route data;
- weather data;
- simulation changes based on retrieved data;
- HTTP failure handling;
- timeout handling.

The project may also implement:

- temporary restrictions;
- HTTP lifecycle logging;
- sequential versus concurrent HTTP comparison;
- variable response time.

These remain optional within Part D unless explicitly promoted to the final target implementation.

---

# 4. OUT OF SCOPE

Unless later promoted into scope:

- database or persistence;
- authentication;
- physical flight simulation;
- geography or real route optimization;
- packages/customers/delivery management;
- battery/fuel simulation;
- mandatory cancellation support;
- mandatory retry/backoff;
- `IAsyncEnumerable`;
- unrelated features;
- additional architectural layers without a concrete responsibility.

Bonus features must not become prerequisites for completion.

---

# 5. MVP Definition of Done

The MVP is complete when:

- `R1–R21` are satisfied;
- `R22–R25` delivery requirements are satisfied;
- Parts A–C can be demonstrated from the application menu;
- required execution mechanisms are present and observable;
- the application builds and runs;
- relevant automated tests pass;
- required implementation inspections pass;
- required manual demonstrations pass;
- README and reflection requirements are satisfied;
- the project is stored in GitHub.

Part D is not required for MVP completion.

---

# 6. Final project success

The final project is successful when the MVP Definition of Done is satisfied and the selected Part D scope is also successfully implemented and verified.

If Part D cannot be completed without putting the mandatory submission at risk, the project remains an acceptable MVP.

---

# 7. Acceptance criteria

## Core

### AC-CORE-1 — Runnable project

`R1`

The project builds and launches as a C# console application.

### AC-CORE-2 — Drone model

`R2`

A drone is represented by:

- `Name`;
- `MaxCheckpoints`;
- `DelayMs`.

### AC-CORE-3 — Checkpoint progression

`R3`

For a valid drone with `MaxCheckpoints = 3`, the flight reports:

`0 → 1 → 2 → 3`

in ascending order.

### AC-CORE-4 — Checkpoint delay

`R4`

The configured `DelayMs` is applied between checkpoint steps.

### AC-CORE-5 — Flight reporting

`R5`

A successful flight reports:

- start;
- every checkpoint;
- completion.

---

## Part A

### AC-A1 — Concurrent Threads

`R6`

At least two drones execute using separate `Thread` instances.

### AC-A2 — Join waits for all drones

`R7`

The normal Part A run does not report overall completion until all required drone threads have finished.

### AC-A3 — No-Join behaviour

`R8`

The no-`Join` demonstration allows the main thread to continue before all drone work has completed.

### AC-A4 — Concurrent output

`R9`

Concurrent drone output can be observed as interleaved or otherwise non-deterministic.

---

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

A defined drone failure scenario causes the affected operation to fail.

### AC-B5 — Failure propagation

`R14`

The failure reaches the orchestration layer through the Task/TCS model.

### AC-B6 — Task.Exception

`R15`

The failure can be observed through `Task.Exception`, including the relevant underlying exception.

---

## Part C

### AC-C1 — Async flight

`R16`

A drone flight is implemented as an asynchronous operation.

### AC-C2 — Async checkpoint delay

`R17`

Checkpoint delays use `await Task.Delay`.

### AC-C3 — Multiple async flights

`R18`

At least two async drone flights can progress concurrently.

### AC-C4 — Async Task.WhenAll

`R19`

The orchestration uses `await Task.WhenAll` to coordinate multiple async flights.

### AC-C5 — Async error handling

`R20`

Async flight failure reaches orchestration and is handled with `try/catch`.

### AC-C6 — Comparison with Part B

`R21`

The project provides a meaningful comparison of Part B and Part C regarding:

- boilerplate;
- complexity;
- readability;
- maintainability.

---

## Delivery

### AC-DLV-1 — Menu

`R22`

The application provides access to Parts A–D.

### AC-DLV-2 — GitHub

`R23`

The project exists in a GitHub repository.

### AC-DLV-3 — README

`R24`

README contains:

- prerequisites;
- build/run instructions;
- testing instructions for the parts;
- local HTTP startup instructions if applicable.

### AC-DLV-4 — Reflection

`R25`

`reflection.md` contains the required observations, short answers, and relevant thoughts.

---

# 8. Conditional Part D acceptance criteria

These become active if Part D remains in final scope.

### AC-D1 — Route information

`PD2`

The control-tower service returns route information that can affect the simulation.

### AC-D2 — Weather information

`PD3`

The control-tower service returns weather information that can affect the simulation.

### AC-D3 — Asynchronous HTTP consumption

`PD4`

The client consumes the control-tower service asynchronously using `HttpClient`.

### AC-D4 — Simulation effect

`PD5`

Retrieved data changes the simulation according to the documented mapping.

### AC-D5 — HTTP failure handling

`PD6`

HTTP/network failures produce documented error handling.

### AC-D6 — Timeout handling

`PD7`

Request timeouts produce documented timeout handling.

### AC-D7 — Non-blocking HTTP flow

`PD8`

The HTTP flow does not introduce synchronous blocking into the async path.

### AC-D8 — Restrictions

`PD9`

If included, temporary restrictions are retrieved and applied according to the final contract.

### AC-D9 — HTTP lifecycle logging

`PD10`

If included, request start and completion/failure can be observed.

### AC-D10 — Sequential/concurrent comparison

`PD11`

If included, sequential and concurrent HTTP calls can be compared using equivalent functional results.

### AC-D11 — Variable response time

`PD12`

If included, the local service can simulate variable response times.

---

# 9. Scope protection

The following rules protect the project from scope creep:

- A–C always take priority over Part D.
- Part D work begins only after the mandatory core is stable enough to protect the submission.
- Bonus features never become prerequisites for completion.
- New features require a documented connection to the assignment or a deliberate learning objective.
- Technical curiosity alone is not sufficient reason to expand scope.
- New scope must receive corresponding requirements, behaviours, and verification before implementation.

---

# 10. Success evidence

Success is demonstrated through multiple kinds of evidence.

### Automated

- deterministic core behaviours;
- checkpoint progression;
- task/async completion and failure;
- relevant Part D HTTP behaviour.

### Implementation inspection

- required execution mechanisms;
- correct async APIs;
- absence of synchronous blocking where prohibited.

### Manual

- Thread/Join demonstration;
- no-Join comparison;
- concurrent console output;
- menu;
- selected Part D demonstration.

### Documentation

- README;
- reflection;
- GitHub repository.

---

# 11. Current scope status

## MVP

- [x] Defined.
- [x] Mandatory A–C requirements included.
- [x] Delivery requirements included.
- [x] Definition of Done defined.

## Final target

- [x] Part D selected as project target.
- [x] Part D remains optional at assignment level.
- [ ] Final Part D API contract.
- [ ] Final Part D response/data contract.
- [ ] Final Part D failure contract.

## Later / bonus

- [ ] Cancellation.
- [ ] Retry/backoff.
- [ ] `IAsyncEnumerable`.

These remain outside mandatory completion.