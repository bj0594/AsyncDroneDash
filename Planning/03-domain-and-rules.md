# Async Drone Dash — Domain and Rules

## Domain overview

Async Drone Dash models multiple delivery drones progressing through simple checkpoint-based routes.

The domain is intentionally small because the project is primarily a demonstration of concurrent and asynchronous execution.

The core domain does not model physical movement, geography, fuel, battery, packages, customers, or route optimization.

---

## DroneModel

| Property | Type | Meaning |
|---|---|---|
| `Name` | `string` | Identifies the drone |
| `MaxCheckpoints` | `int` | Highest checkpoint the drone should reach |
| `DelayMs` | `int` | Delay between checkpoint steps in milliseconds |

No additional properties are currently required by the MVP.

---

## Flight behaviour

A valid drone flight progresses from checkpoint `0` through `MaxCheckpoints`, inclusive.

Example:

`MaxCheckpoints = 3` → `0 → 1 → 2 → 3`

`DelayMs` applies between consecutive checkpoint steps.

A normal flight therefore follows the conceptual pattern:

`Start → Checkpoint 0 → Delay → Checkpoint 1 → ... → Checkpoint MaxCheckpoints → Completed`

There is no required delay before checkpoint `0` or after the final checkpoint.

---

## Core behaviours

### B1 — Valid drone configuration

A drone with valid configuration can begin a normal flight.

### B2 — Checkpoint progression

A successful flight reports checkpoints from `0` through `MaxCheckpoints`.

### B3 — Checkpoint order

A successful flight reports checkpoints in ascending order and never reports a checkpoint above `MaxCheckpoints`.

### B4 — Checkpoint delay

The configured delay is applied between checkpoint steps.

### B5 — Flight lifecycle output

A successful flight reports its start, checkpoint progress, and completion.

---

## Part A behaviours

### B6 — Concurrent thread flights

At least two drones can perform their flights concurrently on separate threads.

### B7 — Joined completion

With `Join`, the overall completion indication occurs only after all required drone threads have finished.

### B8 — No-Join behaviour

Without `Join`, the main thread can continue before all drone threads have completed.

### B9 — Non-deterministic console output

Concurrent drone output may appear interleaved or in different orders between runs.

The exact order is not a domain contract.

---

## Part B behaviours

### B10 — Task-based flight completion

Each drone flight has a `Task` representing its completion or failure.

### B11 — Individual completion signalling

Each participating drone has its own `TaskCompletionSource`.

### B12 — Combined task completion

Multiple drone tasks are coordinated using `Task.WhenAll`.

### B13 — Deterministic failure

A defined failure condition causes the affected drone operation to fail.

The exact trigger remains open.

### B14 — Failure propagation

A failed drone operation propagates its failure through the task/TCS model to the orchestration level.

### B15 — Task exception observation

The relevant fault information can be observed through `Task.Exception`.

---

## Part C behaviours

### B16 — Async flight

A drone flight is represented by an asynchronous operation.

### B17 — Async checkpoint delay

The delay between checkpoint steps uses `await Task.Delay`.

### B18 — Concurrent async flights

Multiple drone flights can progress concurrently.

### B19 — Combined async completion

Multiple flights are coordinated using `await Task.WhenAll`.

### B20 — Async failure handling

When an async flight fails, the failure reaches orchestration and is handled with `try/catch`.

---

## Flight state

The conceptual lifecycle of a normal flight is:

`NotStarted → Running → Completed`

A failed flight follows:

`NotStarted → Running → Faulted`

Optional cancellation would introduce:

`NotStarted → Running → Cancelled`

Cancellation is outside the MVP.

A formal state-machine implementation is not required.

---

## State transitions

| Event | Before | After |
|---|---|---|
| Flight starts | `NotStarted` | `Running` |
| Final checkpoint completed successfully | `Running` | `Completed` |
| Required failure occurs | `Running` | `Faulted` |
| Optional cancellation | `Running` | `Cancelled` |

`Completed`, `Faulted`, and `Cancelled` are terminal outcomes for that flight run.

---

## Invariants

### Configuration

- `Name` is not blank.
- `MaxCheckpoints >= 0`.
- `DelayMs >= 0`.

### Progress

- A successful flight reports checkpoint `0` first.
- Checkpoints progress in ascending order.
- No successful flight reports a checkpoint greater than `MaxCheckpoints`.
- Completion is reported only after the final checkpoint.
- Delay occurs between checkpoint steps.

### Outcome

- A completed flight does not continue progressing.
- A faulted flight is not reported as successfully completed.
- Overall completion is not reported before the required coordination mechanism has completed.

### Async flow

- Part C does not use synchronous `.Wait()` or `.Result` in its async execution path.

---

## Rules and boundaries

| Situation | Current rule |
|---|---|
| Valid drone configuration | Allow normal flight |
| `MaxCheckpoints = 0` | Report checkpoint `0` and complete |
| `MaxCheckpoints > 0` | Report `0..MaxCheckpoints` in order |
| `MaxCheckpoints < 0` | Must be handled; exact behaviour TBD |
| `DelayMs < 0` | Must be handled; exact behaviour TBD |
| Missing/blank name | Must be handled; exact behaviour TBD |
| Unknown drone | Relevant only if a lookup/registry exists; exact behaviour TBD |
| Multiple drones | Can execute concurrently |
| Part B failure | Affected operation becomes faulted |
| API/weather failure | Relevant only if Part D is implemented |
| Cancellation | Deferred |

---

## Error scenarios

### Invalid configuration

The assignment identifies negative checkpoint/delay values and missing/unknown drone information as cases that should be handled.

The exact response, exception type, and validation location are not yet fixed.

### Part B failure

The failure must be deterministic and observable.

It should produce a faulted task and allow the task/TCS propagation required by the assignment to be demonstrated.

The exact trigger is TBD.

### Part D failure

If Part D is implemented, HTTP/weather failures and timeouts become additional scenarios. Their detailed rules belong to the Part D design.

---

## Domain responsibilities

### DroneModel

Represents the configuration of one drone.

### Drone flight

Performs one drone's checkpoint progression and produces its progress/outcome.

### Orchestration

Coordinates multiple drone flights and their completion/failure according to Part A, B, or C.

### Console/UI

Starts the selected demonstration and presents its results. It does not own the underlying flight rules.

### Optional HTTP component

Provides control-tower data when Part D is implemented. It does not own drone orchestration.

---

## Domain boundaries

The MVP does not contain domain concepts for:

- physical position;
- geography;
- distance;
- fuel;
- battery;
- package/cargo;
- customer;
- route optimization;
- real-world weather simulation.

Part D may introduce route, weather, and restriction data if that extension is implemented.

---

## Open domain decisions

- Exact validation response for negative `MaxCheckpoints`.
- Exact validation response for negative `DelayMs`.
- Exact treatment of missing/unknown drones.
- Exact Part B failure trigger.
- Any additional model required by Part D.
