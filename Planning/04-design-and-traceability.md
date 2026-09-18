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

Owns Part D control-tower communication if implemented.

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
HTTP component
      ↓
Control Tower API
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

The normal and demonstration paths must remain distinguishable.

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

The failure trigger must be deterministic.

Exact failure and exception contracts remain open until finalized.

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

## 8. Public API

The first behaviour needs a deterministic observation boundary.

### `DroneModel`

```text
Name : string
MaxCheckpoints : int
DelayMs : int
```

### `FlightEvent`

Represents one observable flight event.

The exact final property set may evolve if testing reveals a better observation boundary.

### `DroneFlight.Run`

```text
void Run(DroneModel drone, Action<FlightEvent> report)
```

Purpose:

Executes one synchronous drone flight and reports observable events.

Side effect:

Invokes `report`.

Console output is not produced by `DroneFlight`.

### `DroneFlight.RunAsync`

```text
Task RunAsync(DroneModel drone, Action<FlightEvent> report)
```

Purpose:

Executes one asynchronous drone flight and reports observable events.

Side effect:

Invokes `report`.

Failure propagates through the returned task.

### Part-specific orchestration

Exact class names and final signatures remain open until their behaviours are implemented.

Required contracts:

- Part A: `Thread` + `Join`;
- Part B: `Task` + TCS + `Task.WhenAll`;
- Part C: `async` + `await Task.WhenAll` + `try/catch`.

---

## 9. Observability

`FlightEvent` provides the deterministic observation boundary for tests.

The application can transform events into console output.

The design therefore separates:

```text
Flight behaviour
      ↓
FlightEvent
      ↓
Orchestration / Console output
```

This allows automated verification without making `Console.WriteLine` the core flight API.

Exact concurrent console ordering is not a contract.

---

## 10. Testability

### Automated behaviour

- configuration;
- checkpoint progression;
- checkpoint order;
- completion;
- flight events;
- deterministic Part B failure;
- task completion/failure;
- async completion/failure.

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
- no synchronous blocking.

### Manual observation

- no-`Join` demonstration;
- non-deterministic/interleaved console output.

Detailed test scenarios are in:

`AsyncDroneDash.Tests/TestPlan.md`

---

## 11. Requirement traceability

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

Detailed test definitions live in `AsyncDroneDash.Tests/TestPlan.md`.

---

## 12. Part D traceability

Part D is optional.

| ID | Optional behaviour | Verification |
|---|---|---|
| `D-B1` | Route retrieval | `D10` |
| `D-B2` | Weather retrieval | `D11` |
| `D-B3` | Restrictions retrieval | `D12` |
| `D-B4` | Data affects simulation | `D13` |
| `D-B5` | HTTP failure handling | `D14` |
| `D-B6` | Timeout handling | `D15` |
| `D-B7` | HTTP logging | `D16` |
| `D-B8` | Concurrent HTTP calls | `D17` |
| `D-B9` | Sequential/concurrent comparison | `D18` |

---

## 13. Risks

| Risk | Response |
|---|---|
| Thread scheduling is non-deterministic | Test completion semantics; observe race output manually |
| Console output is unstable | Test structured events; manually observe interleaving |
| Real delays make tests slow/flaky | Use deterministic observation seams; avoid wall-clock assertions |
| TCS/WhenAll semantics are misunderstood | Verify with focused tests/spikes |
| Async flow becomes blocking | Inspect for `.Wait()` / `.Result` |
| Part D expands scope | Complete A–C before committing significant D work |
| Architecture becomes over-engineered | Add layers only when a concrete responsibility requires them |

---

## 14. Open decisions

- exact validation contract;
- exact Part B failure trigger/exception;
- final Part A/B/C orchestration class names and signatures;
- exact no-`Join` presentation;
- whether any additional abstraction is justified;
- final Part D architecture if implemented.

Open decisions should be resolved when the dependent behaviour requires them rather than being invented prematurely.

---

## 15. First behaviour

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

The first TDD cycle can therefore use structured flight events rather than console output.

---

# Status

### Phase 4 — Traceability and risk

- [x] Requirements have IDs.
- [x] Requirements connect to acceptance criteria.
- [x] Acceptance criteria connect to behaviours.
- [x] Verification connects through to `TestPlan.md`.
- [x] Major risks identified.

### Phase 5 — Solution design

- [x] Responsibilities identified.
- [x] Dependency direction established.
- [x] Parts A–C separated by execution model.
- [x] First public API defined.
- [x] Deterministic observation boundary defined.
- [x] Testability considered without adding unnecessary layers.
- [ ] Validation contracts finalized.
- [ ] Part B failure contract finalized.
- [ ] Part-specific orchestration APIs finalized when their behaviours are implemented.
- [ ] Part D architecture finalized if Part D enters implementation.