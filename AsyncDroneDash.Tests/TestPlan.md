# Async Drone Dash — Test Plan

## 1. Purpose and testing strategy

This plan turns the behaviours in `Planning/05-behaviour-design.md` into a concrete verification plan.

The test strategy deliberately uses different evidence types because this assignment contains both ordinary behaviour and explicit implementation requirements.

| Evidence type | Used for |
|---|---|
| Unit test | Deterministic drone rules and focused behaviour that can be isolated |
| Component/integration test | Multi-drone orchestration and HTTP behaviour where several production components must work together |
| Manual observation | Intentionally non-deterministic console/concurrency demonstrations |
| Implementation inspection | Assignment requirements that explicitly require `Thread`, `Join`, `TaskCompletionSource`, `Task.Delay`, `await Task.WhenAll`, etc. |
| Documentation review | README, reflection, repository and menu/delivery requirements |

The test inventory is a map, not a command to implement the entire future test suite before starting TDD. The first TDD cycle uses one small behaviour from the inventory; the inventory is updated as the design becomes clearer.

Part D is optional in the assignment, but it is a project target. Its test scenarios are therefore planned separately and become active if Part D remains in scope after the MVP.

---

# 2. Test matrix

The matrix considers the scenario categories required by the workflow. `—` means the category is not meaningful for that behaviour rather than an unreviewed omission.

| Behaviour | Happy | Boundary | Equivalence | Empty/null | Missing | Duplicate | State | Data variation | Dependency failure | Evidence |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| VB01 Valid configuration | ✓ | — | — | — | — | — | — | ✓ | — | Unit |
| VB02 Negative `MaxCheckpoints` | — | ✓ | ✓ | — | — | — | — | ✓ | — | Unit |
| VB03 Negative `DelayMs` | — | ✓ | ✓ | — | — | — | — | ✓ | — | Unit |
| VB04 Blank/missing name | — | ✓ | ✓ | ✓ | ✓ | — | — | ✓ | — | Unit |
| VB05 First checkpoint | ✓ | ✓ (`Max=0`) | — | — | — | — | Running | ✓ | — | Unit |
| VB06 Checkpoint progression | ✓ | ✓ (0/final) | ✓ | — | — | — | Running | ✓ | — | Unit |
| VB07 Successful completion | ✓ | ✓ (`Max=0`) | — | — | — | — | Running→Completed | ✓ | — | Unit/component |
| VB08 Concurrent Part A flights | ✓ | — | — | — | — | — | Running | ≥2 drones | — | Component + inspection |
| VB09 Part A `Join` | ✓ | — | — | — | — | — | Running→Completed | ≥2 drones | — | Component + inspection |
| VB10 Part A without `Join` | — | — | — | — | — | — | Running | ≥2 drones | — | Manual + inspection |
| VB11 Concurrent output | — | — | — | — | — | — | — | ≥2 drones | — | Manual |
| VB12 Part B task completion | ✓ | — | — | — | — | — | Running→Completed | ≥1 drone | — | Component |
| VB13 One TCS per drone | ✓ | — | — | — | — | — | — | ≥2 drones | — | Inspection |
| VB14 Part B `Task.WhenAll` | ✓ | — | — | — | — | — | Running→Completed | ≥2 tasks | — | Component + inspection |
| VB15 Deterministic Part B failure | — | — | — | — | — | — | Running→Faulted | — | — | Component |
| VB16 Part B failure propagation | — | — | — | — | — | — | Running→Faulted | — | — | Component + inspection |
| VB17 `Task.Exception` | — | — | — | — | — | — | Faulted | — | — | Component + inspection |
| VB18 Async flight | ✓ | — | — | — | — | — | Running→Completed | ≥1 drone | — | Component |
| VB19 Async checkpoint delay | ✓ | — | — | — | — | — | Running | ✓ | — | Inspection + component |
| VB20 Multiple async flights | ✓ | — | — | — | — | — | Running→Completed | ≥2 drones | — | Component |
| VB21 Async `Task.WhenAll` | ✓ | — | — | — | — | — | Running→Completed | ≥2 tasks | — | Component + inspection |
| VB22 Async failure handling | — | — | — | — | — | — | Running→Faulted | — | — | Component + inspection |

### Category decisions

Duplicate-data testing is not currently applicable to the core domain because the assignment defines no duplicate-drone or duplicate-record behaviour.

Missing/not-found testing is only applicable if a lookup/registry is introduced. The current MVP passes drone objects/configuration directly and has no required lookup contract.

Null testing is only applicable where the final public API can actually receive `null`. It should not be invented as a test merely because the category exists in the checklist.

Equivalence partitioning is useful for numeric validation (`negative`, `zero`, `positive`) and, where appropriate, name validation (`blank`, `non-blank`). It is not useful for every concurrency behaviour.

---

# 3. Mandatory test inventory

## Core drone behaviour

| ID | Behaviour | Test level | Form | Oracle | Candidate |
|---|---|---|---|---|---|
| T01 | VB01 valid configuration | Unit | Fact | Valid configuration is accepted and exposes the supplied values | `Drone_ValidConfiguration_ShouldBeAccepted` |
| T02 | VB05 `MaxCheckpoints = 0` | Unit | Fact | Checkpoint `0` is observable and flight can complete | `Drone_ZeroMaxCheckpoints_ShouldReportZeroAndComplete` |
| T03 | VB06 checkpoint progression | Unit | Theory + structured data | Observed sequence equals `0..MaxCheckpoints` | `Drone_Checkpoints_ShouldProgressFromZeroToMax` |
| T04 | VB02 negative `MaxCheckpoints` | Unit | Theory | Final validation contract is observed | `Drone_NegativeMaxCheckpoints_ShouldBeRejected` |
| T05 | VB03 negative `DelayMs` | Unit | Theory | Final validation contract is observed | `Drone_NegativeDelayMs_ShouldBeRejected` |
| T06 | VB04 blank/missing name | Unit | Theory | Final validation contract is observed | `Drone_BlankName_ShouldBeRejected` |
| T07 | VB07 successful completion | Unit/component | Fact | Completion occurs only after final checkpoint | `Drone_SuccessfulFlight_ShouldCompleteAfterFinalCheckpoint` |

## Part A — Thread Race

| ID | Behaviour | Test level | Form | Oracle | Candidate |
|---|---|---|---|---|---|
| T08 | VB08 multiple drone flights use concurrent threads | Component + inspection | Fact | Required drone work executes on separate threads; no single-thread implementation is accepted | `ThreadRace_MultipleDrones_ShouldComplete` + inspect `Thread` usage |
| T09 | VB09 `Join` waits | Component + inspection | Fact | `Run` does not report overall completion before every required drone has completed | `ThreadRace_WithJoin_ShouldWaitForAllDrones` |
| T10 | VB10 no-`Join` demonstration | Manual + inspection | Observation | Main-thread continuation can be observed before all drone work finishes | Manual demo + inspect no-`Join` path |
| T11 | VB11 non-deterministic output | Manual | Observation | Concurrent output can be observed as interleaved or reordered; exact order is not asserted | Manual race demonstration |

T08/T09 should use a deterministic observation seam (for example captured output or another controllable completion signal) rather than asserting a particular wall-clock duration.

The implementation must still be inspected because a normal behaviour assertion alone cannot prove that the required mechanism is specifically `Thread`/`Join`.

## Part B — Task + TaskCompletionSource

| ID | Behaviour | Test level | Form | Oracle | Candidate |
|---|---|---|---|---|---|
| T12 | VB12 task represents successful drone completion | Component | Fact | Successful drone operation completes its task | `TaskFlight_SuccessfulDrone_ShouldComplete` |
| T13 | VB13 one TCS per drone | Inspection | — | Source code clearly creates/owns one `TaskCompletionSource` for each participating drone | Inspection record |
| T14 | VB14 `Task.WhenAll` waits for all tasks | Component | Fact | Combined completion occurs only after all participating drone tasks complete | `TaskFlight_WhenAll_ShouldWaitForAllDrones` |
| T15 | VB15 deterministic failure | Component | Fact | Selected failure condition produces a faulted operation | `TaskFlight_ConfiguredFailure_ShouldFaultTask` |
| T16 | VB16 failure propagation | Component | Fact | Fault reaches the orchestration boundary | `TaskFlight_Failure_ShouldPropagate` |
| T17 | VB17 `Task.Exception` | Component + inspection | Fact | Faulted task exposes the expected exception information through `Task.Exception` | `TaskFlight_FaultedTask_ShouldExposeException` |

## Part C — Async/Await

| ID | Behaviour | Test level | Form | Oracle | Candidate |
|---|---|---|---|---|---|
| T18 | VB18 successful async flight | Component | Fact | Async flight completes with the expected checkpoint/result behaviour | `AsyncFlight_ValidDrone_ShouldComplete` |
| T19 | VB19 asynchronous checkpoint delay | Component + inspection | Fact | Flight uses asynchronous delay between checkpoints; no synchronous wait is introduced | `AsyncFlight_ShouldUseAsyncDelay` + inspect implementation |
| T20 | VB20 multiple async flights | Component | Fact | Multiple participating flights can complete without forced sequential orchestration | `AsyncFlight_MultipleDrones_ShouldCompleteAll` |
| T21 | VB21 async `Task.WhenAll` | Component + inspection | Fact | Combined operation is awaited and completes after participating flights | `AsyncFlight_ShouldAwaitTaskWhenAll` |
| T22 | VB22 async failure handling | Component + inspection | Fact | Flight failure reaches orchestration and is handled by the required `try/catch` | `AsyncFlight_Failure_ShouldBeHandledByOrchestration` |
| T23 | Part C non-blocking implementation requirement | Inspection | — | No `.Wait()` or `.Result` occurs in the async execution path | Inspection record |

---

# 4. Part D test target

Part D is optional in the assignment. The project currently intends to implement it, so these are planned tests rather than mandatory MVP tests.

The test plan should remain independent of a live external demo API. Automated tests should use a controllable local HTTP endpoint or a fake `HttpMessageHandler`; one manual smoke test can verify the chosen real service arrangement.

## Target behaviours

| ID | Scenario | Level | Form | Oracle | Candidate |
|---|---|---|---|---|---|
| D01 | Route data retrieved | Integration | Fact | Valid route response is mapped into the application model | `ControlTower_GetRoute_ShouldReturnRouteData` |
| D02 | Weather retrieved | Integration | Fact | Valid weather response is mapped correctly | `ControlTower_GetWeather_ShouldReturnWeatherData` |
| D03 | Temporary restrictions retrieved | Integration | Fact | Restriction data is mapped correctly, including the no-restriction case if supported | `ControlTower_GetRestrictions_ShouldReturnData` |
| D04 | Control-tower data affects simulation | Integration | Fact / Theory | Documented mapping changes `DelayMs` and/or `MaxCheckpoints` as intended | `ControlTower_Data_ShouldAffectSimulation` |
| D05 | HTTP non-success response | Integration | Fact | Expected error handling occurs; false success is impossible | `ControlTower_NonSuccessResponse_ShouldBeHandled` |
| D06 | HTTP timeout | Integration | Fact | Timeout produces the documented failure behaviour | `ControlTower_Timeout_ShouldBeHandled` |
| D07 | HTTP request logging | Integration/manual | Fact or observation | Required request start and completion/failure events are observable | `ControlTower_HttpRequest_ShouldBeLogged` |
| D08 | Concurrent HTTP calls | Integration | Fact | Independent calls can run concurrently and all required results are collected | `ControlTower_ConcurrentCalls_ShouldComplete` |
| D09 | Sequential vs concurrent calls | Integration + manual | Fact + observation | Both modes produce equivalent required data; relative speed is observed, not a brittle pass/fail threshold | `ControlTower_SequentialAndConcurrent_ShouldReturnEquivalentData` |

If the local HTTP option is chosen, the actual server implementation and startup method must be decided before D01–D09 are locked. README documentation for starting the local service becomes mandatory because the assignment explicitly requires it when a local service is included.

---

# 5. Fact / Theory and data-form decisions

### Use Fact when

- the scenario has its own semantic meaning;
- the test represents one important execution path;
- the scenario is about concurrency/orchestration or a specific failure.

### Use Theory when

- one rule genuinely applies to several representative inputs;
- the datasets express the same behaviour rather than unrelated cases.

### Data form

- `InlineData` for simple scalar boundaries if the test remains readable;
- `MemberData` for structured checkpoint expectations or richer combinations;
- avoid parameterization solely to reduce test-file length.

Likely Theory candidates:

- numeric validation classes;
- blank/invalid name variants if they share the same contract;
- checkpoint progression across several representative `MaxCheckpoints` values.

Likely Fact candidates:

- zero-checkpoint behaviour;
- successful completion;
- Part A/B/C orchestration;
- individual failure scenarios;
- `Task.Exception` observation;
- HTTP timeout/failure;
- concurrent HTTP orchestration.

---

# 6. Test oracle

| Area | Oracle |
|---|---|
| Valid configuration | Configuration is accepted and preserves required values |
| Invalid configuration | Exact documented exception/result once validation contract is finalized |
| Checkpoint progression | Captured sequence is exactly `0..MaxCheckpoints` |
| Completion | Completion is observable only after final checkpoint |
| Part A `Join` | Overall completion follows completion of all required threads |
| No `Join` | Main-thread continuation can be observed before drone completion |
| Concurrent output | Human-observable interleaving/reordering; no fixed order expected |
| Part B success | Combined task completes successfully |
| Part B failure | Affected task is faulted with expected exception information |
| Failure propagation | Orchestration observes the expected failure |
| `Task.Exception` | Expected exception information is available on the faulted task |
| Part C success | Async operation completes with expected flight result |
| Part C failure | Exception reaches the orchestration handler |
| Part D response | Expected data is mapped correctly |
| Part D simulation effect | Exact documented input change is observed |
| Part D HTTP failure/timeout | Documented failure behaviour occurs |
| Part D logging | Required events are captured/observable |

Expected values are written explicitly in tests. Tests must not duplicate the production algorithm simply to calculate the expected result.

---

# 7. Contract decisions required before affected tests are written

The following contracts must be finalized before the first tests that depend on them are implemented.

## Drone validation

- Negative `MaxCheckpoints`: exact response/exception TBD.
- Negative `DelayMs`: exact response/exception TBD.
- Blank/missing `Name`: exact response/exception TBD.
- `null` handling: only required if the final API allows `null`.
- Validation location: model construction or another clearly owned boundary.

## Part B failure

- Exact deterministic trigger: TBD.
- Exact exception type: TBD.
- How the selected failure is exposed to the caller: TBD.

## Part D

- External demo API or local HTTP service: TBD.
- Response models/data contract: TBD.
- Exact mapping from route/weather/restrictions to `DelayMs` / `MaxCheckpoints`: TBD.
- Timeout policy: TBD.
- Logging mechanism: TBD.

No test should encode a guessed contract merely to make the test file compile.

---

# 8. Determinism and timing policy

Tests must not depend on arbitrary wall-clock thresholds or a particular thread scheduling order.

For `DelayMs`, test the observable placement/effect of the delay where the design gives a deterministic observation seam. Inspect the implementation for the required `Task.Delay` mechanism rather than asserting that a test took exactly N milliseconds.

For Part A concurrency, verify stable completion semantics and inspect the required `Thread`/`Join` usage. Treat interleaving as a manual observation.

For Part D sequential/concurrent comparison, automate correctness of the returned data and observe performance/concurrency separately. Do not use a fragile timing threshold as the correctness oracle.

---

# 9. Delivery and documentation verification

These are required deliverables rather than xUnit behaviours.

| Requirement | Verification |
|---|---|
| GitHub repository | Repository check |
| Runnable console application | `dotnet build` + manual run |
| Menu for Parts A–D | Manual smoke test |
| `README.md` running instructions | Documentation review |
| README explains how each part is tested | Documentation review |
| README explains local HTTP startup when a local service is included | Documentation review |
| `reflection.md` contains observations and short answers | Documentation review |

Part D documentation is not an independent feature requirement, but once a local HTTP service is included the required README instructions become part of the assignment delivery contract.

---

# 10. Review of the suite before test code

- [ ] Every mandatory acceptance criterion has a verification path.
- [ ] Every vertical behaviour has either a concrete test candidate or an explicit inspection/manual verification path.
- [ ] Boundary and invalid-input scenarios are only finalized after their contracts are decided.
- [ ] Part B has one deterministic failure mechanism.
- [ ] Part D target scenarios are separated from mandatory MVP coverage.
- [ ] Every automated candidate has a clear oracle.
- [ ] Fact/Theory choices have a reason.
- [ ] Test levels match what each test can actually prove.
- [ ] Exact thread scheduling is not asserted.
- [ ] Exact elapsed time is not used as a fragile correctness oracle.
- [ ] Expected results do not duplicate production logic.
- [ ] Required implementation mechanisms have an inspection path.
- [ ] Duplicate or overlapping tests have been removed.
- [ ] Test data is representative and deliberately chosen.
- [ ] The first test can be written without inventing an undocumented contract.

---

# 11. First TDD candidate

The current first behaviour remains:

**VB05 — Report the first checkpoint**

Planned first test:

`Drone_StartValidFlight_ShouldReportCheckpointZero`

Before writing it, the final public interaction and deterministic output/observation mechanism must be confirmed.

The first cycle is then:

`Select VB05 → write test → RED → minimum GREEN → REFACTOR → update inventory → next behaviour`

The future inventory is a plan, not a requirement to implement the entire project before starting the first TDD cycle.