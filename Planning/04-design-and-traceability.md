# Async Drone Dash — Design and Traceability

## 1. Design direction

The solution should remain small and make the differences between Parts A–C visible.

| Part | Execution | Coordination |
|---|---|---|
| A | `Thread` | `Join` |
| B | `Task` + `TaskCompletionSource` | `Task.WhenAll` |
| C | `async`/`await` | `await Task.WhenAll` |

The design should not hide these differences behind abstractions that make the assignment harder to demonstrate.

Additional layers or abstractions require a concrete responsibility.

---

## 2. Responsibilities

### Console / Menu

- display menu;
- accept selection;
- start the selected demonstration;
- present results/errors;
- return to menu or exit.

Does not contain drone-flight or concurrency logic.

### DroneModel

Represents:

- `Name`
- `MaxCheckpoints`
- `DelayMs`

### FlightEvent

Represents an observable flight event.

Initial event types:

- `Started`
- `CheckpointReached`
- `Completed`
- `Faulted`

Checkpoint events contain the checkpoint number.

### DroneFlight

Owns one drone's flight behaviour:

- start;
- checkpoint progression;
- configured delay;
- progress reporting;
- completion;
- failure.

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

### Optional HTTP component

Owns Part D control-tower communication.

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
```

Part D:

```text
Part D orchestration
      ↓
ControlTowerClient
      ↓
HttpClient
      ↓
Local Control Tower
```

No additional architectural layers are currently justified.

---

## 4. Shared flight behaviour

Parts A–C use the same underlying flight concept:

1. validate the drone configuration;
2. report `Started`;
3. report checkpoint `0`;
4. for each remaining checkpoint:
   - apply `DelayMs`;
   - advance;
   - report `CheckpointReached`;
5. report `Completed`.

For `MaxCheckpoints = 0`, checkpoint `0` is reported and no intermediate delay is required.

Execution and coordination differ between Parts A–C, but the domain behaviour remains comparable.

---

## 5. Part A

Normal flow:

```text
Create drones
→ create one Thread per drone
→ start Threads
→ execute DroneFlight
→ Join all Threads
→ report overall completion
```

The no-`Join` demonstration uses the same flight scenario but deliberately omits the waiting step.

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

The deterministic demonstration failure is:

`InvalidOperationException("Simulated drone failure.")`

The failure is introduced by the Part B scenario, not by adding a permanent failure property to `DroneModel`.

---

## 7. Part C

```text
Create drones
→ start async flights
→ await Task.Delay between checkpoints
→ await Task.WhenAll
→ handle orchestration failure with try/catch
```

The async path must not use `.Wait()` or `.Result`.

---

## 8. Part D

Part D uses a local `HttpListener` control tower and `HttpClient` as the client.

The control tower provides:

```text
GET /api/routes/{droneName}
GET /api/weather
GET /api/restrictions
```

The three data sources are independent and may be requested sequentially or concurrently.

### ControlTowerClient

Responsible for:

- HTTP requests;
- response deserialization;
- timeout/error handling;
- exposing route, weather and restriction data.

### Part D orchestration

Responsible for:

- obtaining the three data sources;
- coordinating sequential/concurrent requests;
- combining the data;
- producing final simulation values.

`DroneFlight` remains independent of HTTP.

---

## 9. Public API

### `DroneModel`

```text
Name : string
MaxCheckpoints : int
DelayMs : int
```

### `FlightEvent`

Represents an observable flight event.

### `DroneFlight.Run`

```text
void Run(DroneModel drone, Action<FlightEvent> report)
```

Executes one synchronous drone flight and reports observable events.

`DroneFlight` does not write directly to the console.

### `DroneFlight.RunAsync`

```text
Task RunAsync(DroneModel drone, Action<FlightEvent> report)
```

Executes one asynchronous drone flight and reports observable events.

Failure propagates through the returned task.

### Part-specific orchestration APIs

Exact class names and final signatures remain open until their behaviours are implemented.

Required contracts:

- Part A: `Thread` + `Join`;
- Part B: `Task` + TCS + `Task.WhenAll`;
- Part C: `async` + `await Task.WhenAll` + `try/catch`;
- Part D: asynchronous HTTP communication through `HttpClient`.

---

## 10. Observability

`FlightEvent` provides the deterministic observation boundary for tests.

The application can transform events into console output.

```text
Flight behaviour
      ↓
FlightEvent
      ↓
Orchestration / Console
```

This keeps console output out of the core flight logic.

Exact concurrent console ordering is not a contract.

---

## 11. Part D data flow

```text
RouteData
    ↓
Base MaxCheckpoints

WeatherData
    ↓
Delay adjustment

RestrictionData
    ↓
Maximum checkpoint restriction
```

Final values:

```text
FinalMaxCheckpoints =
    min(Route.MaxCheckpoints, Restriction.MaxCheckpoints)
```

when a restriction exists.

Otherwise:

```text
FinalMaxCheckpoints =
    Route.MaxCheckpoints
```

Weather modifies the configured delay according to the project rules in `03-domain-and-rules.md`.

---

## 12. Testability

### Automated behaviour

- configuration;
- checkpoint progression;
- checkpoint order;
- completion;
- flight events;
- deterministic Part B failure;
- task completion/failure;
- async completion/failure;
- HTTP response mapping;
- Part D simulation mapping;
- HTTP failure/timeout.

### Implementation inspection

- `Thread`;
- `Join`;
- `Task`;
- `TaskCompletionSource`;
- `Task.WhenAll`;
- `async`;
- `Task.Delay`;
- `await Task.WhenAll`;
- `try/catch`;
- no synchronous blocking;
- `HttpClient`;
- selected local HTTP technology.

### Manual observation

- no-`Join` demonstration;
- non-deterministic console output;
- sequential/concurrent HTTP demonstration where applicable.

Detailed test scenarios remain in:

`AsyncDroneDash.Tests/TestPlan.md`

---

## 13. Requirement traceability

| ID | Requirement | Acceptance criterion | Behaviour | Verification |
|---|---|---|---|---|
| `R1` | Runnable C# console application | `AC-DLV-1` | Project runs | `D01` |
| `R2` | Required `DroneModel` properties | `AC-CORE-1` | `B1` | `I01`, `T01` |
| `R3` | Progress `0..MaxCheckpoints` | `AC-CORE-2` | `B2`, `B3` | `T04`, `T05` |
| `R4` | Apply configured delay | `AC-A3` | `B4` | `T07`, `I02` |
| `R5` | Report start/progress/completion | `AC-CORE-3` | `B5` | `T08`, `T13` |
| `R6` | Part A uses separate Threads | `AC-A1` | `B6` | `T10`, `I03` |
| `R7` | Part A uses Join | `AC-A2` | `B7` | `T11`, `I04` |
| `R8` | Demonstrate no Join | `AC-A4` | `B8` | `M01`, `I05` |
| `R9` | Demonstrate concurrent output | `AC-A5` | `B9` | `M02` |
| `R10` | Part B uses Task | `AC-B1` | `B10` | `T15`, `I06` |
| `R11` | One TCS per drone | `AC-B2` | `B11` | `T16`, `I07` |
| `R12` | Part B uses Task.WhenAll | `AC-B3` | `B12` | `T17`, `T21`, `I08` |
| `R13` | Part B failure scenario | `AC-B4` | `B13` | `T18` |
| `R14` | Part B failure propagation | `AC-B5` | `B14` | `T19`, `T21` |
| `R15` | Task.Exception demonstrated | `AC-B6` | `B15` | `T20`, `I09` |
| `R16` | Part C async flight | `AC-C1` | `B16` | `T22`, `I10` |
| `R17` | Part C uses await Task.Delay | `AC-C2` | `B17` | `T23`, `I11` |
| `R18` | Multiple async flights | `AC-C3` | `B18` | `T24` |
| `R19` | Part C uses await Task.WhenAll | `AC-C3` | `B19` | `T25`, `I12` |
| `R20` | Part C uses try/catch | `AC-C4` | `B20` | `T26`, `I13` |
| `R21` | Compare Part C with Part B | `AC-C5` | Comparison | `D05` |
| `R22` | Menu for Parts A–D | `AC-DLV-2` | Menu | `M03` |
| `R23` | GitHub repository | `AC-DLV-5` | Delivery | `D06` |
| `R24` | README requirements | `AC-DLV-3` | Documentation | `D07`, `D08` |
| `R25` | Reflection requirements | `AC-DLV-4` | Reflection | `D09` |

Detailed Part D verification is maintained separately in `AsyncDroneDash.Tests/TestPlan.md`.

---

## 14. Risks

| Risk | Response |
|---|---|
| Thread scheduling is non-deterministic | Test completion semantics; observe race output manually |
| Console output is unstable | Test structured events; manually observe interleaving |
| Real delays make tests slow/flaky | Avoid wall-clock assertions |
| TCS/WhenAll semantics are misunderstood | Use focused tests before implementation |
| Async flow becomes blocking | Inspect for `.Wait()` / `.Result` |
| Part D expands scope | Complete A–C before significant optional work |
| Architecture becomes over-engineered | Add layers only for concrete responsibilities |
| HTTP tests become network-dependent | Use the local control tower / controllable HTTP boundary |

---

## 15. Open decisions

- exact Part D response JSON contract;
- exact public HTTP exception contract;
- final orchestration class names/signatures;
- exact weather-to-delay mapping location;
- exact no-`Join` presentation;
- whether any additional abstraction becomes necessary.

These should be resolved before dependent test contracts are finalized.

---

## 16. First behaviour

**VB05 — Report the first checkpoint**

Traceability:

`R3 → AC-CORE-2 → VB05 → T05`

Scenario:

```text
Given a valid drone with MaxCheckpoints = 0
When the basic flight is executed
Then checkpoint 0 is observable
```

Current interaction:

```text
DroneFlight.Run(DroneModel, Action<FlightEvent>)
```

The first TDD cycle can therefore observe structured flight events without depending on console output.