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

Each drone shall have `Name`, `MaxCheckpoints`, and `DelayMs`.

### R3 — Checkpoint progression

Each drone shall progress from checkpoint `0` through its configured `MaxCheckpoints`.

### R4 — Checkpoint delay

The configured `DelayMs` shall be applied between checkpoint steps.

### R5 — Progress reporting

The application shall report drone start, each checkpoint reached, and drone completion.

## Part A — Thread Race

### R6 — Concurrent Thread execution

The application shall start at least two drones concurrently, with each drone running on its own `Thread`.

### R7 — Join

The normal Part A run shall use `Join` to wait for all required drone threads before reporting that all drones are finished.

### R8 — No-Join demonstration

The application shall demonstrate the effect of removing `Join`.

### R9 — Non-deterministic concurrent output

The application shall demonstrate that concurrent console output can become interleaved or otherwise non-deterministic.

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

## Part C — Async/Await

### R16 — Async drone flight

Drone flight shall be implemented as an asynchronous method.

### R17 — Async checkpoint delay

Checkpoint delays shall use `await Task.Delay`.

### R18 — Multiple async flights

Multiple drone flights shall be able to make overlapping progress.

### R19 — Async Task.WhenAll

Multiple async flights shall be coordinated using `await Task.WhenAll`.

### R20 — Async error handling

The orchestration shall use `try/catch` for async flight failures.

### R21 — Comparison with Part B

The project shall provide a meaningful comparison between Part B and Part C regarding boilerplate, complexity, readability, and maintainability.

## Delivery

### R22 — Menu

The application shall provide a menu giving access to Parts A–D.

### R23 — GitHub repository

The project shall be stored in a GitHub repository.

### R24 — README

The repository shall contain a root `README.md` with prerequisites, build/run instructions, testing instructions for each part, and local HTTP startup instructions when a local service is included.

### R25 — Reflection

The repository shall contain `reflection.md` with the required observations, short answers, and other relevant thoughts.

---

# 3. Assignment edge cases

These are explicitly called out by the assignment, but their exact response is not fully specified there.

### E1 — Negative MaxCheckpoints

The application shall handle a negative `MaxCheckpoints` value according to the project's finalized validation contract.

### E2 — Negative DelayMs

The application shall handle a negative `DelayMs` value according to the project's finalized validation contract.

### E3 — Missing or blank drone name

The application shall handle a missing, empty, or whitespace-only drone name according to the finalized validation contract.

### E4 — Unknown drone

For Part D route lookup, an unknown drone name shall be handled as not found.

### E5 — Temporary control-tower failure

The application shall handle a control-tower HTTP failure or timeout according to the finalized Part D error contract when Part D is in scope.

---

# 4. Conditional Part D requirements

Part D is optional in the assignment. This project currently includes the local-service alternative in its final target scope.

### PD1 — Local control-tower service

The project shall provide a local control-tower service using the `HttpListener` option supplied by the assignment.

### PD2 — Route data

The control tower shall provide route information through:

`GET /route?drone=Navn`

### PD3 — Weather data

The control tower shall provide weather information through:

`GET /weather`

### PD4 — Asynchronous HTTP consumption

The client shall use one reusable `HttpClient` and asynchronous HTTP APIs.

### PD5 — Simulation effect

Retrieved control-tower data shall affect the final simulation configuration.

### PD6 — HTTP failure handling

Non-success HTTP responses and connection-level request failures shall produce the documented control-tower failure behaviour.

### PD7 — Timeout handling

HTTP request timeouts shall produce the documented timeout behaviour.

### PD8 — Non-blocking HTTP flow

The client and local server shall avoid synchronous blocking in the asynchronous HTTP path.

### PD9 — Restrictions

The project shall provide temporary restriction data through the optional:

`GET /restrictions`

endpoint.

### PD10 — HTTP lifecycle logging

HTTP request start and completion/failure shall be observable in the final Part D implementation.

### PD11 — Sequential versus concurrent HTTP calls

The project shall support comparison of sequential and concurrent control-tower requests using equivalent functional results.

### PD12 — Variable response time

The local control tower shall be able to vary response time to simulate slow network conditions. Exact elapsed duration is not a correctness requirement.

### PD13 — Drone registration

Drone registration remains a bonus extension and is not part of the final target scope.

---

# 5. Technical requirements

The mandatory execution mechanisms are:

- `Thread`
- `Thread.Join`
- `Task`
- `TaskCompletionSource`
- `Task.WhenAll`
- `async`/`await`
- `Task.Delay`
- `try/catch`

When Part D is implemented, `HttpClient` and asynchronous HTTP APIs are required.

---

# 6. Bonus functionality

The following remain optional extras:

- `CancellationToken` support;
- retry/backoff around HTTP calls;
- `IAsyncEnumerable` for streamed progress;
- drone registration.

They are not prerequisites for completion.
