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

### Behaviours

| ID | Behaviour | Traceability |
|---|---|---|
| `B6` | Concurrent Thread execution | `R6 → AC-A1` |
| `B7` | Joined completion | `R7 → AC-A2` |
| `B8` | No-Join behaviour | `R8 → AC-A3` |
| `B9` | Non-deterministic console output | `R9 → AC-A4` |

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
Apply selected failure scenario
    ↓
Complete or fault each TCS
    ↓
Task.WhenAll
    ↓
Observe success/failure
```

### Behaviours

| ID | Behaviour | Traceability |
|---|---|---|
| `B10` | Task-based flight completion | `R10 → AC-B1` |
| `B11` | Individual completion signalling | `R11 → AC-B2` |
| `B12` | Task.WhenAll coordination | `R12 → AC-B3` |
| `B13` | Deterministic simulated failure | `R13 → AC-B4` |
| `B14` | Failure propagation | `R14 → AC-B5` |
| `B15` | Task.Exception observation | `R15 → AC-B6` |

The selected failure target is one participating drone. Its TCS is faulted with `InvalidOperationException("Simulated drone failure.")` when the failure scenario triggers.

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

The async path must not use `.Wait()` or `.Result`.

### Behaviours

| ID | Behaviour | Traceability |
|---|---|---|
| `B16` | Async flight | `R16 → AC-C1` |
| `B17` | Async checkpoint delay | `R17 → AC-C2` |
| `B18` | Multiple async flights can overlap | `R18 → AC-C3` |
| `B19` | Async Task.WhenAll coordination | `R19 → AC-C4` |
| `B20` | Async failure handling | `R20 → AC-C5` |

---

## 5. Core vertical behaviours

| ID | Behaviour | Traceability |
|---|---|---|
| `VB01` | Valid DroneModel configuration is accepted | `R2 → AC-CORE-2 → B1` |
| `VB02` | Negative MaxCheckpoints is rejected | `E1 → AC-EDGE-1 → B2` |
| `VB03` | Negative DelayMs is rejected | `E2 → AC-EDGE-2 → B4` |
| `VB04` | Missing/blank name is rejected | `E3 → AC-EDGE-3 → B1` |
| `VB05` | MaxCheckpoints = 0 reports checkpoint 0 | `R3 → AC-CORE-3 → B2` |
| `VB06` | Checkpoints progress from 0 to Max in order | `R3 → AC-CORE-3 → B2/B3` |
| `VB07` | Delay is applied between checkpoint steps | `R4 → AC-CORE-4 → B4` |
| `VB08` | Successful lifecycle reports start/checkpoints/completion | `R5 → AC-CORE-5 → B5` |

---

## 6. Part A vertical behaviours

| ID | Behaviour | Traceability |
|---|---|---|
| `VB09` | Multiple drones execute on separate Threads | `R6 → AC-A1 → B6` |
| `VB10` | Join waits for all drones | `R7 → AC-A2 → B7` |
| `VB11` | No-Join allows main-thread continuation | `R8 → AC-A3 → B8` |
| `VB12` | Concurrent output can be interleaved | `R9 → AC-A4 → B9` |

---

## 7. Part B vertical behaviours

| ID | Behaviour | Traceability |
|---|---|---|
| `VB13` | A drone operation completes through Task | `R10 → AC-B1 → B10` |
| `VB14` | One TCS is used per drone | `R11 → AC-B2 → B11` |
| `VB15` | Multiple tasks are coordinated with Task.WhenAll | `R12 → AC-B3 → B12` |
| `VB16` | Simulated failure faults the operation | `R13 → AC-B4 → B13` |
| `VB17` | Failure reaches orchestration | `R14 → AC-B5 → B14` |
| `VB18` | Task.Exception exposes the fault | `R15 → AC-B6 → B15` |

---

## 8. Part C vertical behaviours

| ID | Behaviour | Traceability |
|---|---|---|
| `VB19` | Valid async flight completes | `R16 → AC-C1 → B16` |
| `VB20` | Checkpoint delay is asynchronous | `R17 → AC-C2 → B17` |
| `VB21` | Multiple async flights make overlapping progress | `R18 → AC-C3 → B18` |
| `VB22` | Await Task.WhenAll coordinates completion | `R19 → AC-C4 → B19` |
| `VB23` | Async failure is handled by orchestration | `R20 → AC-C5 → B20` |
| `VB24` | Part B and Part C can be compared | `R21 → AC-C6` |

---

## 9. Edge-case vertical behaviours

| ID | Behaviour | Traceability |
|---|---|---|
| `VB-E04` | Unknown route drone is reported as not found | `E4 → AC-EDGE-4 → B-D01` |
| `VB-E05` | Control-tower failure/timeout/invalid response is handled | `E5 → AC-EDGE-5` |

---

## 10. Part D vertical behaviours

Part D is optional in the assignment and active only because it is currently selected as the final project target.

| ID | Behaviour | Traceability |
|---|---|---|
| `VB-D00` | Local control-tower service exposes the required endpoints | `PD1 → AC-D0` |
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
| `VB-D12` | Unknown route drone is reported as not found | `E4 → AC-EDGE-4` |

### Part D API

```text
GET /route?drone=Navn
GET /weather
GET /restrictions
```

The `/route` behaviour includes reading the requested drone from the request URL/query data, with `RawUrl` available as the assignment-specified learning point.

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
│   ├── VB01–VB08
│
├── Part A → B6–B9 → VB09–VB12
├── Part B → B10–B15 → VB13–VB18
└── Part C → B16–B20 → VB19–VB24

Optional Part D
└── VB-D00–VB-D12
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
