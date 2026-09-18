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

## 2. Part A

### Behaviours

- `B6` — concurrent Thread execution.
- `B7` — Join waits for all required drones.
- `B8` — no-Join demonstration.
- `B9` — non-deterministic console output.

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

---

## 3. Part B

### Behaviours

- `B10` — task-based flight completion.
- `B11` — one TCS per drone.
- `B12` — Task.WhenAll coordination.
- `B13` — deterministic simulated failure.
- `B14` — failure propagation.
- `B15` — Task.Exception observation.

### Failure

The selected failure produces:

`InvalidOperationException("Simulated drone failure.")`

### Pseudocode

```text
Prepare drones

For each drone:
    Create one TaskCompletionSource
    Start drone work

On success:
    Complete the TCS

On selected simulated failure:
    Fault the TCS

Observe Task.WhenAll

Observe Task.Exception for the failure demonstration
```

---

## 4. Part C

### Behaviours

- `B16` — async flight.
- `B17` — async checkpoint delay.
- `B18` — multiple async flights can overlap.
- `B19` — await Task.WhenAll.
- `B20` — try/catch failure handling.

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
    Catch and report exception
```

`.Wait()` and `.Result` are not used in the async path.

---

## 5. Core vertical behaviours

| ID | Behaviour | Traceability |
|---|---|---|
| `VB01` | Valid DroneModel configuration is accepted | `R2 → AC-CORE-2 → B1` |
| `VB05` | `MaxCheckpoints = 0` reports checkpoint 0 | `R3 → AC-CORE-3 → B2` |
| `VB06` | Checkpoints progress from 0 to Max in order | `R3 → AC-CORE-3 → B2/B3` |
| `VB07` | Delay is applied between checkpoint steps | `R4 → AC-CORE-4 → B4` |
| `VB08` | Successful lifecycle reports start/checkpoints/completion | `R5 → AC-CORE-5 → B5` |

---

## 6. Edge-case vertical behaviours

| ID | Behaviour | Traceability |
|---|---|---|
| `VB02` | Negative `MaxCheckpoints` is rejected | `E1 → AC-EDGE-1 → B2` |
| `VB03` | Negative `DelayMs` is rejected | `E2 → AC-EDGE-2 → B4` |
| `VB04` | Missing/blank name is rejected | `E3 → AC-EDGE-3 → B1` |

---

## 7. Part A vertical behaviours

| ID | Behaviour | Traceability |
|---|---|---|
| `VB09` | Multiple drones execute on separate Threads | `R6 → AC-A1 → B6` |
| `VB10` | Join waits for all drones | `R7 → AC-A2 → B7` |
| `VB11` | No-Join allows main-thread continuation | `R8 → AC-A3 → B8` |
| `VB12` | Concurrent output can be interleaved | `R9 → AC-A4 → B9` |

---

## 8. Part B vertical behaviours

| ID | Behaviour | Traceability |
|---|---|---|
| `VB13` | A drone operation completes through Task | `R10 → AC-B1 → B10` |
| `VB14` | One TCS is used per drone | `R11 → AC-B2 → B11` |
| `VB15` | Multiple tasks are coordinated with Task.WhenAll | `R12 → AC-B3 → B12` |
| `VB16` | Simulated failure faults the operation | `R13 → AC-B4 → B13` |
| `VB17` | Failure reaches orchestration | `R14 → AC-B5 → B14` |
| `VB18` | Task.Exception exposes the fault | `R15 → AC-B6 → B15` |

---

## 9. Part C vertical behaviours

| ID | Behaviour | Traceability |
|---|---|---|
| `VB19` | Valid async flight completes | `R16 → AC-C1 → B16` |
| `VB20` | Checkpoint delay is asynchronous | `R17 → AC-C2 → B17` |
| `VB21` | Multiple async flights make overlapping progress | `R18 → AC-C3 → B18` |
| `VB22` | Await Task.WhenAll coordinates completion | `R19 → AC-C4 → B19` |
| `VB23` | Async failure is handled by orchestration | `R20 → AC-C5 → B20` |
| `VB24` | Part B and Part C can be compared | `R21 → AC-C6` |

---

## 10. Part D vertical behaviours

Part D is optional in the assignment and active only because it is currently selected as the final project target.

| ID | Behaviour | Traceability |
|---|---|---|
| `VB-D01` | Retrieve route data | `PD2 → AC-D1` |
| `VB-D02` | Retrieve weather data | `PD3 → AC-D2` |
| `VB-D03` | Retrieve restrictions | `PD9 → AC-D8` |
| `VB-D04` | Apply control-tower data | `PD5 → AC-D4` |
| `VB-D05` | Handle HTTP failure | `PD6 → AC-D5` |
| `VB-D06` | Handle HTTP timeout | `PD7 → AC-D6` |
| `VB-D07` | Keep HTTP flow asynchronous | `PD4/PD8 → AC-D3/AC-D7` |
| `VB-D08` | Apply restrictions | `PD9 → AC-D8` |
| `VB-D09` | Log HTTP lifecycle | `PD10 → AC-D9` |
| `VB-D10` | Compare sequential/concurrent HTTP | `PD11 → AC-D10` |
| `VB-D11` | Variable response time is demonstrable | `PD12 → AC-D11` |

### Part D API

```text
GET /route?drone=Navn
GET /weather
GET /restrictions
```

### Part D data flow

```text
Route ───────────────┐
Weather ─────────────┼→ Final simulation configuration
Restrictions ────────┘
                       ↓
                   DroneFlight
```

---

## 11. Behaviour relationships

```text
Core
├── B1–B5
│   ├── VB01
│   ├── VB02
│   ├── VB03
│   ├── VB04
│   ├── VB05
│   ├── VB06
│   ├── VB07
│   └── VB08
│
├── Part A → VB09–VB12
├── Part B → VB13–VB18
└── Part C → VB19–VB24

Optional Part D → VB-D01–VB-D11
```

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

Observation boundary:

`Action<FlightEvent>`

The test observes the event rather than console formatting.

---

## 13. Design status

- [x] Core behaviours defined.
- [x] Assignment edge cases defined.
- [x] Part A behaviours defined.
- [x] Part B behaviours defined.
- [x] Part C behaviours defined.
- [x] Part D target behaviours defined.
- [x] First TDD behaviour identified.
- [x] All behaviours trace to requirements/acceptance criteria or explicit edge cases.
