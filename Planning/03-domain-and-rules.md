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
| `DelayMs` | `int` | Delay between checkpoint steps |

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

`MaxCheckpoints = 3` → `0 → 1 → 2 → 3`

`DelayMs` applies between consecutive checkpoint steps.

There is no required delay before checkpoint `0` or after the final checkpoint.

---

## Validation rules

| Situation | Contract |
|---|---|
| `Name == null` | `ArgumentException` |
| `Name == ""` | `ArgumentException` |
| whitespace-only `Name` | `ArgumentException` |
| `MaxCheckpoints < 0` | `ArgumentOutOfRangeException` |
| `MaxCheckpoints == 0` | Valid |
| `DelayMs < 0` | `ArgumentOutOfRangeException` |
| `DelayMs == 0` | Valid |
| `DelayMs > 0` | Valid |

Validation is enforced at the flight boundary.

---

## Core behaviours

### B1 — Valid drone configuration

A valid drone configuration can be used for a normal flight.

### B2 — Checkpoint progression

A successful flight reports `0..MaxCheckpoints`.

### B3 — Checkpoint order

Checkpoints are ascending, with no skipped or duplicated checkpoint and no value above `MaxCheckpoints`.

### B4 — Checkpoint delay

`DelayMs` is applied between consecutive checkpoint steps.

### B5 — Flight lifecycle

A successful flight reports `Started`, checkpoint events, and `Completed`.

---

## Part A behaviours

### B6 — Concurrent thread flights

At least two drones perform their flights concurrently on separate `Thread` instances.

### B7 — Joined completion

With `Join`, overall completion is reported only after all required threads finish.

### B8 — No-Join behaviour

Without `Join`, the main thread can continue before all drone threads finish.

### B9 — Non-deterministic console output

Concurrent console output may be interleaved or appear in different orders.

---

## Part B behaviours

### B10 — Task-based flight completion

Each drone flight has a `Task` representing completion or failure.

### B11 — Individual completion signalling

Each participating drone has one `TaskCompletionSource`.

### B12 — Combined task completion

Multiple drone tasks are coordinated with `Task.WhenAll`.

### B13 — Deterministic failure

The selected Part B scenario produces:

`InvalidOperationException("Simulated drone failure.")`

The failure is introduced by the scenario rather than by a permanent `DroneModel` property.

### B14 — Failure propagation

The faulted TCS/task reaches the orchestration layer.

### B15 — Task.Exception observation

`Task.Exception` is an `AggregateException` containing the simulated failure.

---

## Part C behaviours

### B16 — Async flight

A drone flight is represented by an asynchronous `Task` operation.

### B17 — Async checkpoint delay

Checkpoint delays use `await Task.Delay`.

### B18 — Concurrent async flights

Multiple drone flights can make overlapping progress.

### B19 — Combined async completion

Multiple async flights are coordinated using `await Task.WhenAll`.

### B20 — Async failure handling

An async flight failure reaches orchestration and is handled with `try/catch`.

---

## Part D — Optional project target

Part D uses the local `HttpListener` alternative supplied by the assignment and `HttpClient` for the client.

### Endpoints

```text
GET /route?drone=Navn
GET /weather
GET /restrictions
```

`/restrictions` is the project's selected extension of the minimum local API described by the assignment.

### Route JSON

```json
{
  "maxCheckpoints": 3
}
```

Rules:

- property is required;
- value is integer;
- value must be `>= 0`;
- it is the base route checkpoint count.

An unknown route drone returns HTTP `404`.

### Weather JSON

```json
{
  "condition": "storm"
}
```

Supported values:

| Condition | Delay adjustment |
|---|---:|
| `clear` | `+0 ms` |
| `wind` | `+250 ms` |
| `storm` | `+500 ms` |

Unknown values are invalid.

### Restriction JSON

With restriction:

```json
{
  "maxCheckpoints": 2
}
```

Without an active restriction:

```json
{
  "maxCheckpoints": null
}
```

The value is either null or a non-negative integer.

### Final configuration

Without restriction:

`FinalMaxCheckpoints = RouteMaxCheckpoints`

With restriction:

`FinalMaxCheckpoints = min(RouteMaxCheckpoints, RestrictionMaxCheckpoints)`

`FinalDelayMs = Drone.DelayMs + Weather.DelayAdjustmentMs`

The final values must satisfy the core validation rules.

### ControlTowerException

The client translates dependency failures into:

- `RequestFailed` — non-success response except not-found, or connection-level failure;
- `NotFound` — requested route/drone was not found;
- `Timeout` — request exceeded configured timeout;
- `InvalidResponse` — malformed or invalid response data.

The original exception is preserved where useful.

### Variable response time

The local service can vary response time to simulate slow network conditions.

Exact elapsed duration is not a correctness rule.

### HTTP client lifetime

`ControlTowerClient` reuses one `HttpClient` for all requests.

### Local server execution

The local `HttpListener` request loop uses asynchronous request handling and does not use blocking `GetContext()` as its normal request loop.

---

## Optional features

- cancellation;
- retry/backoff;
- `IAsyncEnumerable`;
- drone registration.
