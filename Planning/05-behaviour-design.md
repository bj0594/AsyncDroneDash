# Async Drone Dash — Behaviour Design

## 1. Common flight behaviour

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

Checkpoint progression is inclusive:

`0 → 1 → ... → MaxCheckpoints`

For `MaxCheckpoints = 0`:

`Started → CheckpointReached(0) → Completed`

---

## 2. Part A — Thread Race

### Flow

    Create drones
    → create one Thread per drone
    → start all Threads
    → execute drone flights
    → Join all Threads
    → report overall completion

No-Join:

    Create and start Threads
    → do not Join
    → main thread continues
    → drone Threads continue

| Behaviour | Traceability |
|---|---|
| `B6` Concurrent Thread execution | `R6 → AC-A1` |
| `B7` Joined completion | `R7 → AC-A2` |
| `B8` No-Join behaviour | `R8 → AC-A3` |
| `B9` Non-deterministic output | `R9 → AC-A4` |

Exact scheduling order is not a contract.

---

## 3. Part B — Task + TaskCompletionSource

### Flow

    Create drones
    → create one TCS per drone
    → start drone work
    → selected failure target reaches checkpoint 1
    → fault target TCS
    → complete/fault remaining TCS values
    → Task.WhenAll
    → observe success/failure

Failure contract:

    InvalidOperationException("Simulated drone failure.")

The selected failure target fails immediately after reporting checkpoint `1`.

| Behaviour | Traceability |
|---|---|
| `B10` Task-based flight completion | `R10 → AC-B1` |
| `B11` Individual completion signalling | `R11 → AC-B2` |
| `B12` Task.WhenAll coordination | `R12 → AC-B3` |
| `B13` Deterministic simulated failure | `R13 → AC-B4` |
| `B14` Failure propagation | `R14 → AC-B5` |
| `B15` Task.Exception observation | `R15 → AC-B6` |

---

## 4. Part C — Async/Await

### Flow

    Create drones
    → start async flights
    → await Task.Delay between checkpoints
    → await Task.WhenAll
    → try/catch

`.Wait()` and `.Result` are prohibited in the async execution path.

The Part C failure demonstration uses the same deterministic failure scenario as Part B: the selected failure drone reports checkpoint `1` and then produces `InvalidOperationException("Simulated drone failure.")`.

| Behaviour | Traceability |
|---|---|
| `B16` Async flight | `R16 → AC-C1` |
| `B17` Async checkpoint delay | `R17 → AC-C2` |
| `B18` Multiple async flights overlap | `R18 → AC-C3` |
| `B19` Async Task.WhenAll coordination | `R19 → AC-C4` |
| `B20` Async failure handling | `R20 → AC-C5` |

---

## 5. Core vertical behaviours

| ID | Behaviour | Traceability |
|---|---|---|
| `VB01` | Valid DroneModel configuration is accepted | `R2 → AC-CORE-2 → B1` |
| `VB02` | Negative `MaxCheckpoints` is rejected | `E1 → AC-EDGE-1 → B2` |
| `VB03` | Negative `DelayMs` is rejected | `E2 → AC-EDGE-2 → B4` |
| `VB04` | Missing/blank name is rejected | `E3 → AC-EDGE-3 → B1` |
| `VB05` | `MaxCheckpoints = 0` reports checkpoint `0` | `R3 → AC-CORE-3 → B2` |
| `VB06` | Checkpoints progress from `0` to `MaxCheckpoints` in order | `R3 → AC-CORE-3 → B2/B3` |
| `VB07` | Delay is applied between checkpoint steps | `R4 → AC-CORE-4 → B4` |
| `VB08` | Successful lifecycle reports start/checkpoints/completion | `R5 → AC-CORE-5 → B5` |

---

## 6. Part A vertical behaviours

| ID | Behaviour | Traceability |
|---|---|---|
| `VB09` | Multiple drones execute on separate Threads | `R6 → AC-A1 → B6` |
| `VB10` | Join waits for all drones | `R7 → AC-A2 → B7` |
| `VB11` | No-Join allows main-thread continuation | `R8 → AC-A3 → B8` |
| `VB12` | Concurrent console output can be interleaved | `R9 → AC-A4 → B9` |
| `VB13` | Each drone retains its own progress sequence | `R5 → AC-CORE-5` |
| `VB14` | Multiple Thread flights make meaningful overlapping progress | `R6 → AC-A1 → B6` |

---

## 7. Part B vertical behaviours

| ID | Behaviour | Traceability |
|---|---|---|
| `VB15` | A drone operation completes through Task | `R10 → AC-B1 → B10` |
| `VB16` | One TCS is used per participating drone | `R11 → AC-B2 → B11` |
| `VB17` | Multiple tasks are coordinated with Task.WhenAll | `R12 → AC-B3 → B12` |
| `VB18` | Simulated failure faults after checkpoint `1` | `R13 → AC-B4 → B13` |
| `VB19` | Failure reaches orchestration | `R14 → AC-B5 → B14` |
| `VB20` | Task.Exception exposes the fault | `R15 → AC-B6 → B15` |
| `VB21` | Combined Task.WhenAll failure cannot report false success | `R12/R14 → AC-B3/AC-B5` |

---

## 8. Part C vertical behaviours

| ID | Behaviour | Traceability |
|---|---|---|
| `VB22` | Valid async flight completes | `R16 → AC-C1 → B16` |
| `VB23` | Checkpoint delay is asynchronous | `R17 → AC-C2 → B17` |
| `VB24` | Multiple async flights make overlapping progress | `R18 → AC-C3 → B18` |
| `VB25` | Await Task.WhenAll coordinates completion | `R19 → AC-C4 → B19` |
| `VB26` | Async failure is handled by orchestration | `R20 → AC-C5 → B20` |
| `VB27` | Part B and Part C can be compared | `R21 → AC-C6` |

---

## 9. Edge-case vertical behaviours

| ID | Behaviour | Traceability |
|---|---|---|
| `VB-E04` | Unknown route drone is reported as not found | `E4 → AC-EDGE-4` |
| `VB-E05` | Control-tower failure, timeout, or invalid response is handled | `E5 → AC-EDGE-5` |

---

## 10. Part D vertical behaviours

Part D is optional in the assignment and active because it is currently selected as the final project target.

| ID | Behaviour | Traceability |
|---|---|---|
| `VB-D00` | Local control tower exposes required endpoints and can be shut down cleanly | `PD1 → AC-D0` |
| `VB-D01` | Route data is retrieved deterministically from the requested drone name | `PD2 → AC-D1` |
| `VB-D02` | Weather data is retrieved | `PD3 → AC-D2` |
| `VB-D03` | Restrictions are retrieved | `PD9 → AC-D8` |
| `VB-D04` | Control-tower data affects final simulation configuration | `PD5 → AC-D4` |
| `VB-D05` | HTTP failure is translated correctly | `PD6 → AC-D5` |
| `VB-D06` | HTTP timeout is translated correctly | `PD7 → AC-D6` |
| `VB-D07` | HTTP consumption remains asynchronous | `PD4 → AC-D3` |
| `VB-D08` | HTTP flow is non-blocking | `PD8 → AC-D7` |
| `VB-D09` | HTTP lifecycle is logged | `PD10 → AC-D9` |
| `VB-D10` | Sequential and concurrent HTTP modes can be compared | `PD11 → AC-D10` |
| `VB-D11` | Variable response time is demonstrable | `PD12 → AC-D11` |

The local API is:

    GET /route?drone=Navn
    GET /weather
    GET /restrictions

The `/route` behaviour reads the requested drone name from the request URL/query data, with `RawUrl` available as the assignment-specific learning point. The selected project uses the deterministic drone-name-to-route mapping defined in `03-domain-and-rules.md`.

---

## 11. Behaviour relationships

    Core
    ├── B1–B5
    │   └── VB01–VB08
    ├── Part A
    │   └── B6–B9 → VB09–VB14
    ├── Part B
    │   └── B10–B15 → VB15–VB21
    └── Part C
        └── B16–B20 → VB22–VB26
            └── R21/AC-C6 → VB27

    Optional Part D
    └── VB-D00–VB-D11

    Edge cases
    └── VB-E04–VB-E05

---

## 12. First behaviour

### VB05 — Report checkpoint 0

Traceability:

`R3 → AC-CORE-3 → B2 → VB05`

Scenario:

    Given a valid drone with MaxCheckpoints = 0
    When the basic flight executes
    Then CheckpointReached(0) is observable

Observation boundary:

`Action<FlightEvent>`

The first test verifies structured behaviour rather than console formatting.

---

## 13. Status

- [x] Core behaviours defined.
- [x] Part A behaviours defined.
- [x] Part B behaviours defined.
- [x] Part C behaviours defined.
- [x] Part D target behaviours defined.
- [x] Edge cases mapped.
- [x] Concurrency overlap represented as observable behaviour.
- [x] First TDD behaviour selected.