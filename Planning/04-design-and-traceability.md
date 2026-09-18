# Async Drone Dash — Design and Traceability

## 1. Design direction

| Part | Execution | Coordination |
|---|---|---|
| A | `Thread` | `Join` |
| B | `Task` + `TaskCompletionSource` | `Task.WhenAll` |
| C | `async`/`await` | `await Task.WhenAll` |
| D | `HttpClient` + local `HttpListener` | Sequential or concurrent async calls |

The required execution-model differences remain visible in the implementation.

No additional architectural layers are justified.

---

## 2. Responsibilities

### Console / Menu

Owns:

- menu display;
- selection;
- starting demonstrations;
- user-facing output;
- return/exit flow;
- user-facing handling of failures propagated from Part C orchestration.

Does not own drone-flight or concurrency logic.

### DroneModel

Represents:

- `Name`;
- `MaxCheckpoints`;
- `DelayMs`.

### FlightEvent

Provides the observable boundary between flight logic and presentation/testing.

The observable event data is:

    DroneName  : string
    Type       : FlightEventType
    Checkpoint : int?
    Exception  : Exception?

The observable event shape and event-type rules are defined in `03-domain-and-rules.md`. This document does not redefine that contract.

### DroneFlight

Owns:

- validation;
- checkpoint progression;
- delay;
- flight event reporting;
- completion.

Does not own console output, multi-drone orchestration, or the simulated failure scenario.

### Part A orchestration

Owns:

- Thread creation/start;
- normal `Join`;
- no-Join demonstration;
- overall completion.

### Part B orchestration

Owns:

- one TCS per drone;
- task completion/failure;
- `Task.WhenAll`;
- failure propagation;
- `Task.Exception` observation;
- simulated failure scenario.

### Part C orchestration

Owns:

- async flight startup;
- `await Task.WhenAll`;
- orchestration-level `try/catch`;
- the same deterministic simulated failure scenario defined for Part B.

### ControlTowerClient

Conditional on Part D.

Owns:

- asynchronous HTTP requests;
- response mapping;
- timeout/error translation;
- HTTP request lifecycle logging;
- reusable `HttpClient`;
- use of an explicitly configured `HttpClient.Timeout`.

### ControlTowerOrchestrator

Conditional on Part D.

Owns:

- coordinating independent control-tower calls;
- sequential request execution for the demonstration;
- concurrent request execution for the demonstration;
- combining route, weather, and restriction data for the final simulation configuration.

The sequential and concurrent paths use the same `ControlTowerClient` and produce equivalent final simulation configuration. The difference is the order in which the independent requests are started and awaited.

### Local Control Tower

Conditional on Part D.

Owns:

- local `HttpListener`;
- endpoint routing;
- response generation;
- controlled response delay;
- asynchronous request handling;
- clean shutdown and disposal after the Part D demonstration.

---

## 3. Dependency direction

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

    ControlTowerOrchestrator

          ↓

    ControlTowerClient

          ↓

    HttpClient

          ↓

    Local Control Tower

`DroneFlight` has no HTTP dependency.

---

## 4. Public API

### `DroneFlight.Run`

    void Run(DroneModel drone, Action<FlightEvent> report)

Executes one synchronous flight.

Exceptions:

- invalid configuration uses the validation contract from `03-domain-and-rules.md`.

Side effects:

- reports `FlightEvent`;
- does not write directly to the console.

### `DroneFlight.RunAsync`

    Task RunAsync(DroneModel drone, Action<FlightEvent> report)

Executes one asynchronous flight.

Exceptions propagate through the returned `Task`.

Side effects:

- reports `FlightEvent`;
- does not write directly to the console.

### Part A orchestration

    void RunWithJoin(
        IReadOnlyList<DroneModel> drones,
        Action<FlightEvent> report
    )

    void RunWithoutJoin(
        IReadOnlyList<DroneModel> drones,
        Action<FlightEvent> report
    )

`RunWithJoin` does not return overall completion before all participating threads have finished.

`RunWithoutJoin` deliberately demonstrates the absence of this wait.

### Part B orchestration

    Task TaskFlightRunner.RunAsync(
        IReadOnlyList<DroneModel> drones,
        string? failureDroneName,
        Action<FlightEvent> report
    )

`failureDroneName = null` means no simulated failure.

The selected failure drone fails immediately after reporting checkpoint `1`.

The simulated failure is introduced by the Part B orchestration scenario rather than by `DroneFlight` or a permanent `DroneModel` property.

The runner wraps the flight event callback for the selected failure scenario. When the selected failure drone reports `CheckpointReached(1)`, the runner triggers `InvalidOperationException("Simulated drone failure.")`. The runner then reports the corresponding `Faulted` `FlightEvent` and propagates the failure through the Part B TCS/Task model.

### Part C orchestration

    Task AsyncFlightRunner.RunAsync(
        IReadOnlyList<DroneModel> drones,
        string? failureDroneName,
        Action<FlightEvent> report
    )

The async path remains asynchronous throughout execution.

The failure scenario uses the same deterministic contract as Part B: the selected failure drone reports checkpoint `1` and then produces `InvalidOperationException("Simulated drone failure.")`.

The failure propagates to the Part C orchestration boundary, where the documented error-handling path produces a `Faulted` `FlightEvent` for the failing drone, rethrows the original failure, and does not treat the run as successful.

The runner uses the same failure-trigger mechanism as Part B: when the selected failure drone reports `CheckpointReached(1)`, the runner triggers the simulated failure, reports the corresponding `Faulted` `FlightEvent`, and propagates the original failure to the orchestration error-handling path.

### `ControlTowerClient`

    ControlTowerClient(HttpClient httpClient)

    Task<RouteData> GetRouteAsync(string droneName)

    Task<WeatherData> GetWeatherAsync()

    Task<RestrictionData?> GetRestrictionsAsync()

The client reuses the supplied `HttpClient`.

The supplied `HttpClient` uses an explicitly configured `Timeout` for request timeout handling.

HTTP/dependency failures are translated to `ControlTowerException`.

### `ControlTowerOrchestrator`

    ControlTowerOrchestrator(ControlTowerClient controlTowerClient)

The orchestration boundary provides two equivalent final-configuration paths for the Part D comparison:

    Task<DroneModel> LoadSequentialAsync(DroneModel drone)

    Task<DroneModel> LoadConcurrentAsync(DroneModel drone)

`LoadSequentialAsync` awaits each independent request before starting the next request.

`LoadConcurrentAsync` starts the independent route, weather, and restriction requests before awaiting their combined completion.

Both methods use the same injected `ControlTowerClient` and return the final simulation configuration as a `DroneModel`.

For the same successful control-tower responses, both methods must produce equivalent functional configuration.

### Response models

    RouteData

        MaxCheckpoints : int

    WeatherData

        Condition : string

    RestrictionData

        MaxCheckpoints : int?

### `ControlTowerException`

    ControlTowerException : Exception

    ControlTowerErrorKind Kind { get; }

Supported kinds:

    RequestFailed
    NotFound
    Timeout
    InvalidResponse

The original exception is preserved as `InnerException` where useful.

---

## 5. Part A design

    Create drones

    → create one Thread per drone

    → start all Threads

    → execute DroneFlight

    → Join all Threads

    → report overall completion

No-Join:

    Create and start Threads

    → do not Join

    → main thread continues

Exact scheduling order is not a contract.

The automated concurrency test uses controlled observation/synchronization rather than elapsed-time assumptions.

---

## 6. Part B design

    Create drones

    → create one TCS per drone

    → start drone work

    → selected failure target reaches checkpoint 1

    → report Faulted FlightEvent for the selected failure drone

    → fault target TCS

    → complete/fault remaining TCS values

    → Task.WhenAll

    → observe success/failure

Failure contract:

    InvalidOperationException("Simulated drone failure.")

The failure is introduced by the Part B scenario rather than by a permanent `DroneModel` property.

The selected failure target is detected at its `CheckpointReached(1)` event by the Part B runner's failure-scenario callback.

---

## 7. Part C design

    Create drones

    → start async flights

    → try
        → await Task.WhenAll
    → catch
        → report Faulted FlightEvent for the failing drone
        → rethrow original failure
    → Console / Menu handles propagated failure

`.Wait()` and `.Result` are prohibited in the async execution path.

The selected failure drone reports checkpoint `1` and then produces `InvalidOperationException("Simulated drone failure.")`.

The failure propagates to the Part C orchestration boundary, where it produces a `Faulted` `FlightEvent` for the failing drone, rethrows the original failure, and is handled without treating the run as successful.

The propagated failure is then handled by the Console / Menu boundary for user-facing error reporting.

Concurrent progress is verified through observable behaviour and implementation inspection.

---

## 8. Part D design

Part D is optional in the assignment and is currently included in the final project target.

The selected local service prefix is:

    http://localhost:8080/

The local API is:

    GET /route?drone=Navn

    GET /weather

    GET /restrictions

The `/route` handler reads the drone name from the request URL/query data and uses `RawUrl` as the assignment-specific learning point. The deterministic drone-name-to-route mapping is defined in `03-domain-and-rules.md`; this document treats that domain contract as the source of truth.

The local request loop uses asynchronous request handling.

The client uses asynchronous `HttpClient` APIs and one reusable `HttpClient`.

The finalized JSON shapes, weather mapping, restriction rules and HTTP error categories are defined in `03-domain-and-rules.md`.

The supplied `HttpClient` uses an explicitly configured `Timeout`.

Independent HTTP calls are orchestrated through `ControlTowerOrchestrator` and can be compared sequentially and concurrently.

Lifecycle logging is part of the selected Part D target.

Variable response time is demonstrable; exact elapsed duration is not a correctness oracle.

The local listener is stopped and disposed when the Part D demonstration finishes or the application shuts down.

---

## 9. Observability

`FlightEvent` is the primary observation boundary:

    Flight behaviour

          ↓

    FlightEvent

          ↓

    Orchestration / Console / Tests

Console output itself remains a manual observation for the deliberately non-deterministic race demonstration.

Concurrency verification must prove meaningful overlap/coordination rather than eventual completion alone.

---

## 10. Implementation inspection contracts

| ID | Contract |
|---|---|
| `I01` | `DroneModel` exposes `Name`, `MaxCheckpoints`, and `DelayMs` |
| `I02` | Required checkpoint-delay mechanism is used |
| `I03` | One `Thread` per Part A drone |
| `I04` | `Join` in normal Part A run |
| `I05` | Real no-Join demonstration |
| `I06` | Part B uses `Task` |
| `I07` | One TCS per drone |
| `I08` | Part B uses `Task.WhenAll` |
| `I09` | `Task.Exception` is explicitly observed |
| `I10` | Part C uses an async flight method |
| `I11` | Part C uses `await Task.Delay` |
| `I12` | Part C allows independent flights to overlap |
| `I13` | Part C uses `await Task.WhenAll` |
| `I14` | Part C uses orchestration `try/catch` |
| `I15` | Part C call path contains no synchronous blocking via `.Wait()`, `.Result`, or `.GetAwaiter().GetResult()` |
| `I16` | Part D reuses one `HttpClient` |
| `I17` | Part D uses asynchronous HTTP APIs |
| `I18` | Local `HttpListener` uses asynchronous request handling |
| `I19` | Part D uses the finalized JSON, route-mapping, and error contracts from `03-domain-and-rules.md`, including final simulation configuration mapping, the `ControlTowerOrchestrator` sequential/concurrent boundary, and the configured HTTP timeout |

---

## 11. Traceability

| Requirement | Acceptance criterion | Behaviour | Verification |
|---|---|---|---|
| `R1` | `AC-CORE-1` | Project runs | `DOC01`, `DOC02`, `M03` |
| `R2` | `AC-CORE-2` | `B1`, `VB01` | `T01`, `I01` |
| `R3` | `AC-CORE-3` | `B2/B3`, `VB05/VB06` | `T04`, `T05` |
| `R4` | `AC-CORE-4` | `B4`, `VB07` | `T07`, `I02` |
| `R5` | `AC-CORE-5` | `B5`, `VB08/VB13` | `T08`, `T12` |
| `R6` | `AC-A1` | `B6`, `VB09/VB14` | `T10`, `T14`, `I03` |
| `R7` | `AC-A2` | `B7`, `VB10` | `T11`, `I04` |
| `R8` | `AC-A3` | `B8`, `VB11` | `M01`, `I05` |
| `R9` | `AC-A4` | `B9`, `VB12` | `M02` |
| `R10` | `AC-B1` | `B10`, `VB15` | `T15`, `I06` |
| `R11` | `AC-B2` | `B11`, `VB16` | `I07` |
| `R12` | `AC-B3` | `B12`, `VB17/VB21` | `T17`, `T21`, `I08` |
| `R13` | `AC-B4` | `B13`, `VB18` | `T18` |
| `R14` | `AC-B5` | `B14`, `VB19/VB21` | `T19`, `T21` |
| `R15` | `AC-B6` | `B15`, `VB20` | `T20`, `I09` |
| `R16` | `AC-C1` | `B16`, `VB22` | `T22`, `I10` |
| `R17` | `AC-C2` | `B17`, `VB23` | `I11` |
| `R18` | `AC-C3` | `B18`, `VB24` | `T24`, `I12` |
| `R19` | `AC-C4` | `B19`, `VB25` | `T25`, `I13` |
| `R20` | `AC-C5` | `B20`, `VB26` | `T26`, `I14`, `I15` |
| `R21` | `AC-C6` | `VB27` | `DOC05` |
| `R22` | `AC-DLV-1` | Menu | `M03` |
| `R23` | `AC-DLV-2` | Delivery | `DOC06` |
| `R24` | `AC-DLV-3` | Documentation | `DOC07`, `DOC08` |
| `R25` | `AC-DLV-4` | Reflection | `DOC09` |
| `E1` | `AC-EDGE-1` | `VB02` | `T02` |
| `E2` | `AC-EDGE-2` | `VB03` | `T03` |
| `E3` | `AC-EDGE-3` | `VB04` | `T06` |
| `E4` | `AC-EDGE-4` | `VB-E04` | `HTTP08` |
| `E5` | `AC-EDGE-5` | `VB-E05` | `HTTP05`, `HTTP06`, `HTTP07` |
| `PD1` | `AC-D0` | `VB-D00` | `HTTP00`, `I18`, `I19` |
| `PD2` | `AC-D1` | `VB-D01` | `HTTP01` |
| `PD3` | `AC-D2` | `VB-D02` | `HTTP02` |
| `PD4` | `AC-D3` | `VB-D07` | `I16`, `I17` |
| `PD5` | `AC-D4` | `VB-D04` | `HTTP04` |
| `PD6` | `AC-D5` | `VB-D05` | `HTTP05`, `HTTP07` |
| `PD7` | `AC-D6` | `VB-D06` | `HTTP06` |
| `PD8` | `AC-D7` | `VB-D07/VB-D08` | `I18` |
| `PD9` | `AC-D8` | `VB-D03` | `HTTP03` |
| `PD10` | `AC-D9` | `VB-D09` | `M04` |
| `PD11` | `AC-D10` | `VB-D10` | `HTTP11`, `M04` |
| `PD12` | `AC-D11` | `VB-D11` | `M05` |
| `PD13` | — | Bonus | Optional |

---

## 12. Risks

| Risk | Response |
|---|---|
| Non-deterministic scheduling | Do not assert exact ordering; use controlled observation |
| Timing-based tests become flaky | Never use elapsed duration as correctness oracle |
| Task failure semantics are misunderstood | Test faulted state and underlying exception |
| Async code becomes blocking | Inspect `.Wait()` / `.Result` |
| HTTP tests depend on outside services | Use a controllable local/test boundary |
| Part D expands scope | Protect A–C as MVP |
| Architecture grows unnecessarily | Add only components with concrete responsibility |
| Local `HttpListener` behaves differently by environment | Run the local spike before Part D implementation |

---

## 13. Final design status

Mandatory core design is locked.

Selected Part D contracts are defined in `03-domain-and-rules.md`.

This document owns:

- component responsibilities;
- public API;
- dependency direction;
- observation boundary;
- requirement traceability.

No mandatory design contract is intentionally open.