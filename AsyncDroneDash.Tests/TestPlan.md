# Async Drone Dash — Test Plan

## 1. Purpose

This document defines the verification plan for Async Drone Dash.

The objective is:

> When all mandatory verification items pass, every mandatory assignment requirement has been verified.

Not every requirement belongs in an automated unit test. Verification may be automated, inspected, manually demonstrated, or documented.

Part D is optional in the assignment and active only while it remains in the project's final target scope.

---

## 2. Verification types

| Prefix | Type |
|---|---|
| `T` | Automated test |
| `I` | Implementation inspection |
| `M` | Manual verification |
| `DOC` | Documentation/delivery |
| `HTTP` | Part D HTTP/integration |

---

## 3. Requirement coverage

| Requirement | Acceptance criterion | Verification |
|---|---|---|
| `R1` | `AC-CORE-1` | `DOC01` |
| `R2` | `AC-CORE-2` | `T01`, `I01` |
| `R3` | `AC-CORE-3` | `T04`, `T05` |
| `R4` | `AC-CORE-4` | `T07`, `I02` |
| `R5` | `AC-CORE-5` | `T08`, `T13` |
| `R6` | `AC-A1` | `T10`, `I03` |
| `R7` | `AC-A2` | `T11`, `I04` |
| `R8` | `AC-A3` | `M01`, `I05` |
| `R9` | `AC-A4` | `M02` |
| `R10` | `AC-B1` | `T15`, `I06` |
| `R11` | `AC-B2` | `T16`, `I07` |
| `R12` | `AC-B3` | `T17`, `I08` |
| `R13` | `AC-B4` | `T18` |
| `R14` | `AC-B5` | `T19`, `T21` |
| `R15` | `AC-B6` | `T20`, `I09` |
| `R16` | `AC-C1` | `T22`, `I10` |
| `R17` | `AC-C2` | `T23`, `I11` |
| `R18` | `AC-C3` | `T24`, `I12` |
| `R19` | `AC-C4` | `T25`, `I13` |
| `R20` | `AC-C5` | `T26`, `I14` |
| `R21` | `AC-C6` | `DOC05` |
| `R22` | `AC-DLV-1` | `M03` |
| `R23` | `AC-DLV-2` | `DOC06` |
| `R24` | `AC-DLV-3` | `DOC07`, `DOC08` |
| `R25` | `AC-DLV-4` | `DOC09` |

---

## 4. Core test inventory

### T01 — DroneModel_ValidConfiguration_ShouldBeAccepted

`R2 → AC-CORE-2 → VB01`

Unit / Fact.

A valid drone configuration is accepted and preserves supplied values.

### T02 — DroneModel_NegativeMaxCheckpoints_ShouldBeRejected

`VB02`

Unit / Theory.

Representative negative values.

Oracle: `ArgumentOutOfRangeException`.

### T03 — DroneModel_NegativeDelayMs_ShouldBeRejected

`VB03`

Unit / Theory.

Representative negative values.

Oracle: `ArgumentOutOfRangeException`.

### T04 — DroneFlight_ShouldReportAllCheckpointsInOrder

`R3 → AC-CORE-3 → VB06`

Unit / Theory.

Representative values: `0`, `1`, `3`.

Oracle: exact checkpoint sequence `0..MaxCheckpoints`.

### T05 — DroneFlight_ZeroMaxCheckpoints_ShouldReportZeroAndComplete

`R3 → AC-CORE-3 → VB05`

Unit / Fact.

Oracle: `CheckpointReached(0)` followed by completion.

### T06 — DroneFlight_BlankName_ShouldBeRejected

`R2 → AC-CORE-2 → VB04`

Unit / Theory.

Representative values: `null`, empty, whitespace.

Oracle: `ArgumentException`.

### T07 — DroneFlight_ShouldApplyConfiguredDelayBetweenCheckpoints

`R4 → AC-CORE-4 → VB07`

Unit/component plus inspection.

Oracle: configured delay mechanism occurs between steps.

Exact elapsed time is not tested.

### T08 — DroneFlight_ShouldReportStartCheckpointsAndCompletion

`R5 → AC-CORE-5 → VB08`

Unit/component / Fact.

Oracle: start, every checkpoint, completion in the event stream.

---

## 5. Part A

### T10 — ThreadRace_ShouldCompleteAtLeastTwoDrones

`R6 → AC-A1 → VB09`

Component / Fact.

Oracle: all required drones complete. Separate-thread execution is additionally inspected.

### T11 — ThreadRace_WithJoin_ShouldWaitForAllDrones

`R7 → AC-A2 → VB10`

Component / Fact.

Oracle: overall completion cannot occur before all required drones complete.

Use controlled synchronization, not arbitrary timing thresholds.

### T12 — ThreadRace_EachDrone_ShouldReportItsOwnProgress

`R3/R5 → VB06/VB08`

Component / Fact.

At least two drones with different checkpoint counts.

Oracle: each drone has its own correct sequence.

### T13 — ThreadRace_ShouldReportLifecycleForEachDrone

`R5 → AC-CORE-5 → VB08`

Component / Fact.

Oracle: start, every checkpoint and completion for every drone.

Exact cross-thread ordering is not asserted.

---

## 6. Part A manual/inspection

### M01 — ThreadRace_WithoutJoin_ShouldDemonstratePrematureContinuation

`R8 → AC-A3 → VB11`

Observe the main thread continuing before all drone threads finish.

### M02 — ThreadRace_ShouldDemonstrateInterleavedConsoleOutput

`R9 → AC-A4 → VB12`

Observe interleaving/reordering of concurrent console output.

### M03 — Menu_ShouldExposePartsAThroughD

`R22 → AC-DLV-1`

Observe menu and each part's entry path.

### I03 — PartA_UsesOneThreadPerDrone

`R6`

Inspect separate `Thread` instances.

### I04 — PartA_UsesJoin

`R7`

Inspect `Join` in normal orchestration.

### I05 — PartA_ContainsRealNoJoinPath

`R8`

Inspect deliberate no-Join demonstration.

---

## 7. Part B

### T15 — TaskFlight_SuccessfulDrone_ShouldCompleteTask

`R10 → AC-B1 → VB13`

Component / Fact.

Oracle: task reaches successful completion.

### T16 — TaskFlight_ShouldMaintainIndependentCompletionPerDrone

`R11 → AC-B2 → VB14`

Component / Fact.

Oracle: each drone has an independent completion outcome.

### T17 — TaskFlight_WhenAll_ShouldWaitForAllDrones

`R12 → AC-B3 → VB15`

Component / Fact.

Oracle: combined completion follows all participating tasks.

### T18 — TaskFlight_ConfiguredFailure_ShouldFaultAffectedOperation

`R13 → AC-B4 → VB16`

Component / Fact.

Oracle: selected failure produces `InvalidOperationException("Simulated drone failure.")`.

### T19 — TaskFlight_Failure_ShouldReachOrchestration

`R14 → AC-B5 → VB17`

Component / Fact.

Oracle: failure reaches orchestration and is not silently swallowed.

### T20 — TaskFlight_FaultedTask_ShouldExposeExpectedException

`R15 → AC-B6 → VB18`

Component / Fact.

Oracle:

- task is `Faulted`;
- `Task.Exception` is non-null;
- aggregate contains the expected underlying exception.

### T21 — TaskFlight_WhenAll_WithFailure_ShouldRepresentCombinedFailure

`R12/R14 → VB15/VB17`

Component / Fact.

One task faults while another is incomplete.

Oracle:

- failing task faults;
- remaining task reaches terminal state;
- combined operation does not report false success.

### I06 — PartB_UsesTask

`R10`

Inspect Task-based representation.

### I07 — PartB_UsesOneTCSPerDrone

`R11`

Inspect one TCS per participating drone.

### I08 — PartB_UsesTaskWhenAll

`R12`

Inspect `Task.WhenAll`.

### I09 — PartB_ObservesTaskException

`R15`

Inspect explicit `Task.Exception` observation.

---

## 8. Part C

### T22 — AsyncFlight_ValidDrone_ShouldComplete

`R16 → AC-C1 → VB19`

Component / Fact.

Oracle: async flight completes successfully.

### T23 — AsyncFlight_ShouldUseConfiguredAsyncDelay

`R17 → AC-C2 → VB20`

Component plus inspection / Fact.

Oracle: checkpoint delay uses `await Task.Delay` without synchronous blocking.

### T24 — AsyncFlight_MultipleDrones_ShouldMakeOverlappingProgress

`R18 → AC-C3 → VB21`

Component / Fact.

Oracle: multiple flights make meaningful overlapping progress.

The test must establish overlap without depending on one exact scheduling order.

### T25 — AsyncFlight_ShouldAwaitTaskWhenAll

`R19 → AC-C4 → VB22`

Component / Fact.

Oracle: overall completion follows all required async flights.

### T26 — AsyncFlight_Failure_ShouldBeHandledByOrchestration

`R20 → AC-C5 → VB23`

Component / Fact.

Oracle: controlled failure reaches the orchestration `try/catch`.

### I10 — PartC_UsesAsyncFlightMethod

`R16`

Inspect actual async method returning an appropriate Task type.

### I11 — PartC_UsesAwaitTaskDelay

`R17`

Inspect `await Task.Delay`.

### I12 — PartC_AllowsConcurrentProgress

`R18`

Inspect that independent flights are not intentionally serialized.

### I13 — PartC_UsesAwaitTaskWhenAll

`R19`

Inspect `await Task.WhenAll`.

### I14 — PartC_UsesTryCatch

`R20`

Inspect orchestration-level `try/catch`.

### I15 — PartC_ContainsNoSynchronousBlocking

Inspect that `.Wait()` and `.Result` do not block the async path.

---

## 9. Part B / Part C comparison

### DOC05 — Reflection_ShouldComparePartBAndPartC

`R21 → AC-C6 → VB24`

Review final implementation and reflection.

Pass condition:

Comparison addresses boilerplate, complexity, readability and maintainability.

---

## 10. Delivery verification

### DOC01 — Project_ShouldBuild

`R1`

`dotnet build` succeeds.

### DOC02 — TestProject_ShouldRun

`dotnet test` discovers and runs the suite.

### DOC06 — Repository_ShouldExistInGitHub

`R23`

Repository exists, current state is pushed, required files are present, and no secrets are committed.

### DOC07 — README_ShouldContainRunInstructions

`R24`

README explains prerequisites, build, run and test commands.

### DOC08 — README_ShouldExplainTesting

`R24`

README explains how Parts A–D are tested/observed and how the local service is started when included.

### DOC09 — Reflection_ShouldContainRequiredContent

`R25`

`reflection.md` contains required observations and answers.

---

## 11. Part D HTTP verification

Part D is optional and active only if retained in final scope.

Automated HTTP tests use a controllable local/test HTTP boundary rather than uncontrolled external network calls.

### HTTP01 — ControlTower_ShouldReturnRouteData

`PD2 → AC-D1 → B-D01`

Integration / Fact.

Oracle: valid route response maps to expected route data.

### HTTP02 — ControlTower_ShouldReturnWeatherData

`PD3 → AC-D2 → B-D02`

Integration / Fact.

Oracle: valid weather response maps to expected weather data.

### HTTP03 — ControlTower_ShouldReturnRestrictions

`PD9 → AC-D8 → B-D03`

Integration / Fact.

Active only when restrictions remain in final scope.

### HTTP04 — ControlTower_Data_ShouldAffectSimulation

`PD5 → AC-D4 → B-D04`

Integration / Fact.

Oracle: route/weather/restriction data produces the documented final simulation values.

### HTTP05 — ControlTower_NonSuccessResponse_ShouldProduceFailure

`PD6 → AC-D5 → B-D05`

Integration / Fact.

Oracle: documented `ControlTowerException.RequestFailed` behaviour.

### HTTP06 — ControlTower_Timeout_ShouldProduceFailure

`PD7 → AC-D6 → B-D06`

Integration / Fact.

Oracle: documented `ControlTowerException.Timeout` behaviour.

### HTTP07 — ControlTower_InvalidResponse_ShouldProduceFailure

`PD6 → AC-D5 → B-D05`

Integration / Fact.

Oracle: malformed/missing response data produces `ControlTowerException.InvalidResponse`.

### HTTP08 — ControlTower_ShouldRemainAsynchronous

`PD4/PD8 → AC-D3/AC-D7 → B-D07`

Integration + inspection.

Oracle: request flow remains asynchronous and non-blocking.

### HTTP09 — ControlTower_Requests_ShouldBeObservable

`PD10 → AC-D9 → B-D09`

Integration/manual.

Active if HTTP lifecycle logging remains in final scope.

### HTTP10 — ControlTower_SequentialAndConcurrentResults_ShouldMatch

`PD11 → AC-D10 → B-D10`

Integration / Fact.

Oracle: equivalent functional data.

### HTTP11 — ControlTower_ShouldProduceVariableResponseTime

`PD12 → AC-D11 → B-D11`

Integration/manual.

Oracle: server can deliberately vary response time.

Exact duration is not asserted.

### HTTP12 — ControlTower_RouteQuery_ShouldUseDroneName

`PD1/PD2 → AC-D1 → B-D01`

Integration / Fact.

Oracle: `/route?drone=Navn` uses the requested drone name correctly.

### I20 — ControlTower_UsesReusableHttpClient

`PD4`

Inspect that one reusable `HttpClient` is used rather than a new instance per request.

### I21 — LocalControlTower_UsesAsyncRequestHandling

`PD8`

Inspect that the local `HttpListener` uses asynchronous request handling.

### I22 — ControlTower_UsesAsyncHttpApis

`PD4`

Inspect `GetAsync`/equivalent async APIs and asynchronous response handling.

---

## 12. Part D test data

### Route

- valid positive checkpoint count;
- zero checkpoint count;
- invalid negative count.

### Weather

At minimum:

- `clear`;
- `wind`;
- `storm`.

Unknown values are tested only if the final contract rejects them.

### Restrictions

When active:

- no restriction;
- restriction below route maximum;
- restriction equal to route maximum;
- invalid negative restriction.

### HTTP failures

Where supported by the final contract:

- non-success status;
- timeout;
- malformed response;
- missing required response data;
- unavailable service.

---

## 13. Fact / Theory

Use `Fact` for independent meaningful scenarios.

Use `Theory` when the same rule applies to representative datasets.

Good Theory candidates:

- negative numeric validation;
- name validation;
- checkpoint progression.

Do not use Theory solely to reduce line count.

---

## 14. Test naming

Use:

`MethodOrArea_Scenario_ExpectedResult`

Examples:

- `DroneFlight_ValidDrone_ShouldReportCheckpointZero`
- `DroneFlight_Checkpoints_ShouldProgressFromZeroToMax`
- `ThreadRace_WithJoin_ShouldWaitForAllDrones`
- `TaskFlight_FaultedTask_ShouldExposeExpectedException`
- `AsyncFlight_MultipleDrones_ShouldMakeOverlappingProgress`

---

## 15. Oracle rules

Preferred oracles:

- exact checkpoint sequence;
- `FlightEvent` values;
- task state;
- expected exception type/content;
- observable orchestration completion;
- expected mapped HTTP data.

Avoid:

- exact thread scheduling order;
- exact wall-clock duration;
- expected values produced by duplicating production calculations.

Every automated test should answer:

> How would this test fail if the implementation were subtly wrong?

---

## 16. Contract blockers

Before dependent tests are finalized:

- [ ] final Part D JSON schemas;
- [ ] final public Part D method signatures;
- [ ] final restriction response contract;
- [ ] decision on final HTTP lifecycle logging scope.

Core validation, Part B failure, endpoint structure, weather mapping, HTTP exception categories, client reuse, and async local-server handling are already locked.

---

## 17. Final test-design review

Before test files are finalized:

- [ ] Every `R1–R25` has a verification path.
- [ ] Every mandatory acceptance criterion has verification.
- [ ] Every mandatory vertical behaviour has verification.
- [ ] Relevant boundaries are covered.
- [ ] Relevant equivalence partitions are covered.
- [ ] Relevant state transitions are covered.
- [ ] Relevant dependency failures are covered.
- [ ] Concurrency tests prove meaningful overlap/coordination, not only eventual completion.
- [ ] Task failure tests verify task state and exception information.
- [ ] Part D tests are separated from mandatory MVP tests.
- [ ] Every automated test has a clear oracle.
- [ ] Every explicit implementation requirement has an inspection item.
- [ ] Manual demonstrations are explicitly identified.
- [ ] Timing tests do not rely on brittle wall-clock thresholds.
- [ ] Tests do not rely on uncontrolled external HTTP services.
- [ ] Fact/Theory choices are justified.
- [ ] Test names express behaviour, scenario and expected result.
- [ ] Duplicate/redundant tests are removed.
- [ ] No mandatory test depends on an unresolved contract.
- [ ] Test levels match what each test can actually prove.

---

## 18. First TDD target

`VB05 — Report checkpoint 0`

Traceability:

`R3 → AC-CORE-3 → B2 → VB05 → T05`

Scenario:

```text
Given a valid drone with MaxCheckpoints = 0
When the basic flight executes
Then CheckpointReached(0) is observable
```

Observation boundary:

`Action<FlightEvent>`

---

## 19. Status

### Mandatory

- [x] Requirements mapped.
- [x] Acceptance criteria mapped.
- [x] Core/A/B/C behaviours mapped.
- [x] Main oracles defined.
- [x] Implementation inspections identified.
- [x] Manual demonstrations identified.

### Part D

- [x] Route/weather/restriction categories identified.
- [x] HTTP failure/timeout categories identified.
- [x] Async/non-blocking verification identified.
- [x] Sequential/concurrent comparison identified.
- [x] Variable response-time demonstration identified.
- [ ] Final JSON schemas.
- [ ] Final public Part D method signatures.
- [ ] Final restriction contract.
- [ ] Final HTTP lifecycle logging scope.

Test files remain blocked until the final review passes and all mandatory contract blockers are closed.