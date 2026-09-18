# Async Drone Dash — Behaviour Design

## 1. Visualizations

### 1.1 Common drone-flight flow

```text
Start
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

The checkpoint range is inclusive:

`0 → 1 → ... → MaxCheckpoints`

The configured delay is applied between checkpoint steps.

---

### 1.2 Part A — Join versus no Join

```text
Start all drone Threads
        ↓
   Drone flights
        ↓
     Join all
        ↓
Overall completion
```

No-Join demonstration:

```text
Start all drone Threads
        ↓
Main thread continues
        ↓
Overall completion may be reported
        ↓
Drone Threads continue independently
```

The exact concurrent output order is not deterministic.

---

### 1.3 Part A–C execution-model comparison

```text
Part A
Drone → Thread ─┐
Drone → Thread ─┼→ Join
Drone → Thread ─┘

Part B
Drone → Task/TCS ─┐
Drone → Task/TCS ─┼→ Task.WhenAll
Drone → Task/TCS ─┘

Part C
Drone → async flight ─┐
Drone → async flight ─┼→ await Task.WhenAll
Drone → async flight ─┘
```

The three implementations deliberately remain visibly different.

---

# 2. Pseudocode

## 2.1 Common drone flight

```text
Validate drone configuration

Report start

Set checkpoint to 0
Report checkpoint 0

While another checkpoint remains:
    Wait DelayMs
    Advance to next checkpoint
    Report checkpoint

Report completion
```

For `MaxCheckpoints = 0`, checkpoint `0` is reported and the flight can complete without an intermediate delay.

---

## 2.2 Part A

```text
Prepare drones

Create one Thread per drone
Start all Threads

Join every Thread

Report overall completion
```

No-Join variant:

```text
Prepare drones

Create and start Threads

Do not Join

Allow the main thread to continue
```

---

## 2.3 Part B

```text
Prepare drones

For each drone:
    Create one TaskCompletionSource
    Start its drone work

When a drone completes:
    Complete its TCS

When the selected failure occurs:
    Fault its TCS

Coordinate the resulting Tasks with Task.WhenAll

Observe success or failure

For the failure demonstration:
    Observe Task.Exception
```

The exact failure trigger remains open.

---

## 2.4 Part C

```text
Prepare drones

Start the async flight for each drone

Each flight:
    Report start
    Report checkpoint 0

    While another checkpoint remains:
        Await Task.Delay(DelayMs)
        Advance checkpoint
        Report checkpoint

    Report completion

Await Task.WhenAll

If orchestration fails:
    Catch and report the exception
```

The async flow must not use `.Wait()` or `.Result`.

---

## 2.5 Part D — Optional

```text
Request control-tower data

Obtain route information
Obtain weather information
Obtain temporary restrictions

Combine the returned data

Produce the final simulation configuration

Run the drone flight
```

The exact API/data mapping remains subject to the final Part D design.

---

# 3. Vertical behaviours

## 3.1 Core drone behaviour

### VB01 — Accept valid drone configuration

A valid `DroneModel` can be used for a normal flight.

Related domain behaviour: `B1`

### VB02 — Handle negative MaxCheckpoints

A negative `MaxCheckpoints` value is handled according to the final validation contract.

Related domain rule: configuration validation.

### VB03 — Handle negative DelayMs

A negative `DelayMs` value is handled according to the final validation contract.

Related domain rule: configuration validation.

### VB04 — Handle missing or blank drone name

A missing, empty, or whitespace-only name is handled according to the final validation contract.

Related domain rule: configuration validation.

### VB05 — Report checkpoint 0

A valid flight reports checkpoint `0`.

Related domain behaviour: `B2`

### VB06 — Progress through all checkpoints

A successful flight reports every checkpoint from `0` through `MaxCheckpoints` in ascending order.

Related domain behaviours: `B2`, `B3`

### VB07 — Complete after the final checkpoint

A successful flight reports completion only after its final checkpoint.

Related domain behaviour: `B5`

---

## 3.2 Part A

### VB08 — Execute multiple drone flights concurrently

At least two drones execute using separate `Thread` instances.

Related domain behaviour: `B6`

### VB09 — Wait for all drones with Join

The normal Part A run reports overall completion only after all required drone threads have finished.

Related domain behaviour: `B7`

### VB10 — Demonstrate execution without Join

The no-Join variant allows the main thread to continue before all drone threads have finished.

Related domain behaviour: `B8`

### VB11 — Demonstrate non-deterministic concurrent output

Concurrent drone output can become interleaved or appear in different orders.

Related domain behaviour: `B9`

---

## 3.3 Part B

### VB12 — Represent drone completion with Task

A Part B drone flight is represented by a `Task`.

Related domain behaviour: `B10`

### VB13 — Use one TaskCompletionSource per drone

Each participating drone has its own `TaskCompletionSource`.

Related domain behaviour: `B11`

### VB14 — Coordinate tasks with Task.WhenAll

Multiple drone tasks are coordinated with `Task.WhenAll`.

Related domain behaviour: `B12`

### VB15 — Produce a deterministic failure

The selected Part B failure condition causes the affected drone operation to become faulted.

Related domain behaviour: `B13`

### VB16 — Propagate task failure

A failed drone operation propagates its failure to the orchestration level.

Related domain behaviour: `B14`

### VB17 — Observe Task.Exception

The Part B failure can be observed through `Task.Exception`.

Related domain behaviour: `B15`

---

## 3.4 Part C

### VB18 — Execute an async drone flight

A valid drone can complete through an asynchronous flight operation.

Related domain behaviour: `B16`

### VB19 — Perform checkpoint delays asynchronously

Checkpoint delays use `await Task.Delay`.

Related domain behaviour: `B17`

### VB20 — Execute multiple async flights concurrently

Multiple drone flights can progress concurrently.

Related domain behaviour: `B18`

### VB21 — Coordinate async flights with Task.WhenAll

Multiple async flights are coordinated with `await Task.WhenAll`.

Related domain behaviour: `B19`

### VB22 — Handle async flight failure

A failed async flight reaches orchestration and is handled with `try/catch`.

Related domain behaviour: `B20`

---

## 3.5 Part D — Optional target

These behaviours are conditional on Part D entering the implementation scope.

### VB-D01 — Retrieve route data

The application can retrieve route information from the control-tower API.

### VB-D02 — Retrieve weather data

The application can retrieve weather information.

### VB-D03 — Retrieve temporary restrictions

The application can retrieve temporary restriction information.

### VB-D04 — Apply retrieved data

Retrieved control-tower data can affect `DelayMs` and/or `MaxCheckpoints` according to the final documented mapping.

### VB-D05 — Handle HTTP failure

A control-tower failure produces the documented failure behaviour.

### VB-D06 — Handle HTTP timeout

A control-tower timeout produces the documented timeout behaviour.

### VB-D07 — Log HTTP activity

HTTP request start/completion or failure is observable if logging is implemented.

### VB-D08 — Compare sequential and concurrent HTTP calls

Sequential and concurrent calls can be compared using equivalent functional results.

---

# 4. Behaviour relationships

```text
VB01–VB07
      ↓
Core drone flight
   ↙    ↓    ↘
Part A Part B Part C
   \
    └── Part D can provide optional external input
```

The three mandatory execution models reuse the same basic drone-flight problem.

Part D extends the input/control side without replacing the core flight model.

---

# 5. First development candidate

### VB05 — Report checkpoint 0

Given a valid drone, when the basic flight starts, checkpoint `0` is observable.

Traceability:

`R3 → AC-CORE-2 → VB05`

The detailed test specification is maintained in `AsyncDroneDash.Tests/TestPlan.md`.

---

# 6. Open design points

- exact validation contract for negative `MaxCheckpoints`;
- exact validation contract for negative `DelayMs`;
- exact handling of missing/unknown drone names;
- exact Part B failure trigger;
- final Part D API/data contract;
- final Part D data-to-simulation mapping.

---

# 7. Design status

### Visualizations

- [x] Common flight flow.
- [x] Part A Join/no-Join flow.
- [x] Part A–C execution-model comparison.

### Pseudocode

- [x] Common flight.
- [x] Part A.
- [x] Part B.
- [x] Part C.
- [x] Part D outline.

### Behaviours

- [x] Core behaviours.
- [x] Part A behaviours.
- [x] Part B behaviours.
- [x] Part C behaviours.
- [x] Optional Part D behaviours.
- [x] First behaviour identified.

### Remaining

- [ ] Resolve open contracts when their dependent test/design requires them.