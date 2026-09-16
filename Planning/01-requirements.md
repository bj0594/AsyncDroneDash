# Async Drone Dash — Requirements

## Project objective

Async Drone Dash is a C# console application that simulates multiple delivery drones flying routes.

The project demonstrates and compares three approaches to concurrent/asynchronous execution:

1. `Thread` + `Join`
2. `Task` + `TaskCompletionSource`
3. `async`/`await`

Part D is an optional extension involving asynchronous HTTP communication with a control-tower service.

The simulation is intentionally simple. The main purpose is to make the differences between the execution models observable and understandable.

---

## Functional requirements

### Core simulator

- [MUST] Simulate multiple delivery drones flying routes.
- [MUST] Each drone has `Name`, `MaxCheckpoints`, and `DelayMs`.
- [MUST] Each drone progresses through its checkpoints.
- [MUST] The configured delay is applied between checkpoint steps.
- [MUST] Drone progress is reported.

### Part A — Thread Race

- [MUST] Start at least two drones concurrently.
- [MUST] Run each drone on its own `Thread`.
- [MUST] Count each drone from `0` to `MaxCheckpoints`.
- [MUST] Log drone start.
- [MUST] Log each checkpoint reached.
- [MUST] Log drone completion.
- [MUST] Use `Join` to wait for all drone threads before reporting overall completion.
- [MUST] Demonstrate the effect of removing `Join`.
- [MUST] Demonstrate that concurrent console output can be interleaved or non-deterministic.

### Part B — Task + TaskCompletionSource

- [MUST] Represent drone flights using `Task`.
- [MUST] Use one `TaskCompletionSource` per drone.
- [MUST] Start at least two drones.
- [MUST] Use `Task.WhenAll` to coordinate the drone tasks.
- [MUST] Demonstrate at least one failure scenario.
- [MUST] Propagate the failure through the task/TCS model.
- [MUST] Demonstrate task-based exception handling, including `Task.Exception`.

### Part C — Async/Await

- [MUST] Implement drone flight as an `async` method.
- [MUST] Use `await Task.Delay` for checkpoint delays.
- [MUST] Use `await Task.WhenAll` to coordinate multiple drone flights.
- [MUST] Use `try/catch` around orchestration for error reporting.
- [MUST] Allow comparison with Part B regarding boilerplate, complexity, readability, and maintainability.

### Part D — Control Tower API

Part D is explicitly optional.

- [MAY] Consume route information from an HTTP service.
- [MAY] Consume weather information from an HTTP service.
- [MAY] Consume temporary restrictions from an HTTP service.
- [MAY] Use an external demo API or a local HTTP service.
- [MAY] Use retrieved data to affect `DelayMs` or `MaxCheckpoints`.
- [MAY] Handle HTTP failures and timeouts with appropriate error messages.
- [MAY] Log HTTP request start and completion.
- [MAY] Compare concurrent and sequential HTTP calls.

---

## Non-functional requirements

The assignment does not state explicit non-functional requirements.

The following are learning/observation goals rather than additional system requirements:

- concurrency should be observable in the output;
- differences between execution models should be understandable;
- failures should be observable;
- the execution models should be comparable;
- concurrent console output should make overlap visible.

---

## Technical requirements

- [MUST] Runnable C# console application.
- [MUST] Part A uses `Thread`.
- [MUST] Part A uses `Thread.Join`.
- [MUST] Part B uses `Task`.
- [MUST] Part B uses `TaskCompletionSource`.
- [MUST] Part B uses `Task.WhenAll`.
- [MUST] Part B demonstrates task exception propagation.
- [MUST] Part C uses `async`/`await`.
- [MUST] Part C uses `Task.Delay`.
- [MUST] Part C uses `Task.WhenAll`.
- [MUST] Part C uses `try/catch`.
- [MUST] `DroneModel` contains `Name : string`, `MaxCheckpoints : int`, and `DelayMs : int`, unless a justified adaptation is made.
- [MAY] Part D may use `HttpClient`.
- [MAY] A local HTTP alternative may use `HttpListener`.
- [MAY] HTTP calls may use async APIs such as `GetAsync` or `ReadFromJsonAsync`.

---

## Delivery requirements

- [MUST] GitHub repository.
- [MUST] Runnable console application.
- [MUST] Menu for Parts A–D.
- [MUST] `reflection.md` containing observations, short answers, and other relevant thoughts.
- [MUST] `README.md` containing running instructions.
- [MUST] `README.md` explains how each part is tested.
- [MUST] `README.md` explains how to start a local HTTP service if one is included.

---

## Requirement validation

The mandatory Parts A–C requirements are sufficiently clear to continue planning.

No direct conflict has been identified among the mandatory Parts A–C requirements.

The following areas remain open because the assignment does not define the exact behaviour:

- handling of negative `DelayMs`;
- handling of negative `MaxCheckpoints`;
- meaning and handling of missing/unknown drone names;
- exact Part B failure scenario;
- exact behaviour of the A–D menu when Part D is not implemented;
- exact Part D design.

The requirements are generally verifiable through a combination of automated tests, manual observation, and implementation inspection. Exact thread scheduling and console ordering are not deterministic contracts.

---

## Clarifications and assumptions

### Checkpoint range

“Count from `0` to `MaxCheckpoints`” is interpreted as an inclusive range.

Example:

`MaxCheckpoints = 3` → `0 → 1 → 2 → 3`

### Delay placement

“Delay between steps” is interpreted as a delay between consecutive checkpoints.

There is no required delay before checkpoint `0` or after the final checkpoint.

### Part B failure

The failure used for the Part B demonstration should be deterministic so that it can be reproduced and tested. The exact trigger is still open.

### Part D

Part D is deferred until the MVP for Parts A–C is working.

---

## Technical uncertainty

The following may require official documentation or a small spike before the related design is finalized:

- `TaskCompletionSource` completion and fault behaviour;
- `Task.WhenAll` failure behaviour;
- `Task.Exception` observation;
- exception propagation through `await Task.WhenAll`;
- deterministic testing of concurrent behaviour;
- separating testable output behaviour from visual console observation.

Any research decision that changes the design should be recorded in the relevant planning file.

---

## Open decisions

| Decision | Status |
|---|---|
| Negative `DelayMs` handling | TBD |
| Negative `MaxCheckpoints` handling | TBD |
| Missing/unknown drone handling | TBD |
| Part B failure trigger | TBD |
| Part D implementation | Deferred |
| Part D menu behaviour when unimplemented | TBD |
