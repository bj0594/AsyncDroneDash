# Async Drone Dash — Behaviour Design

## 1. Visualizations

Only visualizations that clarify a meaningful part of the design are included.

### 1.1 Common drone-flight flow

The three mandatory parts use the same basic flight concept:

```text
Valid drone
    ↓
Start flight
    ↓
Report start
    ↓
Checkpoint 0
    ↓
More checkpoints?
   ↙          ↘
 Yes           No
  ↓             ↓
Delay         Complete
  ↓
Next checkpoint
  ↓
Report checkpoint
  └────────────→ More checkpoints?
```

The checkpoint range is inclusive:

`0 → 1 → ... → MaxCheckpoints`

The configured delay is applied between checkpoint steps.

---

### 1.2 Part A — Join versus no Join

The most important control-flow distinction in Part A is whether the main thread waits for the drone threads.

```text
                Start all drone Threads
                         ↓
              ┌──────────┼──────────┐
              ↓          ↓          ↓
           Drone 1    Drone 2    Drone ...
              │          │          │
              └──────────┼──────────┘
                         ↓
                       Join
                         ↓
              Report overall completion
```

Without `Join`:

```text
                Start all drone Threads
                         ↓
              Main thread continues
                         ↓
              Report overall completion

        Drone threads continue independently
```

The exact order of concurrent console output is not deterministic.

---

### 1.3 Part A–C execution-model comparison

The same basic flight scenario is executed and coordinated differently in the three mandatory parts.

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

This comparison is the main reason the three implementations should remain visibly distinct.

No separate diagram is currently needed for the application menu, Part B failure path, or dependency structure. Those are simple enough to understand from the written design and pseudocode.

---

# 2. Pseudocode

## 2.1 Common drone flight

```text
Validate drone configuration

Start flight
Report start

Set checkpoint to 0
Report checkpoint

While another checkpoint remains:
    Wait DelayMs
    Move to the next checkpoint
    Report checkpoint

Report completion
```

For `MaxCheckpoints = 0`, checkpoint `0` is reported and the flight can complete without an intermediate delay.

The exact validation mechanism is defined later as part of API and test design.

---

## 2.2 Part A

```text
Prepare drones

Create one Thread for each drone
Start all Threads

Join every Thread

Report that all drones are finished
```

The no-`Join` demonstration uses the same setup but intentionally omits the waiting step:

```text
Prepare drones

Create one Thread for each drone
Start all Threads

Report that the main thread continues

Do not wait for the drone Threads
```

---

## 2.3 Part B

```text
Prepare drones

For each drone:
    Create one TaskCompletionSource
    Start the drone work

When a drone succeeds:
    Complete its TCS

When the required failure occurs:
    Fault its TCS

Combine the drone Tasks with Task.WhenAll

Observe successful completion or failure

For the failure demonstration:
    Observe the relevant Task.Exception
```

The exact failure trigger remains a project decision.

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
        Move to the next checkpoint
        Report checkpoint

    Report completion

Await Task.WhenAll

If the combined operation fails:
    Catch the exception
    Report the failure
```

The async flow must not introduce synchronous `.Wait()` or `.Result`.

---

# 3. Vertical behaviours

The following vertical behaviours divide the MVP into small development units.

They are ordered from the basic drone behaviour toward the three execution-model demonstrations.

## 3.1 Core drone behaviour

### VB01 — Accept valid drone configuration

A valid drone configuration can be used for a normal flight.

Related domain behaviour: `B1`

### VB02 — Reject negative MaxCheckpoints

A drone with a negative `MaxCheckpoints` value is rejected before normal flight execution.

Related domain behaviour: `B2`

### VB03 — Reject negative DelayMs

A drone with a negative `DelayMs` value is rejected before normal flight execution.

Related domain behaviour: `B3`

### VB04 — Reject missing or blank drone name

A drone without a usable name is rejected.

Related domain behaviour: `B4`

### VB05 — Report the first checkpoint

When a valid drone starts a flight, checkpoint `0` is observable.

Related domain behaviour: `B5`

### VB06 — Progress through remaining checkpoints

A valid drone progresses through its remaining checkpoints in ascending order until `MaxCheckpoints` is reached.

Related domain behaviours: `B5`, `B6`

### VB07 — Report successful completion

A drone reports completion only after its checkpoint progression has finished.

Related domain behaviour: `B7`

---

## 3.2 Part A — Thread behaviours

### VB08 — Execute multiple drone flights concurrently

When Part A is run with at least two drones, the drone flights execute using separate `Thread` instances.

Related domain behaviour: `B8`

### VB09 — Wait for all drones before overall completion

When the normal Part A execution uses `Join`, the overall completion indication occurs only after the required drone threads have finished.

Related domain behaviour: `B9`

### VB10 — Demonstrate execution without Join

The no-`Join` variant demonstrates that the main thread can continue before all drone threads have finished.

Related domain behaviour: `B10`

### VB11 — Demonstrate non-deterministic concurrent output

Concurrent drone execution can produce interleaved or otherwise non-deterministic console output.

Related domain behaviour: `B11`

---

## 3.3 Part B — Task/TCS behaviours

### VB12 — Represent drone completion with Task

A Part B drone flight has a `Task` representing its completion or failure.

Related domain behaviour: `B12`

### VB13 — Give each drone an individual completion source

Each participating Part B drone has its own `TaskCompletionSource`.

Related domain behaviour: `B13`

### VB14 — Coordinate multiple drone tasks

Multiple drone tasks are coordinated with `Task.WhenAll`.

Related domain behaviour: `B14`

### VB15 — Produce a deterministic task failure

The selected Part B failure condition causes the affected drone operation to become faulted.

Related domain behaviour: `B15`

### VB16 — Propagate task failure

A faulted Part B drone operation propagates its failure to the relevant orchestration boundary.

Related domain behaviour: `B16`

### VB17 — Observe Task.Exception

The Part B demonstration makes the relevant fault information observable through `Task.Exception`.

Related domain behaviour: `B17`

---

## 3.4 Part C — Async behaviours

### VB18 — Execute an async drone flight

A valid drone can complete its flight through an asynchronous operation.

Related domain behaviour: `B18`

### VB19 — Perform checkpoint delays asynchronously

Part C waits asynchronously between checkpoint steps using `Task.Delay`.

Related domain behaviour: `B19`

### VB20 — Execute multiple async flights concurrently

Multiple Part C drone flights can progress concurrently.

Related domain behaviour: `B20`

### VB21 — Coordinate async flights with Task.WhenAll

Part C waits for all participating flights using `await Task.WhenAll`.

Related domain behaviour: `B21`

### VB22 — Handle async flight failure

A failure in a Part C flight reaches the orchestration layer and is handled with `try/catch`.

Related domain behaviour: `B22`

---

# 4. Development relationships

The core drone behaviours provide the foundation for the execution-model work.

```text
VB01–VB07
     ↓
Core drone flight
   ↙   ↓   ↘
Part A Part B Part C
```

The three execution models deliberately reuse the same basic domain problem.

This keeps the comparison focused on how the work is executed and coordinated rather than introducing unrelated domain differences.

The Part A–C branches do not have to be completed strictly one after another if a focused spike or test provides useful information earlier.

---

# 5. First development candidate

The first behaviour should avoid concurrency, thread scheduling, TCS semantics, and real timing where possible.

Current candidate:

### VB05 — Report the first checkpoint

Given a valid drone, when its flight starts, checkpoint `0` is observable.

This provides a small entry point into the flight behaviour before the execution-model complexity is introduced.

The exact test for this behaviour will be designed in `testplan.md`.

---

# 6. Open design points

The following remain unresolved:

- exact validation contract for negative `MaxCheckpoints`;
- exact validation contract for negative `DelayMs`;
- exact handling of missing/unknown drones;
- exact Part B failure trigger;
- final public API used by the first behaviour.

These decisions should be resolved when the corresponding test and API design requires them.

---

# 7. Design status

### Visualizations

- [x] Common drone-flight flow.
- [x] Part A `Join` / no-`Join` comparison.
- [x] Part A–C execution-model comparison.

### Pseudocode

- [x] Common drone flight.
- [x] Part A.
- [x] Part A no-`Join` variant.
- [x] Part B.
- [x] Part C.

### Vertical behaviours

- [x] Core behaviours divided into development units.
- [x] Part A behaviours divided.
- [x] Part B behaviours divided.
- [x] Part C behaviours divided.
- [x] Development relationships identified.
- [x] First behaviour candidate identified.