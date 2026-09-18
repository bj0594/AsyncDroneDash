# Async Drone Dash — Test Plan

## 1. Purpose

The test plan defines how requirements and behaviours will be verified.

The target is:

> When all mandatory verification items pass, every mandatory assignment requirement has a passing verification path.

Verification can be automated, inspected, manually demonstrated, or documented.

Part D is optional in the assignment and active because it is currently selected as the final project target.

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
| `R5` | `T08`, `T12`, `T13` |
| `R6` | `T10`, `T14`, `I03` |
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
| `E1` Negative `MaxCheckpoints` | `T02` |
| `E2` Negative `DelayMs` | `T03` |
| `E3` Missing/blank name | `T06` |
| `E4` Unknown drone | `HTTP08` |
| `E5` Control-tower failure/invalid response/timeout | `HTTP05`, `HTTP06`, `HTTP07` |

---

## 5. Core automated tests

### T01 — DroneFlight_ValidConfiguration_ShouldBeAccepted

`R2 → AC-CORE-2 → VB01`

Unit / Fact.

Oracle:

A valid configuration reaches the flight boundary successfully and preserves the supplied values.

---

### T02 — DroneFlight_NegativeMaxCheckpoints_ShouldBeRejected

`E1 → AC-EDGE-1 → VB02`

Unit / Theory.

Representative negative values.

Oracle:

`ArgumentOutOfRangeException`.

---

### T03 — DroneFlight_NegativeDelayMs_ShouldBeRejected

`E2 → AC-EDGE-2 → VB03`

Unit / Theory.

Representative negative values.

Oracle:

`ArgumentOutOfRangeException`.

---

### T04 — DroneFlight_Checkpoints_ShouldProgressFromZeroToMax

`R3 → AC-CORE-3 → VB06`

Unit / Theory.

Representative values:

- `0`;
- `1`;
- `3`.

Oracle:

Exact checkpoint sequence `0..MaxCheckpoints`.

---

### T05 — DroneFlight_ZeroMaxCheckpoints_ShouldReportZeroAndComplete

`R3 → AC-CORE-3 → VB05`

Unit / Fact.

Oracle:

`CheckpointReached(0)` is observed and is followed by completion.

---

### T06 — DroneFlight_MissingOrBlankName_ShouldBeRejected

`E3 → AC-EDGE-3 → VB04`

Unit / Theory.

Cases:

- `null`;
- empty string;
- whitespace-only string.

Oracle:

`ArgumentException`.

---

### T07 — DroneFlight_ShouldApplyConfiguredDelayBetweenCheckpoints

`R4 → AC-CORE-4 → VB07`

Component + implementation inspection / Fact.

Oracle:

The required delay mechanism occurs between checkpoint steps.

Exact elapsed time is not a correctness oracle.

---

### T08 — DroneFlight_ShouldReportLifecycle

`R5 → AC-CORE-5 → VB08`

Unit/component / Fact.

Oracle:

The event stream contains:

`Started → all checkpoints → Completed`.

---

## 6. Part A

### T10 — ThreadRace_MultipleDrones_ShouldUseSeparateThreadsAndComplete

`R6 → AC-A1 → VB09`

Component / Fact.

Oracle:

All participating drones complete.

`I03` separately proves the required per-drone `Thread` mechanism.

---

### T11 — ThreadRace_WithJoin_ShouldWaitForAllDrones

`R7 → AC-A2 → VB10`

Component / Fact.

Oracle:

Overall completion cannot occur before all required drone threads finish.

Use controlled synchronization, not arbitrary timing thresholds.

---

### T12 — ThreadRace_EachDrone_ShouldReportItsOwnProgress

`R5 → AC-CORE-5 → VB13`

Component / Fact.

Use at least two drones with different checkpoint counts.

Oracle:

Each drone has its own correct lifecycle and checkpoint sequence.

---

### T13 — ThreadRace_ShouldReportLifecycleForEachDrone

`R5 → AC-CORE-5 → VB08`

Component / Fact.

Oracle:

Every participating drone reports start, checkpoints and completion.

Exact cross-thread order is not asserted.

---

### T14 — ThreadRace_MultipleDrones_ShouldEnterRunningPhaseConcurrently

`R6 → AC-A1 → VB14`

Component / Fact.

Oracle:

A controlled observer/gate establishes that at least two participating Threads have entered the running phase before the test permits the race to continue.

The test must use synchronization rather than elapsed-time thresholds.

---

### I01 — DroneModel_ExposesRequiredProperties

`R2`

Verify:

```text
Name
MaxCheckpoints
DelayMs
```

---

### I02 — RequiredDelayMechanism_IsUsed

`R4`

Inspect the required checkpoint-delay mechanism.

Do not use elapsed wall-clock time as the implementation oracle.

---

### I03 — PartA_UsesOneThreadPerDrone

`R6`

Verify one `Thread` is created/used per participating drone.

---

### I04 — PartA_UsesJoin

`R7`

Verify normal Part A orchestration uses `Join` for all participating Threads.

---

### I05 — PartA_ContainsNoJoinDemonstration

`R8`

Verify a deliberate no-Join path exists.

---

### M01 — ThreadRace_WithoutJoin_ShouldDemonstratePrematureContinuation

`R8 → AC-A3 → VB11`

Observe main-thread continuation before all drone threads have completed.

---

### M02 — ThreadRace_ShouldDemonstrateInterleavedOutput

`R9 → AC-A4 → VB12`

Observe interleaving or reordering of concurrent console output.

---

### M03 — Menu_ShouldExposePartsAThroughD

`R22`

Observe:

- menu appears;
- Parts A–D are represented;
- each part can be entered;
- normal return/exit behaviour works.

---

## 7. Part B

### T15 — TaskFlight_SuccessfulDrone_ShouldCompleteTask

`R10 → AC-B1 → VB15`

Component / Fact.

Oracle:

Successful drone operation reaches a completed Task state.

---

### T16 — TaskFlight_ShouldUseIndependentCompletionPerDrone

`R11 → AC-B2 → VB16`

Component / Fact.

Oracle:

Each participating drone has an independent completion outcome.

---

### T17 — TaskFlight_WhenAll_ShouldWaitForAllDrones

`R12 → AC-B3 → VB17`

Component / Fact.

Oracle:

Combined completion follows all participating tasks.

---

### T18 — TaskFlight_SimulatedFailure_ShouldFaultAfterCheckpointOne

`R13 → AC-B4 → VB18`

Component / Fact.

Scenario:

One selected failure drone reaches checkpoint `1`.

Oracle:

It then produces:

`InvalidOperationException("Simulated drone failure.")`

and its operation becomes faulted.

---

### T19 — TaskFlight_Failure_ShouldReachOrchestration

`R14 → AC-B5 → VB19`

Component / Fact.

Oracle:

The failure reaches orchestration and is not silently swallowed.

---

### T20 — TaskFlight_FaultedTask_ShouldExposeExpectedException

`R15 → AC-B6 → VB20`

Component / Fact.

Oracle:

- Task is `Faulted`;
- `Task.Exception` is non-null;
- its aggregate contains the expected `InvalidOperationException`.

---

### T21 — TaskFlight_WhenAll_WithFailure_ShouldRepresentCombinedFailure

`R12/R14 → AC-B3/AC-B5 → VB21`

Component / Fact.

Scenario:

One task faults while another has not yet completed.

Oracle:

- failing task faults;
- remaining task reaches a terminal state;
- combined operation does not falsely report success.

---

### I06 — PartB_UsesTask

`R10`

Verify Task-based representation.

### I07 — PartB_UsesOneTCSPerDrone

`R11`

Verify one independent TCS per participating drone.

### I08 — PartB_UsesTaskWhenAll

`R12`

Verify `Task.WhenAll`.

### I09 — PartB_ObservesTaskException

`R15`

Verify explicit `Task.Exception` observation.

---

## 8. Part C

### T22 — AsyncFlight_ValidDrone_ShouldComplete

`R16 → AC-C1 → VB22`

Component / Fact.

Oracle:

A valid async flight completes successfully.

### T23 — AsyncFlight_ShouldUseAsyncDelay

`R17 → AC-C2 → VB23`

Component + inspection / Fact.

Oracle:

Checkpoint delay uses `await Task.Delay`.

No elapsed-time oracle.

### T24 — AsyncFlight_MultipleDrones_ShouldMakeOverlappingProgress

`R18 → AC-C3 → VB24`

Component / Fact.

Oracle:

At least two async flights make meaningful overlapping progress.

The test must not require a particular scheduling order.

### T25 — AsyncFlight_ShouldAwaitTaskWhenAll

`R19 → AC-C4 → VB25`

Component / Fact.

Oracle:

Overall completion follows all required async flights.

### T26 — AsyncFlight_Failure_ShouldBeHandledByOrchestration

`R20 → AC-C5 → VB26`

Component / Fact.

Oracle:

Controlled failure reaches the orchestration `try/catch`.

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

`R20`

Verify `.Wait()` and `.Result` are not used in the async execution path.

### DOC05 — Reflection_ShouldComparePartBAndPartC

`R21 → AC-C6 → VB27`

Verify the final reflection compares:

- boilerplate;
- complexity;
- readability;
- maintainability.

---

## 9. Delivery

### DOC01 — Project_ShouldBuild

`R1`

`dotnet build` succeeds.

### DOC02 — TestProject_ShouldRun

`dotnet test` discovers and runs the suite.

### DOC06 — Repository_ShouldExistInGitHub

`R23`

Verify repository existence, current source push, required files, and absence of committed secrets.

### DOC07 — README_ShouldContainRunInstructions

`R24`

Verify prerequisites, build/run/test instructions, and local HTTP startup/troubleshooting.

### DOC08 — README_ShouldExplainPartTesting

`R24`

Verify README explains how Parts A–D are tested/observed.

### DOC09 — Reflection_ShouldContainRequiredContent

`R25`

Verify `reflection.md` answers the five assignment reflection questions.

When Part D is implemented, it also records the sequential-versus-concurrent HTTP observation.

---

## 10. Part D HTTP verification

Part D is optional and active because it is currently the final target.

Automated HTTP tests use a controllable local/test HTTP boundary.

### HTTP00 — ControlTower_LocalService_ShouldExposeRequiredEndpoints

`PD1 → AC-D0 → VB-D00`

Integration / Fact.

Oracle:

The local service starts and exposes:

```text
/route?drone=Navn
/weather
/restrictions
```

using the finalized contracts.

### HTTP01 — ControlTower_ShouldReturnRouteData

`PD2 → AC-D1 → VB-D01`

Oracle:

Valid route response maps to `RouteData`.

### HTTP02 — ControlTower_ShouldReturnWeatherData

`PD3 → AC-D2 → VB-D02`

Oracle:

Valid weather response maps to `WeatherData`.

### HTTP03 — ControlTower_ShouldReturnRestrictions

`PD9 → AC-D8 → VB-D03`

Oracle:

Restriction response maps correctly, including the no-restriction case.

### HTTP04 — ControlTower_Data_ShouldAffectSimulation

`PD5 → AC-D4 → VB-D04`

Oracle:

Route, weather, and restriction data produce the documented final `MaxCheckpoints` and `DelayMs`.

### HTTP05 — ControlTower_NonSuccessResponse_ShouldProduceRequestFailed

`PD6/E5 → AC-D5/AC-EDGE-5 → VB-D05/VB-E05`

Oracle:

Non-success/connection failure maps to `ControlTowerErrorKind.RequestFailed`.

### HTTP06 — ControlTower_Timeout_ShouldProduceTimeout

`PD7/E5 → AC-D6/AC-EDGE-5 → VB-D06/VB-E05`

Oracle:

Timeout maps to `ControlTowerErrorKind.Timeout`.

### HTTP07 — ControlTower_InvalidResponse_ShouldProduceInvalidResponse

`PD6/E5 → AC-D5/AC-EDGE-5 → VB-D05/VB-E05`

Oracle:

Malformed JSON, missing required data, or invalid response values map to `InvalidResponse`.

### HTTP08 — ControlTower_UnknownDrone_ShouldProduceNotFound

`E4 → AC-EDGE-4 → VB-E04`

Oracle:

Unknown route name results in `NotFound`.

### HTTP09 — ControlTower_ShouldRemainAsynchronous

`PD4/PD8 → AC-D3/AC-D7 → VB-D07/VB-D08`

Integration + inspection.

Oracle:

Client and server use asynchronous APIs without synchronous blocking.

### HTTP10 — ControlTower_ShouldLogLifecycle

`PD10 → AC-D9 → VB-D09`

Oracle:

Each HTTP call exposes start and completion/failure logging.

### HTTP11 — ControlTower_SequentialAndConcurrentResults_ShouldMatch

`PD11 → AC-D10 → VB-D10`

Oracle:

Sequential and concurrent modes produce equivalent functional data.

### HTTP12 — ControlTower_ShouldProduceVariableResponseTime

`PD12 → AC-D11 → VB-D11`

Oracle:

Local server can deliberately vary response delay.

Automated tests use controlled delays; manual demonstration may use varied/random delays.

---

## 11. Part D inspection and manual comparison

### I16 — ControlTower_UsesReusableHttpClient

`PD4`

Verify one reusable `HttpClient` is used.

### I17 — ControlTower_UsesAsyncHttpApis

`PD4`

Verify asynchronous request and response APIs.

### I18 — LocalControlTower_UsesAsyncRequestHandling

`PD1/PD8`

Verify asynchronous `HttpListener` request handling.

### I19 — PartD_UsesFinalJsonAndErrorContracts

`PD1–PD12`

Verify finalized:

- endpoint shapes;
- JSON contracts;
- weather mapping;
- restriction mapping;
- error categories.

### M04 — ControlTower_SequentialAndConcurrentModes_ShouldBeCompared

`PD11 → AC-D10 → VB-D10`

Observe:

- equivalent functional results;
- overlapping request activity;
- relative execution behaviour.

Elapsed time is an observation, not a functional pass/fail threshold.

### M05 — ControlTower_VariableResponseDelay_ShouldBeObservable

`PD12 → AC-D11 → VB-D11`

Observe varied local response delays.

---

## 12. Test data

### Drone validation

- `MaxCheckpoints = -1` → reject;
- `MaxCheckpoints = 0` → valid;
- `MaxCheckpoints = 1`;
- `MaxCheckpoints = 3`;
- `DelayMs = -1` → reject;
- `DelayMs = 0` → valid;
- positive delay;
- null/empty/whitespace name → reject.

### Part B

- at least two drones;
- one selected failure target;
- failure immediately after checkpoint `1`;
- failure scenario requires `MaxCheckpoints >= 1`.

### Route

- positive checkpoint count;
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
- non-success status;
- timeout;
- malformed JSON;
- missing required property;
- invalid property value.

---

## 13. Oracles

Preferred automated-test oracles:

- exact checkpoint sequence;
- captured `FlightEvent` values;
- Task state;
- expected exception/error kind;
- expected mapped HTTP data;
- observable orchestration completion.

Avoid:

- exact thread scheduling order;
- exact wall-clock duration;
- expected values produced by duplicating production logic.

Every automated test must answer:

> How would this test fail if the implementation were subtly wrong?

---

## 14. Fact / Theory

Use `Fact` for independent meaningful scenarios.

Use `Theory` when one behaviour genuinely applies to representative datasets.

Good Theory candidates:

- negative numeric validation;
- name validation;
- checkpoint progression.

Do not use Theory solely to reduce line count.

---

## 15. Test naming

Use:

`MethodOrArea_Scenario_ExpectedResult`

Examples:

```text
DroneFlight_ValidConfiguration_ShouldBeAccepted
DroneFlight_NegativeMaxCheckpoints_ShouldBeRejected
DroneFlight_Checkpoints_ShouldProgressFromZeroToMax
ThreadRace_WithJoin_ShouldWaitForAllDrones
ThreadRace_MultipleDrones_ShouldEnterRunningPhaseConcurrently
TaskFlight_FaultedTask_ShouldExposeExpectedException
AsyncFlight_MultipleDrones_ShouldMakeOverlappingProgress
ControlTower_UnknownDrone_ShouldProduceNotFound
```

---

## 16. Contract and test-design review

- [ ] Every `R1–R25` has a verification path.
- [ ] Every active `E1–E5` edge case has verification.
- [ ] Every active `PD1–PD12` requirement has verification.
- [ ] Every mandatory acceptance criterion has verification.
- [ ] Every mandatory vertical behaviour has verification.
- [ ] Relevant boundaries and equivalence partitions are covered.
- [ ] Relevant state transitions are covered.
- [ ] Relevant dependency failures are covered.
- [ ] Concurrency tests prove overlap/coordination rather than eventual completion only.
- [ ] Task failure tests verify faulted state and exception information.
- [ ] Part D automated tests use controllable dependencies.
- [ ] Timing is never an exact correctness oracle.
- [ ] Every automated test has a clear oracle.
- [ ] Required implementation mechanisms have inspection items.
- [ ] Manual-only assignment demonstrations are explicitly identified.
- [ ] Test levels are appropriate.
- [ ] Fact/Theory choices are justified.
- [ ] Test names express behaviour, scenario and expected result.
- [ ] Duplicate/redundant tests are removed.
- [ ] No mandatory test depends on an unresolved contract.

---

## 17. Definition of Ready

Test files may be created only when:

1. all mandatory contracts in `01–05` are closed;
2. every mandatory requirement has a verification path;
3. every acceptance criterion has a verification path;
4. every mandatory vertical behaviour has verification;
5. every automated test has a clear oracle and test level;
6. concurrency tests have deterministic observation strategies;
7. Part D automated tests use controllable HTTP dependencies;
8. no planned test depends on an unspecified contract;
9. `dotnet build` succeeds;
10. `dotnet test` succeeds before custom tests are added.

The TestPlan is the verification map. The actual xUnit code is still developed iteratively through Red → Green → Refactor.

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

Planning/test design is ready for the final Definition of Ready check.

The next implementation-stage actions are:

- run `dotnet build`;
- run `dotnet test`;
- perform the local `HttpListener` spike;
- resolve any runtime issue discovered by that spike.

No production implementation or full test suite is to be written before those checks pass.