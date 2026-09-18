# Async Drone Dash — Design and Traceability

## 1. Design direction

The solution should remain small and make the differences between Parts A–C visible.

| Part | Execution | Coordination |
|---|---|---|
| A | `Thread` | `Join` |
| B | `Task` + `TaskCompletionSource` | `Task.WhenAll` |
| C | `async`/`await` | `await Task.WhenAll` |
| D | `HttpClient` / optional `HttpListener` | Sequential or concurrent async calls |

The design should not hide the required execution-model differences behind unnecessary abstractions.

Additional layers require a concrete responsibility.

---

## 2. Responsibilities

### Console / Menu

- display menu;
- accept selection;
- start the selected demonstration;
- present results/errors;
- return to menu or exit.

Does not own drone-flight or concurrency logic.

### DroneModel

Represents:

- `Name`
- `MaxCheckpoints`
- `DelayMs`

### FlightEvent

Represents an observable flight event:

- `Started`
- `CheckpointReached`
- `Completed`
- `Faulted`

A checkpoint event contains the checkpoint number.

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

Owns:

- one `Thread` per drone;
- starting threads;
- normal `Join` behaviour;
- no-`Join` demonstration;
- overall completion.

### Part B orchestration

Owns:

- one `TaskCompletionSource` per drone;
- starting drone work;
- successful/faulted completion;
- `Task.WhenAll`;
- failure propagation;
- `Task.Exception` demonstration.

### Part C orchestration

Owns:

- starting multiple async flights;
- `await Task.WhenAll`;
- orchestration-level `try/catch`.

### ControlTowerClient

Conditional on Part D.

Owns:

- asynchronous HTTP requests;
- response deserialization;
- timeout/error handling;
- exposing route, weather and restriction data.

The client should reuse a `HttpClient` rather than create one per request.

### Local Control Tower

Conditional on Part D.

Owns the local HTTP service and deterministic demonstration responses.

The server is an assignment-specific learning component, not a general-purpose production HTTP architecture.

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

No additional Core/Application/Infrastructure layers are currently justified.

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

The execution and coordination model changes between Parts A–C, not the basic domain behaviour.

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

The exact cross-thread console order is not a contract.

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

The selected failure condition is introduced by the Part B scenario rather than by adding a permanent failure property to `DroneModel`.

The final failure trigger must be identical in domain rules, behaviour design and tests.

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

Multiple async flights must have an observable opportunity to overlap; completion alone is not sufficient evidence of concurrency.

---

## 8. Part D design

Part D is optional and currently selected as the project's final target.

The project uses the local-service alternative offered by the assignment.

### Client

`ControlTowerClient` uses `HttpClient` and asynchronous request APIs.

### Local service

The local service uses `HttpListener` because the assignment explicitly provides that option.

The server must handle requests asynchronously rather than through blocking request handling.

### Endpoints

The local API follows the assignment's suggested form:

```text
GET /route?drone=Navn
GET /weather
```

Optional extension:

```text
GET /restrictions
```

`/route?drone=Navn` uses the request's `RawUrl`/query data to determine the requested drone.

### Control-tower data

The client obtains:

- route information;
- weather information;
- optional temporary restrictions.

The retrieved information is converted into the final simulation configuration before the drone flight begins.

### Variable response time

The local service may introduce variable response time to simulate slow network conditions.

This is a demonstration mechanism, not a timing correctness requirement.

### Sequential versus concurrent requests

Independent control-tower requests may be performed:

```text
sequentially
```

or:

```text
concurrently
```

Both approaches must produce equivalent functional data.

Performance may be observed, but exact elapsed time is not a correctness oracle.

---

## 9. Public API

### `DroneModel`

```text
Name : string
MaxCheckpoints : int
DelayMs : int
```

Represents drone configuration.

---

### `FlightEvent`

Represents observable flight progress/outcome.

No direct console side effect belongs to the event model.

---

### `DroneFlight.Run`

```text
void Run(DroneModel drone, Action<FlightEvent> report)
```

Purpose:

Executes one synchronous drone flight and reports observable flight events.

Exceptions:

- invalid configuration follows the validation contract in `03-domain-and-rules.md`;
- flight-specific failure follows the final Part B failure contract where applicable.

Side effects:

- invokes `report`;
- does not write directly to the console.

---

### `DroneFlight.RunAsync`

```text
Task RunAsync(DroneModel drone, Action<FlightEvent> report)
```

Purpose:

Executes one asynchronous drone flight and reports observable flight events.

Exceptions:

Flight failures propagate through the returned `Task`.

Side effects:

- invokes `report`;
- does not write directly to the console.

The method must remain asynchronous throughout its execution path.

---

### Part-specific orchestration APIs

Exact class names and final signatures remain open until the corresponding behaviours are implemented.

Required contracts:

- Part A: `Thread` + `Join`;
- Part B: `Task` + TCS + `Task.WhenAll`;
- Part C: `async` + `await Task.WhenAll` + `try/catch`.

---

### `ControlTowerClient` — conditional Part D

Purpose:

Retrieves route, weather and optional restriction data from the control tower.

The final public methods and exact exception contract are still open.

Side effects:

- performs asynchronous HTTP requests;
- may produce request lifecycle logging if the final Part D scope includes it.

The client should reuse its `HttpClient`.

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

This allows tests to verify behaviour without making console output the system-under-test boundary.

Concurrent console ordering remains intentionally non-deterministic.

For concurrency tests, the oracle must prove meaningful overlap or coordination rather than merely checking that all operations eventually completed.

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

```text
FinalMaxCheckpoints =
min(RouteMaxCheckpoints, RestrictionMaxCheckpoints)
```

Otherwise:

```text
FinalMaxCheckpoints =
RouteMaxCheckpoints
```

Weather changes the final `DelayMs` according to the mapping defined in `03-domain-and-rules.md`.

---

## 12. Implementation-specific contracts

The following requirements are intentionally verified through implementation inspection as well as observable behaviour:

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
| `R21` | Part B/C comparison | `AC-C6` | Comparison behaviour |
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
| `PD4` | Async HTTP consumption | `AC-D3` | `B-D03` |
| `PD5` | Simulation effect | `AC-D4` | `B-D04` |
| `PD6` | HTTP failure handling | `AC-D5` | `B-D05` |
| `PD7` | Timeout handling | `AC-D6` | `B-D06` |
| `PD8` | Non-blocking HTTP flow | `AC-D7` | `B-D07` |
| `PD9` | Restrictions | `AC-D8` | `B-D08` |
| `PD10` | HTTP lifecycle logging | `AC-D9` | `B-D09` |
| `PD11` | Sequential/concurrent comparison | `AC-D10` | `B-D10` |
| `PD12` | Variable response time | `AC-D11` | `B-D11` |
| `PD13` | Drone registration bonus | — | Optional extension |

Verification/test IDs are maintained in `AsyncDroneDash.Tests/TestPlan.md`.

---

## 14. Risks

| Risk | Response |
|---|---|
| Thread scheduling is non-deterministic | Test completion/overlap semantics; manually observe output |
| Console output is unstable | Test structured events; manually observe interleaving |
| Timing tests become flaky | Avoid wall-clock correctness assertions |
| Part B failure semantics are misunderstood | Use explicit failure contract and focused tests |
| `Task.WhenAll` failure semantics are misunderstood | Test combined success and fault states |
| Async flow becomes blocking | Inspect for `.Wait()` / `.Result` |
| HTTP tests depend on network | Use controllable local service/test boundary |
| New Part D features expand scope | Keep A–C as protected MVP |
| Architecture becomes over-engineered | Add components only for concrete responsibilities |

---

## 15. Open design decisions

- Exact Part B failure trigger.
- Exact Part B failure exception contract.
- Exact validation exception/interaction boundary.
- Exact Part D JSON response schemas.
- Exact Part D HTTP exception contract.
- Exact weather-to-delay mapping.
- Exact restriction data contract.
- Final orchestration class names and signatures.

These must be resolved before the dependent test contracts are finalized.

---

## 16. First behaviour

**VB05 — Report checkpoint 0**

Traceability:

`R3 → AC-CORE-3 → B2 → VB05`

Scenario:

```text
Given a valid drone with MaxCheckpoints = 0
When the basic flight executes
Then checkpoint 0 is observable
```

Current observation boundary:

```text
DroneFlight.Run(DroneModel, Action<FlightEvent>)
```

The first test should verify the observable behaviour, not console formatting.

---

# Status

### Phase 4 — Traceability

- [x] `R1–R25` mapped to acceptance criteria.
- [x] `R1–R25` mapped to behaviours.
- [x] Conditional `PD1–PD13` mapped.
- [x] Part C acceptance criteria separated.
- [x] Core delay has its own acceptance criterion.
- [ ] Final verification IDs synchronized in TestPlan.

### Phase 5 — Design

- [x] Responsibilities identified.
- [x] Dependency direction established.
- [x] Public API identified.
- [x] FlightEvent observation boundary established.
- [x] Part A–C execution models separated.
- [x] Part D client/server direction established.
- [x] HttpClient reuse specified.
- [x] Async HTTP direction specified.
- [ ] Final Part D response contract.
- [ ] Final exception contracts.
- [ ] Final Part-specific orchestration signatures.