# Async Drone Dash — Domain and Rules

## Domain overview

Async Drone Dash models multiple delivery drones progressing through simple checkpoint-based routes.

The domain is intentionally small because the project primarily demonstrates concurrent and asynchronous execution.

The core domain does not model physical movement, geography, fuel, battery, packages, customers, or route optimization.

---

## DroneModel

| Property | Type | Meaning |
|---|---|---|
| `Name` | `string` | Identifies the drone |
| `MaxCheckpoints` | `int` | Highest checkpoint the drone should reach |
| `DelayMs` | `int` | Delay between checkpoint steps in milliseconds |

No additional properties are required by the MVP.

---

## FlightEvent

A flight produces observable events:

- `Started`
- `CheckpointReached`
- `Completed`
- `Faulted`

A `CheckpointReached` event contains the checkpoint number.

The event model provides a deterministic observation boundary for tests and orchestration. Core flight logic does not write directly to the console.

---

## Flight behaviour

A valid drone flight progresses from checkpoint `0` through `MaxCheckpoints`, inclusive.

Example:

`MaxCheckpoints = 3` → `0 → 1 → 2 → 3`

`DelayMs` applies between consecutive checkpoint steps.

A normal flight therefore follows:

`Started → Checkpoint 0 → Delay → Checkpoint 1 → ... → Checkpoint MaxCheckpoints → Completed`

There is no required delay before checkpoint `0` or after the final checkpoint.

---

## Core behaviours

### B1 — Valid drone configuration

A valid drone configuration can be used for a normal flight.

### B2 — Checkpoint progression

A successful flight reports checkpoints from `0` through `MaxCheckpoints`.

### B3 — Checkpoint order

A successful flight reports checkpoints in ascending order and never reports a checkpoint above `MaxCheckpoints`.

### B4 — Checkpoint delay

The configured delay is applied between consecutive checkpoint steps.

### B5 — Flight lifecycle output

A successful flight reports its start, checkpoint progress, and completion.

---

## Validation rules

| Situation | Rule |
|---|---|
| `Name == null` | Reject with `ArgumentException` |
| `Name == ""` | Reject with `ArgumentException` |
| `Name` is whitespace | Reject with `ArgumentException` |
| `MaxCheckpoints < 0` | Reject with `ArgumentOutOfRangeException` |
| `MaxCheckpoints == 0` | Valid |
| `DelayMs < 0` | Reject with `ArgumentOutOfRangeException` |
| `DelayMs == 0` | Valid |
| `DelayMs > 0` | Valid |

Validation is enforced at the flight boundary rather than by adding behaviour-specific logic to `DroneModel`.

---

## Part A behaviours

### B6 — Concurrent thread flights

At least two drones perform their flights concurrently on separate `Thread` instances.

### B7 — Joined completion

With `Join`, overall completion is reported only after all required drone threads have finished.

### B8 — No-Join behaviour

Without `Join`, the main thread can continue before all drone threads have completed.

### B9 — Non-deterministic console output

Concurrent drone output may appear interleaved or in different orders between runs.

The exact scheduling/output order is not a domain contract.

---

## Part B behaviours

### B10 — Task-based flight completion

Each drone flight has a `Task` representing its completion or failure.

### B11 — Individual completion signalling

Each participating drone has its own `TaskCompletionSource`.

### B12 — Combined task completion

Multiple drone tasks are coordinated with `Task.WhenAll`.

### B13 — Deterministic failure

Part B uses a selected drone/checkpoint failure scenario to produce a deterministic fault.

The failure exception is:

`InvalidOperationException("Simulated drone failure.")`

The failure is introduced by the Part B scenario rather than by adding a permanent failure property to `DroneModel`.

### B14 — Failure propagation

The failed operation propagates its failure through the TCS/Task model to orchestration.

### B15 — Task exception observation

The faulted task exposes the expected exception information through `Task.Exception`.

`Task.Exception` is expected to be an `AggregateException` containing the underlying simulated failure.

---

## Part C behaviours

### B16 — Async flight

A drone flight is represented by an asynchronous operation returning `Task`.

### B17 — Async checkpoint delay

Checkpoint delays use `await Task.Delay`.

### B18 — Concurrent async flights

Multiple drone flights can make meaningful overlapping progress rather than merely completing eventually.

### B19 — Combined async completion

Multiple async flights are coordinated using `await Task.WhenAll`.

### B20 — Async failure handling

An async flight failure reaches orchestration and is handled with `try/catch`.

---

## Part D — Optional target

Part D is optional in the assignment but is currently a project target.

The project uses the local `HttpListener` alternative supplied by the assignment and an asynchronous `HttpClient` client.

### Control-tower endpoints

Required local endpoints for the current target:

```text
GET /route?drone=Navn
GET /weather
```

Optional extension:

```text
GET /restrictions
```

The route endpoint reads the requested drone name from the request query/`RawUrl`.

### Route data

Route data provides the base checkpoint count for the requested drone.

### Weather data

Weather data affects final `DelayMs`.

Current mapping:

| Condition | Delay adjustment |
|---|---:|
| `clear` | `+0 ms` |
| `wind` | `+250 ms` |
| `storm` | `+500 ms` |

Unknown weather values are invalid under the final response contract.

### Restrictions

If included, restriction data provides an optional maximum checkpoint value.

No restriction:

`FinalMaxCheckpoints = RouteMaxCheckpoints`

Restriction present:

`FinalMaxCheckpoints = min(RouteMaxCheckpoints, RestrictionMaxCheckpoints)`

A restriction cannot increase the route checkpoint count.

### Final simulation configuration

```text
FinalMaxCheckpoints = route maximum, optionally capped by restriction
FinalDelayMs = Drone.DelayMs + Weather.DelayAdjustmentMs
```

The resulting configuration must still satisfy the core validation rules.

### Variable response time

The local service can deliberately vary response time to simulate slow network conditions.

Exact elapsed duration is not a correctness rule.

### HTTP failures

The client translates HTTP/dependency failures into a project-specific `ControlTowerException` with these categories:

- `RequestFailed`
- `Timeout`
- `InvalidResponse`

The exact public exception API is documented in `04-design-and-traceability.md`.

### HTTP client lifetime

`ControlTowerClient` reuses one `HttpClient` rather than creating one per request.

### Local server execution

The local `HttpListener` request loop uses asynchronous request handling rather than blocking `GetContext()` calls.

---

## Optional features

The following remain outside mandatory completion:

- `CancellationToken` support;
- retry/backoff;
- `IAsyncEnumerable`;
- drone registration endpoint.