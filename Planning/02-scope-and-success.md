# Async Drone Dash — Scope and Success

## MVP scope

The MVP is the smallest version that satisfies the mandatory learning and delivery requirements of the assignment without optional HTTP or bonus functionality.

### IN SCOPE

#### Core

- Runnable C# console application.
- Multiple delivery drones using the required `DroneModel` data.
- Checkpoint progression from `0` to `MaxCheckpoints`.
- `DelayMs` between checkpoint steps.
- Start, checkpoint, and completion logging.
- Menu providing access to Parts A–D.

#### Part A

- At least two concurrent drones.
- One `Thread` per drone.
- `Thread.Join` for the normal run.
- A no-`Join` demonstration.
- Observable concurrent/interleaved console output.

#### Part B

- `Task`-based drone flights.
- One `TaskCompletionSource` per drone.
- At least two drones.
- `Task.WhenAll`.
- One deterministic failure scenario.
- Failure propagation and task exception observation.

#### Part C

- Async drone-flight method.
- `await Task.Delay`.
- `await Task.WhenAll`.
- `try/catch` around orchestration.
- Comparison with Part B.

#### Delivery

- GitHub repository.
- `README.md`.
- `reflection.md`.
- Automated tests for behaviours that are meaningful and deterministic to test.

---

## OUT OF SCOPE for MVP

- Part D implementation.
- External API integration.
- Local HTTP service.
- Weather/restriction system.
- HTTP timeout/retry implementation.
- `CancellationToken`.
- Retry/backoff.
- `IAsyncEnumerable`.
- Database/persistence.
- Authentication.
- Complex physical flight simulation.
- Route optimization.
- Unnecessary architectural layers or abstractions.

These exclusions protect Parts A–C from optional complexity.

---

## LATER / POSSIBLE EXTENSIONS

After the MVP is working:

- Part D using an external demo API.
- Part D using a local `HttpListener` service.
- Route, weather, and restriction data.
- Control-tower data affecting `DelayMs` or `MaxCheckpoints`.
- HTTP failure and timeout handling.
- HTTP request logging.
- Sequential vs concurrent HTTP comparison.
- `CancellationToken`.
- Retry/backoff.
- `IAsyncEnumerable`.

Optional work is started only after the MVP can be demonstrated independently.

---

## Definition of MVP success

The MVP is complete when:

- Parts A–C satisfy their mandatory requirements.
- The required execution mechanisms are demonstrable.
- Part B has a reproducible failure scenario with observable task failure propagation.
- The concurrency differences required by the assignment can be observed.
- The application builds and runs.
- The menu works.
- Relevant automated tests pass.
- `README.md` contains the required running/testing information.
- `reflection.md` contains the required observations and answers.
- The project is stored in GitHub.

Part D and bonuses are not needed to declare the MVP complete.

---

## Acceptance criteria

### Core

**AC-CORE-1 — Drone configuration**

A drone can be represented using the required `Name`, `MaxCheckpoints`, and `DelayMs` values.

**AC-CORE-2 — Checkpoint progression**

For `MaxCheckpoints = 3`, a successful flight reports checkpoints `0`, `1`, `2`, and `3` in ascending order.

**AC-CORE-3 — Flight reporting**

A successful flight reports its start, checkpoint progress, and completion.

### Part A

**AC-A1 — Concurrent drones**

At least two drones execute using separate `Thread` instances.

**AC-A2 — Join waits for completion**

With `Join` enabled, overall completion is not reported until all required drone threads have finished.

**AC-A3 — Delay**

A configured valid delay is applied between checkpoint steps.

**AC-A4 — No-Join demonstration**

Without `Join`, the demonstration can show the main thread continuing before all drone work has completed.

**AC-A5 — Concurrent output**

Concurrent execution can produce interleaved or otherwise non-deterministic console output.

### Part B

**AC-B1 — Task-based flight**

Each drone flight is represented by a `Task`.

**AC-B2 — Per-drone TCS**

Each participating drone has its own `TaskCompletionSource`.

**AC-B3 — Task coordination**

Multiple drone tasks are coordinated with `Task.WhenAll`.

**AC-B4 — Failure**

The selected failure scenario causes the affected task to become faulted.

**AC-B5 — Failure propagation**

The failure reaches the orchestration level rather than being silently ignored.

**AC-B6 — Task.Exception**

The relevant fault information can be observed through `Task.Exception`.

### Part C

**AC-C1 — Async flight**

A drone flight is represented by an asynchronous operation.

**AC-C2 — Async delay**

Checkpoint waiting uses `await Task.Delay`.

**AC-C3 — Async coordination**

Multiple drone flights are coordinated with `await Task.WhenAll`.

**AC-C4 — Error handling**

A flight failure reaches orchestration and is handled with `try/catch`.

**AC-C5 — Comparison**

The implementation and reflection provide enough evidence to compare Part B and Part C regarding boilerplate, complexity, readability, and maintainability.

### Delivery

**AC-DLV-1 — Runnable project**

The repository contains a runnable C# console application.

**AC-DLV-2 — Menu**

The application provides access to Parts A–D.

**AC-DLV-3 — README**

`README.md` contains running instructions and explains how each part is tested.

**AC-DLV-4 — Reflection**

`reflection.md` contains the required observations, short answers, and other relevant thoughts.

**AC-DLV-5 — GitHub**

The project is stored in a GitHub repository.

---

## Acceptance-criteria verification

Criteria will later be classified as:

- automated test;
- manual observation;
- implementation inspection;
- reflection/documentation.

Exact test scenarios, test levels, test names, Fact/Theory choices, and test oracles belong in `testplan.md`.

---

## Open scope decisions

- Exact handling of invalid drone configuration.
- Exact Part B failure trigger.
- Exact treatment of Part D in the menu if Part D remains unimplemented.
