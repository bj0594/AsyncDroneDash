# Async Drone Dash — Design and Traceability

## 1. Design direction

The solution should remain small and make the differences between Parts A–C visible.

| Part | Execution | Coordination |
|---|---|---|
| A | `Thread` | `Join` |
| B | `Task` + `TaskCompletionSource` | `Task.WhenAll` |
| C | `async`/`await` | `await Task.WhenAll` |
| D | `HttpClient` / `HttpListener` | Sequential or concurrent async calls |

The design should not hide required execution-model differences behind unnecessary abstractions.

---

## 2. Responsibilities

### Console / Menu

- display menu;
- accept selection;
- start selected demonstration;
- present results/errors;
- return to menu or exit.

Does not own drone-flight or concurrency logic.

### DroneModel

Represents `Name`, `MaxCheckpoints`, and `DelayMs`.

### FlightEvent

Represents observable flight events:

- `Started`
- `CheckpointReached`
- `Completed`
- `Faulted`

### DroneFlight

Owns one drone's flight behaviour:

- validation;
- checkpoint progression;
- configured delay;
- event reporting;
- completion;
- flight failure.

Does not own console output or multi-drone orchestration.

### Part A orchestration

Owns thread creation, start, `Join`, the no-Join demonstration, and overall completion.

### Part B orchestration

Owns one TCS per drone, task completion/failure, `Task.WhenAll`, failure propagation, and `Task.Exception` demonstration.

### Part C orchestration

Owns async flight startup, `await Task.WhenAll`, and orchestration-level `try/catch`.

### ControlTowerClient

Conditional on Part D.

Owns asynchronous HTTP requests, response mapping, timeout/error handling, and HTTP client reuse.

### Local Control Tower

Conditional on Part D.

Owns the local HTTP service and deterministic demonstration responses.

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

`DroneFlight` has no dependency on HTTP.

No additional architectural layers are currently justified.

---

## 4. Shared flight behaviour

Parts A–C use the same underlying flight concept:

1. validate configuration;
2. report `Started`;
3. report checkpoint `0`;
4. for each remaining checkpoint:
   - apply `DelayMs`;
   - advance;
   - report `CheckpointReached`;
5. report `Completed`.

For `MaxCheckpoints = 0`, checkpoint `0` is reported and no intermediate delay is required.

---

## 5. Part A design

Normal flow:

```text
Create drones
→ create one Thread per drone
→ start Threads
→ execute DroneFlight
→ Join all Threads
→ report overall completion
```

No-Join demonstration:

```text
Create and start Threads
→ do not Join
→ main thread continues
```

Exact cross-thread console order is not a contract.

---

## 6. Part B design

```text
Create drones
→ create one TCS per drone
→ start drone work
→ complete/fault each TCS
→ Task.WhenAll
→ observe success/failure
```

The selected failure is introduced by the Part B scenario rather than by adding a failure property to `DroneModel`.

Failure contract:

```text
InvalidOperationException("Simulated drone failure.")
```

A worker failure faults the relevant TCS. The aggregate operation therefore becomes faulted and exposes the failure through `Task.Exception`.

---

## 7. Part C design

```text
Create drones
→ start async flights
→ await Task.Delay between checkpoints
→ await Task.WhenAll
→ handle failure with try/catch
```

The async path must not use `.Wait()` or `.Result`.

Multiple flights must have an observable opportunity to overlap; eventual completion alone is not sufficient evidence of concurrency.

---

## 8. Part D design

Part D is optional and currently selected as the project's final target.

The project uses the local `HttpListener` alternative supplied by the assignment and `HttpClient` for the client.

### Local endpoints

```text
GET /route?drone=Navn
GET /weather
```

Optional:

```text
GET /restrictions
```

The route request uses the query data available through the request URL/`RawUrl` to identify the drone.

### ControlTowerClient

The client:

- reuses one `HttpClient`;
- uses asynchronous HTTP APIs;
- deserializes valid responses;
- translates dependency failures into `ControlTowerException`.

### ControlTowerException

The public error categories are:

- `RequestFailed` — non-success HTTP response or connection-level request failure;
- `Timeout` — request exceeded the configured timeout;
- `InvalidResponse` — response data cannot be deserialized or fails response validation.

The exception must preserve the underlying cause where useful.

### Local service

The local `HttpListener` uses asynchronous request handling and must not use blocking `GetContext()` as its normal request loop.

The local service can deliberately vary response time to simulate slow network conditions.

The variable delay is demonstration behaviour, not an exact timing contract.

### Sequential/concurrent requests

Independent route, weather, and optional restriction requests may be executed sequentially or concurrently.

Both modes must produce equivalent functional data.

Performance is observed separately and is not a brittle correctness oracle.

---

## 9. Public API

### `DroneModel`

```text
Name : string
MaxCheckpoints : int
DelayMs : int
```

Represents drone configuration.

### `FlightEvent`

Represents observable flight progress/outcome.

### `DroneFlight.Run`

```text
void Run(DroneModel drone, Action<FlightEvent> report)
```

Purpose:

Executes one synchronous drone flight and reports observable events.

Exceptions:

- invalid configuration uses the validation contract in `03-domain-and-rules.md`;
- the final Part B failure contract applies where that scenario is used.

Side effects:

- invokes `report`;
- does not write directly to the console.

### `DroneFlight.RunAsync`

```text
Task RunAsync(DroneModel drone, Action<FlightEvent> report)
```

Purpose:

Executes one asynchronous drone flight and reports observable events.

Exceptions:

Flight failures propagate through the returned `Task`.

Side effects:

- invokes `report`;
- does not write directly to the console.

The method remains asynchronous throughout its execution path.

### `ControlTowerClient` — conditional

Purpose:

Retrieves route, weather and optional restriction data asynchronously.

Public operations and exact parameter types remain open until Part D implementation design is finalized.

Exceptions:

Failures are represented through the `ControlTowerException` categories above.

Side effects:

Performs asynchronous HTTP requests and may emit request lifecycle logs if that optional behaviour is included.

---

## 10. Observability

`FlightEvent` provides the deterministic observation boundary for flight behaviour.

```text
Flight behaviour
      ↓
FlightEvent
      ↓
Orchestration / Console
```

This keeps console formatting out of the flight behaviour.

Concurrency verification must prove meaningful overlap or coordination rather than merely checking that every operation eventually completed.

---

## 11. Part D data flow

```text
Route
  ↓
Base MaxCheckpoints

Weather
  ↓
Delay adjustment

Restrictions
  ↓
Maximum checkpoint restriction

All data
  ↓
Final simulation configuration
  ↓
DroneFlight
```

If a restriction exists:

`FinalMaxCheckpoints = min(RouteMaxCheckpoints, RestrictionMaxCheckpoints)`

Otherwise:

`FinalMaxCheckpoints = RouteMaxCheckpoints`

`FinalDelayMs = Drone.DelayMs + Weather.DelayAdjustmentMs`

The final values must satisfy core validation.

---

## 12. Implementation-specific contracts

| Requirement | Inspection |
|---|---|
| `R6` | Separate `Thread` per Part A drone |
| `R7` | `Join` in normal Part A run |
| `R8` | Real no-Join demonstration |
| `R10` | Part B uses `Task` |
| `R11` | One TCS per drone |
| `R12` | Part B uses `Task.WhenAll` |
| `R15` | `Task.Exception` explicitly observed |
| `R16` | Actual async flight method |
| `R17` | `await Task.Delay` |
| `R19` | `await Task.WhenAll` |
| `R20` | Orchestration `try/catch` |
| `PD4` | Async `HttpClient` APIs |
| `PD8` | No synchronous blocking in HTTP flow |
| `PD1` | Local `HttpListener` request handling |

---

## 13. Requirement traceability

| ID | Requirement | Acceptance criterion | Behaviour |
|---|---|---|---|
| `R1` | Runnable console application | `AC-CORE-1` | Project runs |
| `R2` | Required DroneModel properties | `AC-CORE-2` | `B1` |
| `R3` | Checkpoint progression | `AC-CORE-3` | `B2`, `B3` |
| `R4` | Configured checkpoint delay | `AC-CORE-4` | `B4` |
| `R5` | Start/progress/completion reporting | `AC-CORE-5` | `B5` |
| `R6` | Concurrent Threads | `AC-A1` | `B6` |
| `R7` | Join | `AC-A2` | `B7` |
| `R8` | No-Join demonstration | `AC-A3` | `B8` |
| `R9` | Concurrent output | `AC-A4` | `B9` |
| `R10` | Task-based flight | `AC-B1` | `B10` |
| `R11` | One TCS per drone | `AC-B2` | `B11` |
| `R12` | Task.WhenAll | `AC-B3` | `B12` |
| `R13` | Failure scenario | `AC-B4` | `B13` |
| `R14` | Failure propagation | `AC-B5` | `B14` |
| `R15` | Task.Exception | `AC-B6` | `B15` |
| `R16` | Async flight | `AC-C1` | `B16` |
| `R17` | Async delay | `AC-C2` | `B17` |
| `R18` | Multiple async flights | `AC-C3` | `B18` |
| `R19` | await Task.WhenAll | `AC-C4` | `B19` |
| `R20` | try/catch | `AC-C5` | `B20` |
| `R21` | Part B/C comparison | `AC-C6` | Comparison |
| `R22` | Menu A–D | `AC-DLV-1` | Menu |
| `R23` | GitHub | `AC-DLV-2` | Delivery |
| `R24` | README | `AC-DLV-3` | Documentation |
| `R25` | Reflection | `AC-DLV-4` | Reflection |

### Conditional Part D

| ID | Requirement | Acceptance criterion | Behaviour |
|---|---|---|---|
| `PD1` | Control-tower service | `AC-D1` | Control-tower service |
| `PD2` | Route data | `AC-D1` | `B-D01` |
| `PD3` | Weather data | `AC-D2` | `B-D02` |
| `PD4` | Async HTTP consumption | `AC-D3` | `B-D07` |
| `PD5` | Simulation effect | `AC-D4` | `B-D04` |
| `PD6` | HTTP failure handling | `AC-D5` | `B-D05` |
| `PD7` | Timeout handling | `AC-D6` | `B-D06` |
| `PD8` | Non-blocking HTTP flow | `AC-D7` | `B-D07` |
| `PD9` | Restrictions | `AC-D8` | `B-D08` |
| `PD10` | HTTP lifecycle logging | `AC-D9` | `B-D09` |
| `PD11` | Sequential/concurrent comparison | `AC-D10` | `B-D10` |
| `PD12` | Variable response time | `AC-D11` | `B-D11` |
| `PD13` | Drone registration bonus | — | Optional extension |

Verification IDs are maintained in `AsyncDroneDash.Tests/TestPlan.md`.

---

## 14. Risks

| Risk | Response |
|---|---|
| Thread scheduling is non-deterministic | Test completion/overlap semantics; observe race output manually |
| Console output is unstable | Test structured events; manually observe interleaving |
| Timing tests become flaky | Avoid exact wall-clock correctness assertions |
| Part B failure semantics are misunderstood | Use explicit failure contract and focused tests |
| `Task.WhenAll` fault semantics are misunderstood | Test task states and exception information |
| Async flow becomes blocking | Inspect for `.Wait()` / `.Result` |
| HTTP tests depend on external network | Use controllable local HTTP boundary |
| Part D expands scope | Protect A–C as MVP |
| Architecture becomes over-engineered | Add components only for concrete responsibilities |

---

## 15. Open design decisions

- final Part D JSON response schemas;
- exact public Part D method signatures;
- whether HTTP lifecycle logging remains in final scope;
- final restriction response contract.

Core validation, Part B failure, endpoint structure, weather mapping, HTTP exception categories, client reuse, and asynchronous local-server handling are now locked.

---

## 16. First behaviour

**VB05 — Report checkpoint 0**

Traceability:

`R3 → AC-CORE-3 → B2 → VB05`

Scenario:

```text
Given a valid drone with MaxCheckpoints = 0
When the basic flight executes
Then CheckpointReached(0) is observable
```

Current observation boundary:

`DroneFlight.Run(DroneModel, Action<FlightEvent>)`

---

## Status

- [x] Requirements mapped.
- [x] Acceptance criteria mapped.
- [x] Behaviours mapped.
- [x] Public API defined for the core flight.
- [x] Part B failure contract defined.
- [x] Part D endpoint direction defined.
- [x] HTTP exception categories defined.
- [x] HttpClient reuse defined.
- [x] Async local-server handling defined.
- [ ] Final Part D JSON schemas.
- [ ] Final Part D public method signatures.