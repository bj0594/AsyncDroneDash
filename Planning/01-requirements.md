# Async Drone Dash — Requirements

## 1. Project objective

Async Drone Dash is a C# console application that simulates multiple delivery drones flying routes.

The project demonstrates and compares:

1. `Thread` + `Join`
2. `Task` + `TaskCompletionSource`
3. `async`/`await`

Part D is optional in the assignment and concerns asynchronous HTTP communication with a control-tower service.

The core simulation is intentionally simple so that differences between the execution models remain observable.

---

# 2. Mandatory functional requirements

## Core simulator

### R1 — Runnable console application

The project shall provide a runnable C# console application.

### R2 — Drone model

Each drone shall have:

- `Name : string`
- `MaxCheckpoints : int`
- `DelayMs : int`

### R3 — Checkpoint progression

Each drone shall progress from checkpoint `0` through its configured `MaxCheckpoints`.

### R4 — Checkpoint delay

The configured `DelayMs` shall be applied between checkpoint steps.

### R5 — Progress reporting

The application shall report:

- drone start;
- each checkpoint reached;
- drone completion.

---

## Part A — Thread Race

### R6 — Concurrent Thread execution

The application shall start at least two drones concurrently, with each drone running on its own `Thread`.

### R7 — Join

The normal Part A run shall use `Join` to wait for all required drone threads before reporting that all drones are finished.

### R8 — No-Join demonstration

The application shall demonstrate the effect of removing `Join`.

### R9 — Non-deterministic concurrent output

The application shall demonstrate that concurrent console output can become interleaved or otherwise non-deterministic.

---

## Part B — Task + TaskCompletionSource

### R10 — Task-based drone flights

Drone flights shall be represented using `Task`.

### R11 — One TaskCompletionSource per drone

Each participating drone shall have one `TaskCompletionSource` representing its completion or failure.

### R12 — Task.WhenAll coordination

At least two drone tasks shall be coordinated using `Task.WhenAll`.

### R13 — Failure scenario

At least one drone-flight failure scenario shall be demonstrated.

### R14 — Failure propagation

The failure shall propagate through the TCS/Task model and reach the orchestration level.

### R15 — Task.Exception

The failure demonstration shall include observation of task exception information through `Task.Exception`.

---

## Part C — Async/Await

### R16 — Async drone flight

Drone flight shall be implemented as an asynchronous method.

### R17 — Async checkpoint delay

Checkpoint delays shall use `await Task.Delay`.

### R18 — Multiple async flights

Multiple drone flights shall be able to progress concurrently.

### R19 — Async Task.WhenAll

Multiple async flights shall be coordinated using `await Task.WhenAll`.

### R20 — Async error handling

The orchestration shall use `try/catch` for async flight failures.

### R21 — Comparison with Part B

The project shall provide a meaningful comparison between Part B and Part C regarding:

- boilerplate;
- complexity;
- readability;
- maintainability.

---

# 3. Delivery requirements

### R22 — Menu

The application shall provide a menu giving access to Parts A–D.

### R23 — GitHub repository

The project shall be stored in a GitHub repository.

### R24 — README

The repository shall contain `README.md` with:

- prerequisites;
- build/run instructions;
- instructions for testing each part;
- instructions for starting a local HTTP service if one is included.

### R25 — Reflection

The repository shall contain `reflection.md` with the required observations, short answers, and other relevant thoughts.

---

# 4. Conditional Part D requirements

Part D is optional in the assignment.

The following requirements become active only if Part D is included in the final project scope.

### PD1 — Control-tower service

The selected Part D implementation shall provide control-tower data through HTTP.

The assignment permits either:

- an external demo API; or
- a self-hosted local `HttpListener` service.

The project currently targets the local-service alternative.

### PD2 — Route data

The control tower shall provide route information that can affect the simulation.

### PD3 — Weather data

The control tower shall provide weather information that can affect the simulation.

### PD4 — Asynchronous HTTP consumption

The client shall consume the control-tower service asynchronously using `HttpClient` and appropriate asynchronous APIs.

### PD5 — Simulation effect

Retrieved control-tower data shall affect the simulation, such as `DelayMs` or `MaxCheckpoints`.

### PD6 — HTTP failure handling

The application shall handle network/HTTP failures with appropriate error handling.

### PD7 — HTTP timeout handling

The application shall handle request timeouts with appropriate error handling.

### PD8 — Non-blocking HTTP flow

The HTTP implementation shall not introduce synchronous blocking into the asynchronous request flow.

---

# 5. Additional Part D capabilities from the assignment

These are optional capabilities within the optional Part D.

### PD9 — Temporary restrictions

The control tower may provide temporary restriction data that affects the simulation.

### PD10 — HTTP lifecycle logging

The application may log request start and completion/failure so overlapping requests can be observed.

### PD11 — Sequential versus concurrent HTTP comparison

The project may compare sequential and concurrent control-tower requests.

### PD12 — Variable response time

The local-service alternative may introduce variable response time to simulate slow network conditions.

### PD13 — Drone registration

A bonus implementation may provide an endpoint for registering a drone before flight.

These capabilities are not required for the mandatory assignment and are not prerequisites for MVP completion.

---

# 6. Bonus functionality

The following are explicitly optional extras from the assignment:

- `CancellationToken` support for cancelling drones;
- retry/backoff around HTTP calls;
- `IAsyncEnumerable` for streaming drone progress.

They are not required for the project to be considered complete.

---

# 7. Explicit edge cases from the assignment

The assignment specifically calls for consideration of:

- negative `DelayMs`;
- negative `MaxCheckpoints`;
- unidentified/missing drone name;
- weather/API failure;
- timeout;
- cancellation as a bonus.

The assignment does not define the exact result for every edge case.

Concrete validation and exception contracts are therefore defined in the later domain/design documents before dependent tests are finalized.

---

# 8. Technical requirements

The following implementation mechanisms are explicitly required by the mandatory Parts A–C:

- `Thread`
- `Thread.Join`
- `Task`
- `TaskCompletionSource`
- `Task.WhenAll`
- `async`/`await`
- `Task.Delay`
- `try/catch`

The implementation must not replace these required mechanisms with unrelated alternatives.

For Part D, `HttpClient` and asynchronous HTTP APIs are required only when Part D is included.

---

# 9. Best-practice guidance from the assignment

These are implementation guidance rather than separate functional requirements:

- Do not mix synchronous blocking such as `.Result` or `.Wait()` into async flow.
- Propagate asynchronous operations upward.
- Use `Task.WhenAll` for independent operations.
- Log enough information to make overlap visible.
- Separate orchestration from the actual drone work.
- Keep the implementation proportional to the assignment.

---

# 10. Requirement ownership

The requirement IDs in this document are the authoritative requirement IDs for the project.

Mandatory requirements:

`R1–R25`

Conditional Part D requirements:

`PD1–PD13`

Bonus functionality is deliberately outside the mandatory requirement chain.

Later planning documents must trace requirements to acceptance criteria, behaviours, and verification without changing what the requirement itself means.

---

# 11. Status

## Mandatory requirements

`R1–R25` extracted and identified.

## Part D

`PD1–PD13` identified as conditional/optional.

## Bonus

Cancellation, retry/backoff and `IAsyncEnumerable` remain optional.

## Open decisions

Concrete validation, exception, HTTP-response, and simulation-mapping contracts are maintained in the later planning/design documents.