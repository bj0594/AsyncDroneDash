# Async Drone Dash — Test Plan

## 1. Purpose

When all mandatory verification items pass, every mandatory assignment requirement has a verification path that has passed.

Verification may be automated, inspected, manually demonstrated, or documented.

Part D is optional in the assignment and active only while it remains in the final target scope.

---

## 2. Verification prefixes

| Prefix | Type |
|---|---|
| `T` | Automated test |
| `I` | Implementation inspection |
| `M` | Manual verification |
| `DOC` | Documentation/delivery |
| `HTTP` | Part D HTTP/integration |

---

## 3. Mandatory requirement coverage

| Requirement | Verification |
|---|---|
| `R1` | `DOC01`, `M03` |
| `R2` | `T01`, `I01` |
| `R3` | `T04`, `T05` |
| `R4` | `T07`, `I02` |
| `R5` | `T08`, `T13` |
| `R6` | `T10`, `I03` |
| `R7` | `T11`, `I04` |
| `R8` | `M01`, `I05` |
| `R9` | `M02` |
| `R10` | `T15`, `I06` |
| `R11` | `T16`, `I07` |
| `R12` | `T17`, `T21`, `I08` |
| `R13` | `T18` |
| `R14` | `T19`, `T21` |
| `R15` | `T20`, `I09` |
| `R16` | `T22`, `I10` |
| `R17` | `T23`, `I11` |
| `R18` | `T24`, `I12` |
| `R19` | `T25`, `I13` |
| `R20` | `T26`, `I14`, `I15` |
| `R21` | `DOC05` |
| `R22` | `M03` |
| `R23` | `DOC06` |
| `R24` | `DOC07`, `DOC08` |
| `R25` | `DOC09` |

---

## 4. Assignment edge-case coverage

| Edge case | Verification |
|---|---|
| `E1` Negative MaxCheckpoints | `T02` |
| `E2` Negative DelayMs | `T03` |
| `E3` Missing/blank name | `T06` |
| `E4` Unknown drone | `HTTP08` |
| `E5` Control-tower failure/invalid response/timeout | `HTTP05`, `HTTP06`, `HTTP07` |

---

## 5. Core automated tests

### T01 — DroneModel_ValidConfiguration_ShouldBeAccepted

`R2 → AC-CORE-2 → VB01`

Unit / Fact.

Oracle: valid values are accepted and preserved.

### T02 — DroneModel_NegativeMaxCheckpoints_ShouldBeRejected

`E1 → AC-EDGE-1 → VB02`

Unit / Theory.

Representative negative values.

Oracle: `ArgumentOutOfRangeException`.

### T03 — DroneModel_NegativeDelayMs_ShouldBeRejected

`E2 → AC-EDGE-2 → VB03`

Unit / Theory.

Representative negative values.

Oracle: `ArgumentOutOfRangeException`.

### T04 — DroneFlight_Checkpoints_ShouldProgressFromZeroToMax

`R3 → AC-CORE-3 → VB06`

Unit / Theory.

Representative values: `0`, `1`, `3`.

Oracle: exact sequence `0..MaxCheckpoints`.

### T05 — DroneFlight_ZeroMaxCheckpoints_ShouldReportZeroAndComplete

`R3 → AC-CORE-3 → VB05`

Unit / Fact.

Oracle: checkpoint `0` then completion.

### T06 — DroneFlight_MissingOrBlankName_ShouldBeRejected

`E3 → AC-EDGE-3 → VB04`

Unit / Theory.

Cases: null, empty, whitespace.

Oracle: `ArgumentException`.

### T07 — DroneFlight_ShouldApplyConfiguredDelayBetweenCheckpoints

`R4 → AC-CORE-4 → VB07`

Unit/component + inspection.

Oracle: required delay mechanism is used between checkpoint steps.

No exact elapsed-time assertion.

### T08 — DroneFlight_ShouldReportLifecycle

`R5 → AC-CORE-5 → VB08`

Unit/component / Fact.

Oracle: `Started`, every checkpoint, `Completed`.

---

## 6. Part A

### T10 — ThreadRace_MultipleDrones_ShouldUseSeparateThreadsAndComplete

`R6 → AC-A1 → VB09`

Component / Fact.

Oracle: all participating drones complete. `I03` proves the required per-drone Thread mechanism.

### T11 — ThreadRace_WithJoin_ShouldWaitForAllDrones

`R7 → AC-A2 → VB10`

Component / Fact.

Oracle: overall completion cannot occur before all required drone threads finish.

Use controlled synchronization; no arbitrary timing thresholds.

### T12 — ThreadRace_EachDrone_ShouldReportItsOwnProgress

`R3/R5 → VB06/VB08`

Component / Fact.

Oracle: each participating drone has its own correct checkpoint sequence.

### T13 — ThreadRace_ShouldReportLifecycleForEachDrone

`R5 → AC-CORE-5 → VB08`

Component / Fact.

Oracle: each drone reports start, checkpoints, completion.

Exact cross-thread ordering is not asserted.

### T14 — ThreadRace_MultipleDrones_ShouldEnterRunningPhaseConcurrently

`R6 → AC-A1 → VB09`

Component / Fact.

Oracle: a controlled observer/gate can establish that at least two participating drone Threads enter the running phase before the race is allowed to continue. The test must use synchronization rather than elapsed-time thresholds.

### I01 — DroneModel_ExposesRequiredProperties

`R2`

### I02 — RequiredDelayMechanism_IsUsed

`R4`

Inspect the required delay implementation. Test correctness must not depend on elapsed wall-clock time.

### I03 — PartA_UsesOneThreadPerDrone

`R6`

### I04 — PartA_UsesJoin

`R7`

### I05 — PartA_ContainsNoJoinDemonstration

`R8`

### M01 — ThreadRace_WithoutJoin_ShouldDemonstratePrematureContinuation

`R8`

### M02 — ThreadRace_ShouldDemonstrateInterleavedOutput

`R9`

### M03 — Menu_ShouldExposePartsAThroughD

`R22`

---

## 7. Part B

### T15 — TaskFlight_SuccessfulDrone_ShouldCompleteTask

`R10 → AC-B1 → VB13`

Component / Fact.

Oracle: task completes successfully.

### T16 — TaskFlight_ShouldUseIndependentCompletionPerDrone

`R11 → AC-B2 → VB14`

Component / Fact.

Oracle: each drone has an independent completion outcome.

### T17 — TaskFlight_WhenAll_ShouldWaitForAllDrones

`R12 → AC-B3 → VB15`

Component / Fact.

Oracle: combined completion follows all participating tasks.

### T18 — TaskFlight_SimulatedFailure_ShouldFaultOperation

`R13 → AC-B4 → VB16`

Component / Fact.

Oracle: `InvalidOperationException("Simulated drone failure.")`.

The test selects the designated failure scenario without adding a permanent failure property to `DroneModel`.

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
- aggregate contains the expected `InvalidOperationException`.

### T21 — TaskFlight_WhenAll_WithFailure_ShouldRepresentCombinedFailure

`R12/R14 → VB15/VB17`

Component / Fact.

One task faults while another has not yet completed.

Oracle:

- failing task faults;
- remaining task reaches a terminal state;
- combined operation does not falsely report success.

### I06 — PartB_UsesTask

`R10`

### I07 — PartB_UsesOneTCSPerDrone

`R11`

### I08 — PartB_UsesTaskWhenAll

`R12`

### I09 — PartB_ObservesTaskException

`R15`

---

## 8. Part C

### T22 — AsyncFlight_ValidDrone_ShouldComplete

`R16 → AC-C1 → VB19`

Component / Fact.

Oracle: async flight completes successfully.

### T23 — AsyncFlight_ShouldUseAsyncDelay

`R17 → AC-C2 → VB20`

Component + inspection / Fact.

Oracle: `await Task.Delay` is used; no elapsed-time oracle.

### T24 — AsyncFlight_MultipleDrones_ShouldMakeOverlappingProgress

`R18 → AC-C3 → VB21`

Component / Fact.

Oracle: evidence must establish meaningful overlapping progress; eventual completion alone is insufficient.

The exact scheduling order is not asserted.

### T25 — AsyncFlight_ShouldAwaitTaskWhenAll

`R19 → AC-C4 → VB22`

Component / Fact.

Oracle: overall completion follows all required flights.

### T26 — AsyncFlight_Failure_ShouldBeHandledByOrchestration

`R20 → AC-C5 → VB23`

Component / Fact.

Oracle: controlled failure reaches orchestration `try/catch`.

### I10 — PartC_UsesAsyncFlightMethod

`R16`

### I11 — PartC_UsesAwaitTaskDelay

`R17`

### I12 — PartC_AllowsConcurrentProgress

`R18`

### I13 — PartC_UsesAwaitTaskWhenAll

`R19`

### I14 — PartC_UsesTryCatch

`R20`

### I15 — PartC_ContainsNoSynchronousBlocking

Inspect absence of `.Wait()` and `.Result` in the async execution path.

### DOC05 — Reflection_ShouldComparePartBAndPartC

`R21 → AC-C6 → VB24`

Review reflection against actual implementations.

---

## 9. Delivery

### DOC01 — Project_ShouldBuild

`R1`

`dotnet build` succeeds.

### DOC02 — TestProject_ShouldRun

`dotnet test` discovers and runs the suite.

### DOC06 — Repository_ShouldExistInGitHub

`R23`

Repository exists and current source is pushed.

### DOC07 — README_ShouldContainRunInstructions

`R24`

Root README contains prerequisites, build/run/test instructions and local HTTP startup/troubleshooting.

### DOC08 — README_ShouldExplainPartTesting

`R24`

README explains how Parts A–D are tested/observed and how the local service is started.

### DOC09 — Reflection_ShouldContainRequiredContent

`R25`

`reflection.md` answers the five assignment questions and, when Part D is implemented, records the sequential-versus-concurrent HTTP observation.

---

## 10. Part D HTTP verification

Part D is optional in the assignment and active because it is the current project target.

Automated HTTP tests use a controllable local/test HTTP boundary, not an uncontrolled external service.

### HTTP00 — ControlTower_LocalService_ShouldExposeRequiredEndpoints

`PD1 → AC-D0 → VB-D00`

Integration / Fact.

Oracle: the local service starts and exposes `/route`, `/weather`, and `/restrictions` using the finalized response contracts.

### HTTP01 — ControlTower_ShouldReturnRouteData

`PD2 → AC-D1 → VB-D01`

Integration / Fact.

Oracle: `{ "maxCheckpoints": n }` maps correctly.

### HTTP02 — ControlTower_ShouldReturnWeatherData

`PD3 → AC-D2 → VB-D02`

Integration / Fact.

Oracle: `clear`, `wind`, and `storm` map correctly.

### HTTP03 — ControlTower_ShouldReturnRestrictions

`PD9 → AC-D8 → VB-D03`

Integration / Fact.

Oracle: integer or null maps correctly.

### HTTP04 — ControlTower_Data_ShouldAffectSimulation

`PD5 → AC-D4 → VB-D04`

Integration / Fact.

Oracle: documented final `MaxCheckpoints` and `DelayMs` values.

### HTTP05 — ControlTower_NonSuccessResponse_ShouldProduceRequestFailed

`PD6 → AC-D5 → VB-D05`

Integration / Fact.

Oracle: non-success/connection failure maps to `RequestFailed`.

### HTTP06 — ControlTower_Timeout_ShouldProduceTimeout

`PD7 → AC-D6 → VB-D06`

Integration / Fact.

Oracle: timeout maps to `Timeout`.

### HTTP07 — ControlTower_InvalidResponse_ShouldProduceInvalidResponse

`PD6 → AC-D5 → VB-D05`

Integration / Fact.

Oracle: malformed JSON, missing required property, or invalid value maps to `InvalidResponse`.

### HTTP08 — ControlTower_UnknownDrone_ShouldProduceNotFound

`E4 → AC-EDGE-4 → VB-D12`

Integration / Fact.

Oracle: `/route?drone=Unknown` maps to `NotFound`.

### HTTP09 — ControlTower_ShouldRemainAsynchronous

`PD4/PD8 → AC-D3/AC-D7 → VB-D07`

Integration + inspection.

Oracle: client uses asynchronous APIs and server uses asynchronous request handling without synchronous blocking.

### HTTP10 — ControlTower_ShouldLogLifecycle

`PD10 → AC-D9 → VB-D09`

Integration/manual.

Oracle: each request has observable start and completion/failure logging.

### HTTP11 — ControlTower_SequentialAndConcurrentResults_ShouldMatch

`PD11 → AC-D10 → VB-D10`

Integration / Fact.

Oracle: equivalent functional data in both modes.

Manual observation additionally compares relative execution time/overlap; elapsed time is evidence for comparison, not the functional correctness oracle.

### HTTP12 — ControlTower_ShouldProduceVariableResponseTime

`PD12 → AC-D11 → VB-D11`

Integration/manual.

Oracle: local service can deliberately vary response time. Randomness is manual/demo behaviour; automated tests use controlled delays.

### I16 — ControlTower_UsesReusableHttpClient

`PD4`

### I17 — ControlTower_UsesAsyncHttpApis

`PD4`

### I18 — LocalControlTower_UsesAsyncRequestHandling

`PD1/PD8`

### I19 — PartD_UsesFinalJsonAndErrorContracts

`PD1–PD12`

Inspect finalized JSON shapes, `ControlTowerErrorKind`, endpoint mapping, and failure translation.

---

## 11. Part D manual comparison

### M04 — ControlTower_SequentialAndConcurrentModes_ShouldBeCompared

`PD11 → AC-D10`

Observe:

- both modes return equivalent functional data;
- concurrent mode shows overlapping request activity in the logs;
- the relative total execution time is recorded as an observation, not a pass/fail timing threshold.

---

## 12. Test data

### Drone validation

- `MaxCheckpoints = -1` → reject;
- `MaxCheckpoints = 0` → valid;
- `MaxCheckpoints = 1` → `0,1`;
- `MaxCheckpoints = 3` → `0,1,2,3`;
- `DelayMs = -1` → reject;
- `DelayMs = 0` → valid;
- valid positive delay → valid;
- null/empty/whitespace name → reject.

### Route

- valid positive count;
- zero;
- negative invalid data;
- unknown drone.

### Weather

- `clear`;
- `wind`;
- `storm`;
- unsupported condition.

### Restrictions

- `null`;
- value below route maximum;
- value equal to route maximum;
- invalid negative value.

### HTTP failures

- `404`;
- non-success response;
- timeout;
- malformed JSON;
- missing required property;
- invalid property value.

---

## 13. Oracles

Preferred automated-test oracles:

- exact checkpoint sequence;
- captured `FlightEvent` values;
- task state;
- expected exception/error kind;
- observable orchestration completion;
- expected mapped HTTP data.

Avoid:

- exact thread scheduling order;
- exact wall-clock duration;
- expected values calculated by duplicating production logic.

---

## 14. Fact / Theory

Use Fact for independent meaningful scenarios.

Use Theory where the same rule applies to representative datasets.

Good Theory candidates:

- negative numeric validation;
- name validation;
- checkpoint progression.

Do not use Theory merely to reduce test-file length.

---

## 15. Test naming

Use:

`MethodOrArea_Scenario_ExpectedResult`

Examples:

- `DroneFlight_ValidDrone_ShouldReportCheckpointZero`
- `DroneFlight_Checkpoints_ShouldProgressFromZeroToMax`
- `ThreadRace_WithJoin_ShouldWaitForAllDrones`
- `ThreadRace_MultipleDrones_ShouldEnterRunningPhaseConcurrently`
- `TaskFlight_FaultedTask_ShouldExposeExpectedException`
- `AsyncFlight_MultipleDrones_ShouldMakeOverlappingProgress`
- `ControlTower_UnknownDrone_ShouldProduceNotFound`

---

## 16. Contract and test-design review

- [ ] Every `R1–R25` has a passing verification path.
- [ ] Every active `E` edge case has verification.
- [ ] Every active `PD1–PD12` requirement has verification.
- [ ] Every mandatory acceptance criterion has verification.
- [ ] Every mandatory vertical behaviour has verification.
- [ ] Relevant boundaries and partitions are covered.
- [ ] Relevant state transitions are covered.
- [ ] Relevant dependency failures are covered.
- [ ] Concurrency verification proves coordination/overlap rather than only eventual completion.
- [ ] Task failure verifies both faulted state and exception information.
- [ ] Part D tests use controllable dependencies.
- [ ] No timing test depends on an exact elapsed duration.
- [ ] Every automated test has a clear oracle.
- [ ] Implementation-specific requirements have inspection items.
- [ ] Manual demonstrations are identified.
- [ ] Test levels are appropriate.
- [ ] Fact/Theory choices are justified.
- [ ] Test names express behaviour, scenario and expected result.
- [ ] Duplicate/redundant tests have been removed.
- [ ] No mandatory test depends on an unresolved contract.

---

## 17. Definition of Ready for test files

The test files may be created when:

1. all contracts in `01–05` are closed;
2. every mandatory requirement has a verification path;
3. every acceptance criterion has a verification path;
4. every vertical behaviour has a test, inspection, manual, or documentation path;
5. every automated test has a clear oracle and test level;
6. concurrency scenarios have deterministic observation strategies;
7. Part D uses controllable HTTP dependencies;
8. no planned test depends on an unspecified contract.

This document is the verification map. The actual xUnit code will still be developed iteratively through Red → Green → Refactor.

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

The mandatory requirements, selected Part D target, data contracts, error categories, behaviours, test levels, oracles, and verification routes are now defined.

No mandatory contract remains intentionally open.
