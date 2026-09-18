# Async Drone Dash — Behaviour Design

## 1. Common flight behaviour

### Flow

```text
Start
  ↓
Validate configuration
  ↓
Report Started
  ↓
Checkpoint 0
  ↓
More checkpoints?
 ├─ No → Completed
 └─ Yes
      ↓
   DelayMs
      ↓
   Next checkpoint
      ↓
   Report checkpoint
      └──→ More checkpoints?
```

Checkpoint progression is inclusive:

`0 → 1 → ... → MaxCheckpoints`

For `MaxCheckpoints = 0`, checkpoint `0` is reported and the flight can complete without an intermediate delay.

---

## 2. Part A — Thread Race

### Flow

```text
Create drones
    ↓
Create one Thread per drone
    ↓
Start all Threads
    ↓
Drone flights execute concurrently
    ↓
Join all Threads
    ↓
Report overall completion
```

No-Join demonstration:

```text
Create and start Threads
        ↓
Do not Join
        ↓
Main thread continues
        ↓
Drone Threads continue independently
```

### Pseudocode

```text
Prepare drones

For each drone:
    Create Thread
    Thread executes DroneFlight.Run

Start all Threads

Join all Threads

Report overall completion
```

### Behaviours

#### B6 — Concurrent Thread execution

At least two drones execute concurrently using separate `Thread` instances.

#### B7 — Joined completion

The normal run does not report overall completion until all required Threads have finished.

#### B8 — No-Join behaviour

The no-Join variant allows the main thread to continue before all drone Threads have finished.

#### B9 — Non-deterministic output

Concurrent console output can become interleaved or appear in different orders.

Exact scheduling order is not a contract.

---

## 3. Part B — Task + TaskCompletionSource

### Flow

```text
Create drones
    ↓
One TaskCompletionSource per drone
    ↓
Start drone work
    ↓
Complete or fault each TCS
    ↓
Task.WhenAll
    ↓
Observe success/failure
```

### Pseudocode

```text
Prepare drones

For each drone:
    Create one TaskCompletionSource
    Start drone work

When the drone completes:
    Complete its TCS

When the selected failure occurs:
    Fault its TCS

Observe Task.WhenAll

Observe success or failure

For the failure demonstration:
    Observe Task.Exception
```

### Behaviours

#### B10 — Task-based flight completion

Each Part B drone flight is represented by a `Task`.

#### B11 — Individual completion signalling

Each participating drone has its own `TaskCompletionSource`.

#### B12 — Task.WhenAll coordination

Multiple drone tasks are coordinated with `Task.WhenAll`.

#### B13 — Deterministic failure

A selected drone/checkpoint failure produces:

`InvalidOperationException("Simulated drone failure.")`

The failure is introduced by the Part B scenario rather than a permanent property on `DroneModel`.

#### B14 — Failure propagation

The failed operation reaches orchestration through the TCS/Task model.

#### B15 — Task.Exception

The faulted task exposes the expected underlying exception through its `Task.Exception` aggregate.

---

## 4. Part C — Async/Await

### Flow

```text
Create drones
    ↓
Start async flights
    ↓
await Task.Delay between checkpoints
    ↓
Checkpoint progression
    ↓
await Task.WhenAll
    ↓
try/catch orchestration
```

### Pseudocode

```text
Prepare drones

Start async flight for each drone

Each flight:
    Report Started
    Report checkpoint 0

    While another checkpoint remains:
        Await Task.Delay(DelayMs)
        Advance checkpoint
        Report checkpoint

    Report Completed

Await Task.WhenAll

If orchestration fails:
    Catch and report the exception
```

The async path must not use `.Wait()` or `.Result`.

### Behaviours

#### B16 — Async flight

A valid drone can complete through an asynchronous flight operation.

#### B17 — Async checkpoint delay

Checkpoint delays use `await Task.Delay`.

#### B18 — Multiple async flights

Multiple async flights can make meaningful overlapping progress.

Completion alone is not sufficient evidence of concurrency.

#### B19 — Async Task.WhenAll

Multiple async flights are coordinated with `await Task.WhenAll`.

#### B20 — Async failure handling

An async flight failure reaches orchestration and is handled with `try/catch`.

---

## 5. Core vertical behaviours

### VB01 — Accept valid drone configuration

`R2 → AC-CORE-2 → B1`

A valid `DroneModel` can be used for normal flight execution.

### VB02 — Reject negative MaxCheckpoints

`R3 → AC-CORE-3 → B2`

A negative `MaxCheckpoints` is rejected according to the validation contract.

### VB03 — Reject negative DelayMs

`R4 → AC-CORE-4 → B4`

A negative `DelayMs` is rejected according to the validation contract.

### VB04 — Reject missing or blank name

`R2 → AC-CORE-2 → B1`

A null, empty, or whitespace-only name is rejected according to the validation contract.

### VB05 — Report checkpoint 0

`R3 → AC-CORE-3 → B2`

Given `MaxCheckpoints = 0`, the flight reports checkpoint `0`.

### VB06 — Progress through all checkpoints

`R3 → AC-CORE-3 → B2/B3`

A successful flight reports every checkpoint from `0` through `MaxCheckpoints` in ascending order.

### VB07 — Apply configured delay

`R4 → AC-CORE-4 → B4`

The configured delay is applied between consecutive checkpoint steps.

### VB08 — Report flight lifecycle

`R5 → AC-CORE-5 → B5`

A successful flight reports start, each checkpoint, and completion.

---

## 6. Part A vertical behaviours

### VB09 — Run multiple drones on separate Threads

`R6 → AC-A1 → B6`

At least two drones execute using separate `Thread` instances.

### VB10 — Join waits for all drones

`R7 → AC-A2 → B7`

Overall completion occurs only after all required drone Threads have finished.

### VB11 — Demonstrate no-Join

`R8 → AC-A3 → B8`

The no-Join variant permits main-thread continuation before all drone Threads finish.

### VB12 — Demonstrate concurrent output

`R9 → AC-A4 → B9`

Concurrent output can be interleaved or otherwise non-deterministic.

---

## 7. Part B vertical behaviours

### VB13 — Complete a drone through Task

`R10 → AC-B1 → B10`

A successful Part B drone operation completes its Task.

### VB14 — Use one TCS per drone

`R11 → AC-B2 → B11`

Each participating drone has an independent completion signal.

### VB15 — Coordinate multiple tasks

`R12 → AC-B3 → B12`

Multiple drone Tasks are coordinated with `Task.WhenAll`.

### VB16 — Produce deterministic failure

`R13 → AC-B4 → B13`

The selected failure scenario faults the affected operation.

### VB17 — Propagate failure

`R14 → AC-B5 → B14`

The failure reaches the orchestration boundary.

### VB18 — Observe Task.Exception

`R15 → AC-B6 → B15`

The failure is observable through `Task.Exception` and the underlying simulated exception.

---

## 8. Part C vertical behaviours

### VB19 — Complete an async flight

`R16 → AC-C1 → B16`

A valid drone completes through an async flight operation.

### VB20 — Delay asynchronously

`R17 → AC-C2 → B17`

Checkpoint delays use `await Task.Delay`.

### VB21 — Make multiple async flights overlap

`R18 → AC-C3 → B18`

At least two async flights make meaningful overlapping progress.

Verification must not rely only on eventual completion.

### VB22 — Await combined completion

`R19 → AC-C4 → B19`

Overall completion is obtained through `await Task.WhenAll`.

### VB23 — Handle async failure

`R20 → AC-C5 → B20`

A failed async flight reaches orchestration and is handled with `try/catch`.

### VB24 — Compare Part B and Part C

`R21 → AC-C6`

The implementations support comparison of boilerplate, complexity, readability, and maintainability.

---

## 9. Part D — Optional vertical behaviours

Part D is active only if retained in final scope.

### B-D01 — Retrieve route data

The control tower provides route information for the requested drone.

### B-D02 — Retrieve weather data

The control tower provides weather information.

### B-D03 — Retrieve temporary restrictions

The control tower provides restriction data when the optional restrictions feature is included.

### B-D04 — Apply control-tower data

Retrieved data changes the final simulation configuration according to the documented mapping.

### B-D05 — Handle HTTP failure

A non-success HTTP response produces the documented `ControlTowerException` failure category.

### B-D06 — Handle timeout

A timeout produces the documented `ControlTowerException.Timeout` behaviour.

### B-D07 — Keep HTTP flow asynchronous

The client consumes the service using asynchronous HTTP APIs and does not synchronously block the request flow.

### B-D08 — Apply restrictions

Restrictions can reduce, but never increase, route `MaxCheckpoints`.

### B-D09 — Observe HTTP lifecycle

If logging remains in final scope, request start and completion/failure are observable.

### B-D10 — Compare sequential and concurrent HTTP calls

Sequential and concurrent requests produce equivalent functional results.

### B-D11 — Variable response time

The local service can produce variable response times to simulate slow network conditions.

Exact response duration is not a correctness oracle.

---

## 10. Part D flow

```text
Route ───────────────┐
Weather ─────────────┼→ Final simulation configuration
Restrictions ────────┘
                       ↓
                   DroneFlight
```

The current local API is:

```text
GET /route?drone=Navn
GET /weather
```

Optional:

```text
GET /restrictions
```

Requests may be sequential or concurrent.

---

## 11. Behaviour relationships

```text
Core
├── B1–B5
│
├── Part A
│   └── B6–B9
│
├── Part B
│   └── B10–B15
│
└── Part C
    └── B16–B20

Optional Part D
└── B-D01–B-D11
```

Parts A–C share the same basic drone-flight problem but use deliberately different execution models.

Part D provides optional control-tower input to the same simulation.

---

## 12. First behaviour

### VB05 — Report checkpoint 0

Traceability:

`R3 → AC-CORE-3 → B2 → VB05`

Scenario:

```text
Given a valid drone with MaxCheckpoints = 0
When the basic flight executes
Then CheckpointReached(0) is observable
```

Current observation boundary:

`Action<FlightEvent>`

The first test verifies the observable event, not console formatting.

---

## 13. Open contracts

- final Part D JSON schemas;
- final Part D public method signatures;
- final restriction response contract;
- whether optional HTTP lifecycle logging remains in final scope.

Core validation, Part B failure, endpoint structure, weather mapping, and HTTP failure categories are already defined in the domain/design documents.