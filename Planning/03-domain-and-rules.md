# Async Drone Dash — Domain and Rules

## Domain overview

Async Drone Dash models multiple delivery drones progressing through simple checkpoint-based routes.

The domain is intentionally small because the project is primarily a demonstration of concurrent and asynchronous execution.

The core domain does not model physical movement, geography, fuel, battery, packages, customers, route optimization, or realistic weather simulation.

---

## DroneModel

| Property | Type | Meaning |
|---|---|---|
| `Name` | `string` | Identifies the drone |
| `MaxCheckpoints` | `int` | Highest checkpoint the drone should reach |
| `DelayMs` | `int` | Delay between checkpoint steps in milliseconds |

No additional properties are currently required by the MVP.

---

## FlightEvent

A flight produces observable events:

- `Started`
- `CheckpointReached`
- `Completed`
- `Faulted`

A `CheckpointReached` event contains the checkpoint number.

The event model provides a deterministic observation boundary for tests and orchestration. The core flight logic does not write directly to the console.

---

## Flight behaviour

A valid drone flight progresses from checkpoint `0` through `MaxCheckpoints`, inclusive.

Example:

`MaxCheckpoints = 3` → `0 → 1 → 2 → 3`

`DelayMs` applies between consecutive checkpoint steps.

A normal flight therefore follows:

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

The selected demonstration failure is:

`InvalidOperationException("Simulated drone failure.")`

The failure is introduced by the Part B scenario rather than by adding a permanent failure property to `DroneModel`.

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

## Part D — Control Tower

Part D is optional in the assignment but is currently a project target.

The project uses a local `HttpListener` service as the control tower and `HttpClient` as the client.

The control tower provides three categories of data:

### Route data

Provides the base `MaxCheckpoints` for a drone route.

### Weather data

Provides weather information that can affect the final `DelayMs`.

The initial supported conditions are:

- `clear`
- `wind`
- `storm`

The exact delay adjustments are project decisions and are kept in the Part D design rather than in the core `DroneModel`.

### Restriction data

Provides an optional maximum checkpoint restriction.

A restriction may reduce the route's usable `MaxCheckpoints`, but must not increase it.

---

## Part D rules

The final simulation configuration is derived from the control-tower data.

### Checkpoints

Without a restriction:

`FinalMaxCheckpoints = RouteMaxCheckpoints`

With a restriction:

`FinalMaxCheckpoints = min(RouteMaxCheckpoints, RestrictionMaxCheckpoints)`

### Delay

Weather may modify the drone's configured `DelayMs`.

The resulting delay must still satisfy the normal `DelayMs >= 0` rule.

### HTTP independence

Route, weather and restriction requests represent independent data sources and may be requested sequentially or concurrently.

Both approaches must produce equivalent simulation input.

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

- `Name` must not be null, empty, or whitespace.
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
| `MaxCheckpoints < 0` | Reject with `ArgumentOutOfRangeException` |
| `DelayMs < 0` | Reject with `ArgumentOutOfRangeException` |
| Missing/blank name | Reject with `ArgumentException` |
| Unknown drone | Relevant only if a lookup/registry exists |
| Multiple drones | Can execute concurrently |
| Part B failure | Affected operation becomes faulted |
| HTTP non-success response | Control-tower operation fails |
| HTTP timeout | Control-tower operation fails |
| Cancellation | Deferred |

---

## Domain responsibilities

### DroneModel

Represents the configuration of one drone.

### FlightEvent

Represents observable flight progress and outcome.

### Drone flight

Performs one drone's checkpoint progression and produces its progress/outcome.

### Orchestration

Coordinates multiple drone flights and their completion/failure according to Part A, B, or C.

### Console/UI

Starts the selected demonstration and presents its results. It does not own the underlying flight rules.

### Optional HTTP component

Provides control-tower data for Part D. It does not own drone orchestration.

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
- route optimization.

Part D may introduce route, weather and restriction data without turning the core drone model into a full physical simulation.

---

## Open domain decisions

- Exact Part D response/data contract.
- Exact Part D public exception contract.
- Exact treatment of an unknown drone if a lookup mechanism is introduced.
- Final Part D orchestration API.
- Exact weather-to-delay mapping.

Optional features such as cancellation, retry/backoff and `IAsyncEnumerable` remain outside the current scope.