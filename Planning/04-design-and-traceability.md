# Async Drone Dash — Design and Traceability

## 1. Design direction

| Part | Execution | Coordination |
|---|---|---|
| A | `Thread` | `Join` |
| B | `Task` + `TaskCompletionSource` | `Task.WhenAll` |
| C | `async`/`await` | `await Task.WhenAll` |
| D | `HttpClient` / `HttpListener` | Sequential or concurrent async calls |

Required execution-model differences remain visible. No unnecessary architectural layers are added.

---

## 2. Responsibilities

### Console / Menu

Owns menu display, selection, starting demonstrations, user-facing output, and return/exit flow.

Does not own drone-flight or concurrency logic.

### DroneModel

Represents `Name`, `MaxCheckpoints`, and `DelayMs`.

### FlightEvent

Represents observable flight progress/outcome.

### DroneFlight

Owns validation, checkpoint progression, delay, event reporting, completion, and flight failure.

Does not own console output or multi-drone orchestration.

### Part A orchestration

Owns `Thread` creation/start, `Join`, no-Join demonstration, and overall completion.

### Part B orchestration

Owns one TCS per drone, task completion/failure, `Task.WhenAll`, failure propagation, and `Task.Exception` demonstration.

### Part C orchestration

Owns async flight startup, `await Task.WhenAll`, and `try/catch`.

### ControlTowerClient

Conditional Part D component.

Owns asynchronous HTTP requests, response mapping, timeout/error translation, and reusable `HttpClient` ownership.

### Local Control Tower

Conditional Part D component.

Owns local `HttpListener` request handling and deterministic demonstration responses.

---

## 3. Dependency direction

```text
Console / Menu
      ↓
Part-specific orchestration
      ↓
DroneFlight
      ↓
DroneModel

DroneFlight
      ↓
FlightEvent

Part D orchestration
      ↓
ControlTowerClient
      ↓
HttpClient
      ↓
Local Control Tower
```

`DroneFlight` has no HTTP dependency.

---

## 4. Public API

### `DroneFlight.Run`

```text
void Run(DroneModel drone, Action<FlightEvent> report)
```

Executes one synchronous flight.

Exceptions:

- invalid configuration → validation contract in `03`;
- simulated flight failure → documented failure exception when that scenario is used.

Side effects:

- invokes `report`;
- no direct console output.

### `DroneFlight.RunAsync`

```text
Task RunAsync(DroneModel drone, Action<FlightEvent> report)
```

Executes one asynchronous flight.

Exceptions propagate through the returned `Task`.

Side effects:

- invokes `report`;
- no direct console output.

### `ControlTowerClient`

```text
ControlTowerClient(HttpClient httpClient)

Task<RouteData> GetRouteAsync(string droneName)
Task<WeatherData> GetWeatherAsync()
Task<RestrictionData> GetRestrictionsAsync()
```

`HttpClient` is reused for the lifetime of the client.

### Part D response models

```text
RouteData
    MaxCheckpoints : int

WeatherData
    Condition : string

RestrictionData
    MaxCheckpoints : int?
```

### `ControlTowerException`

```text
ControlTowerErrorKind
    RequestFailed
    NotFound
    Timeout
    InvalidResponse
```

`ControlTowerException` exposes the error kind and preserves the underlying exception where useful.

---

## 5. Part A

```text
Create drones
→ create one Thread per drone
→ start Threads
→ execute DroneFlight
→ Join all Threads
→ report overall completion
```

The no-Join demonstration deliberately omits the waiting step.

---

## 6. Part B

```text
Create drones
→ create one TCS per drone
→ start drone work
→ complete/fault each TCS
→ Task.WhenAll
→ observe success/failure
```

Failure contract:

```text
InvalidOperationException("Simulated drone failure.")
```

The affected TCS is faulted with the exception.

---

## 7. Part C

```text
Create drones
→ start async flights
→ await Task.Delay
→ await Task.WhenAll
→ try/catch
```

`.Wait()` and `.Result` are prohibited in the Part C async path.

---

## 8. Part D

The local API is:

```text
GET /route?drone=Navn
GET /weather
GET /restrictions
```

The server uses asynchronous `HttpListener` request handling.

The client uses asynchronous `HttpClient` operations.

### Request flow

```text
Route ───────────────┐
Weather ─────────────┼→ final simulation configuration
Restrictions ────────┘
                       ↓
                   DroneFlight
```

Independent requests may be sequential or concurrent. Both modes must produce equivalent functional data.

HTTP lifecycle logging is part of the selected final Part D target.

Variable response time is a demonstration capability. Exact duration is not a test oracle.

---

## 9. Observability

`FlightEvent` is the deterministic observation boundary for flight behaviour.

```text
Flight behaviour
      ↓
FlightEvent
      ↓
Orchestration / Console
```

Concurrency verification combines behaviour tests with required implementation inspection. It must prove meaningful coordination/overlap rather than merely eventual completion.

---

## 10. Implementation inspection contracts

| ID | Contract |
|---|---|
| `I01` | DroneModel exposes required properties |
| `I02` | Required delay mechanism is used without timing-based correctness assertions |
| `I03` | One Thread per Part A drone |
| `I04` | Join in normal Part A run |
| `I05` | Real no-Join demonstration path |
| `I06` | Part B uses Task |
| `I07` | One TCS per drone |
| `I08` | Part B uses Task.WhenAll |
| `I09` | Task.Exception explicitly observed |
| `I10` | Part C uses an async flight method |
| `I11` | Part C uses await Task.Delay |
| `I12` | Part C allows independent flights to overlap |
| `I13` | Part C uses await Task.WhenAll |
| `I14` | Part C uses try/catch |
| `I15` | Part C contains no Wait/Result blocking |
| `I16` | ControlTowerClient reuses HttpClient |
| `I17` | HTTP requests use asynchronous APIs |
| `I18` | Local HttpListener uses asynchronous request handling |
| `I19` | Part D uses final JSON contracts and error categories |

---

## 11. Traceability

| ID | Acceptance criterion | Behaviour / verification |
|---|---|---|
| `R1` | `AC-CORE-1` | Project/DOC01 |
| `R2` | `AC-CORE-2` | B1/VB01 |
| `R3` | `AC-CORE-3` | B2/B3/VB05/VB06 |
| `R4` | `AC-CORE-4` | B4/VB07 |
| `R5` | `AC-CORE-5` | B5/VB08 |
| `R6` | `AC-A1` | B6/VB09 + I03 |
| `R7` | `AC-A2` | B7/VB10 + I04 |
| `R8` | `AC-A3` | B8/VB11 + M01/I05 |
| `R9` | `AC-A4` | B9/VB12 + M02 |
| `R10` | `AC-B1` | B10/VB13 + I06 |
| `R11` | `AC-B2` | B11/VB14 + I07 |
| `R12` | `AC-B3` | B12/VB15 + I08 |
| `R13` | `AC-B4` | B13/VB16/T18 |
| `R14` | `AC-B5` | B14/VB17/T19/T21 |
| `R15` | `AC-B6` | B15/VB18/T20 + I09 |
| `R16` | `AC-C1` | B16/VB19 + I10 |
| `R17` | `AC-C2` | B17/VB20 + I11 |
| `R18` | `AC-C3` | B18/VB21 + I12/T24 |
| `R19` | `AC-C4` | B19/VB22 + I13/T25 |
| `R20` | `AC-C5` | B20/VB23 + I14/T26 |
| `R21` | `AC-C6` | VB24/DOC05 |
| `R22` | `AC-DLV-1` | M03 |
| `R23` | `AC-DLV-2` | DOC06 |
| `R24` | `AC-DLV-3` | DOC07/DOC08 |
| `R25` | `AC-DLV-4` | DOC09 |
| `E1` | `AC-EDGE-1` | VB02/T02 |
| `E2` | `AC-EDGE-2` | VB03/T03 |
| `E3` | `AC-EDGE-3` | VB04/T06 + HTTP12 |
| `E4` | `AC-EDGE-4` | Part D failure tests |
| `PD1` | `AC-D1` | HTTP01 + I18 |
| `PD2` | `AC-D1` | B-D01/HTTP01 |
| `PD3` | `AC-D2` | B-D02/HTTP02 |
| `PD4` | `AC-D3` | B-D07/HTTP08 + I16/I17 |
| `PD5` | `AC-D4` | B-D04/HTTP04 |
| `PD6` | `AC-D5` | B-D05/HTTP05/HTTP07 |
| `PD7` | `AC-D6` | B-D06/HTTP06 |
| `PD8` | `AC-D7` | B-D07/HTTP08 + I18 |
| `PD9` | `AC-D8` | B-D08/HTTP03 |
| `PD10` | `AC-D9` | B-D09/HTTP09 |
| `PD11` | `AC-D10` | B-D10/HTTP10 |
| `PD12` | `AC-D11` | B-D11/HTTP11 |
| `PD13` | — | Bonus/out of target |

---

## 12. Risks

| Risk | Response |
|---|---|
| Non-deterministic scheduling | Do not assert exact ordering; use completion/overlap semantics and inspection |
| Timing-based tests become flaky | Never use elapsed duration as the correctness oracle |
| Task failure semantics misunderstood | Test faulted state plus underlying exception |
| Async code becomes blocking | Inspect for `.Wait()`/`.Result` |
| External HTTP makes tests unstable | Use the local/controllable HTTP boundary |
| Part D grows too far | Protect A–C as MVP |
| Architecture grows unnecessarily | Add only components with concrete responsibilities |

---

## 13. Final design status

All core contracts are locked.

Part D contracts are locked to the final local API and response models above.

No unresolved design decision remains that blocks test-file creation.
