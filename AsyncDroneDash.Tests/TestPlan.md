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
| `R1` | `DOC01`, `DOC02`, `M03` |
| `R2` | `T01`, `I01` |
| `R3` | `T04`, `T05` |
| `R4` | `T07`, `I02` |
| `R5` | `T08`, `T12` |
| `R6` | `T10`, `T14`, `I03` |
| `R7` | `T11`, `I04` |
| `R8` | `M01`, `I05` |
| `R9` | `M02` |
| `R10` | `T15`, `I06` |
| `R11` | `I07` |
| `R12` | `T17`, `T21`, `I08` |
| `R13` | `T18` |
| `R14` | `T19`, `T21` |
| `R15` | `T20`, `I09` |
| `R16` | `T22`, `I10` |
| `R17` | `I11` |
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

A valid configuration is accepted without validation failure.

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

- `1`;
- `3`;
- `5`.

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

### T07 — DroneFlight_ZeroDelay_ShouldCompleteSuccessfully

`R4 → AC-CORE-4 → VB07`

Unit / Fact.

Scenario:

A valid drone uses `DelayMs = 0`.

Oracle:

The flight completes successfully and reports the expected lifecycle/checkpoint events.

This test covers the valid zero-delay boundary. `I02` separately verifies the required delay mechanism.

---

### T08 — DroneFlight_ShouldReportLifecycle

`R5 → AC-CORE-5 → VB08`

Unit / Fact.

Oracle:

The event stream contains:

`Started → all checkpoints → Completed`.

Each event is associated with the drone that produced it through `FlightEvent.DroneName`.

---

## 6. Part A

### T10 — ThreadRace_MultipleDrones_ShouldComplete

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

Use at least two drones with different checkpoint counts and different names.

Oracle:

Captured events can be grouped by `FlightEvent.DroneName`. Each drone has its own correct lifecycle and checkpoint sequence, with checkpoints in ascending order from `0` to its own `MaxCheckpoints`.

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

    Name
    MaxCheckpoints
    DelayMs

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

`R5/R9 → AC-CORE-5/AC-A4 → VB08/VB12`

Observe that each drone reports start, each checkpoint, and completion, and that messages from concurrent drones can interleave or appear in different orders.

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

A `Faulted` `FlightEvent` identifies the selected drone through `FlightEvent.DroneName` and carries:

`InvalidOperationException("Simulated drone failure.")`

`T20` separately verifies the resulting Task fault state and `Task.Exception`.

---

### T19 — TaskFlight_Failure_ShouldReachOrchestration

`R14 → AC-B5 → VB19`

Component / Fact.

Oracle:

The simulated failure reaches the orchestration boundary and is not silently swallowed.

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

One selected drone faults after checkpoint `1` while another drone has not yet completed.

Oracle:

- the failing drone is identifiable through its `FlightEvent.DroneName`;
- the failing operation faults;
- the other drone produces `Completed`;
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

---

### T24 — AsyncFlight_MultipleDrones_ShouldMakeOverlappingProgress

`R18 → AC-C3 → VB24`

Component / Fact.

Oracle:

At least two async flights make meaningful overlapping progress.

Captured events include `FlightEvent.DroneName`, allowing the overlapping progress of individual drones to be distinguished.

The test must not require a particular scheduling order.

---

### T25 — AsyncFlight_ShouldAwaitTaskWhenAll

`R19 → AC-C4 → VB25`

Component / Fact.

Oracle:

Overall completion follows all required async flights.

---

### T26 — AsyncFlight_Failure_ShouldReachOrchestrationBoundary

`R20 → AC-C5 → VB26`

Component / Fact.

Scenario:

The selected failure drone reports checkpoint `1` and then produces:

`InvalidOperationException("Simulated drone failure.")`

Oracle:

The async flight failure propagates to the Part C orchestration boundary and is observable there. The outer orchestration/application boundary handles the failure through the documented `try/catch` path; the failure is not silently converted into a successful result.

---

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

Verify the documented Part C orchestration boundary contains the required `try/catch`, reports the `Faulted` `FlightEvent`, and rethrows the original failure.

### I15 — PartC_ContainsNoSynchronousBlocking

`R20`

Verify `.Wait()`, `.Result`, and `.GetAwaiter().GetResult()` are not used anywhere in the Part C call path, including the application entry/menu boundary.

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

`R1`

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

Smoke / integration / manual verification.

Oracle:

The actual local `HttpListener` service starts successfully on the target Windows environment and exposes:

    /route?drone=Navn

    /weather

    /restrictions

using the finalized contracts, and can be stopped/disposed cleanly.

This verification is deliberately separate from the deterministic client-side HTTP tests because the actual listener depends on the target environment, port availability, and Windows HTTP configuration.

### HTTP01 — ControlTower_ShouldReturnRouteData

`PD2 → AC-D1 → VB-D01`

HTTP boundary/client / Fact.

Oracle:

Known route fixtures map to the exact documented values:

- `Alpha` → `MaxCheckpoints == 3`;
- `Beta` → `MaxCheckpoints == 5`;
- `Gamma` → `MaxCheckpoints == 2`.

The response maps to `RouteData` using the deterministic drone-name mapping defined in `03-domain-and-rules.md`. Unknown drone names are verified separately by `HTTP08`.

### HTTP02 — ControlTower_ShouldReturnWeatherData

`PD3 → AC-D2 → VB-D02`

HTTP boundary/client / Fact.

Oracle:

Valid weather response maps to `WeatherData`.

### HTTP03 — ControlTower_ShouldReturnRestrictions

`PD9 → AC-D8 → VB-D03`

HTTP boundary/client / Fact.

Oracle:

Restriction response maps correctly, including the no-restriction case.

### HTTP04 — ControlTower_Data_ShouldProduceFinalSimulationConfiguration

`PD5 → AC-D4 → VB-D04`

HTTP boundary/client / Fact.

Oracle:

For a supplied drone and successful control-tower responses, the orchestration returns the documented final simulation configuration:

- `Name` remains the original drone name;
- `MaxCheckpoints` equals the route value or the lower applicable restriction;
- `DelayMs` equals the original delay plus the documented weather adjustment.

The test asserts the returned final configuration directly and does not duplicate the production mapping logic.

### HTTP05 — ControlTower_NonSuccessResponse_ShouldProduceRequestFailed

`PD6/E5 → AC-D5/AC-EDGE-5 → VB-D05/VB-E05`

HTTP boundary/client / Fact.

Oracle:

Non-success HTTP responses other than `404`, or connection-level failures, map to `ControlTowerErrorKind.RequestFailed`.

### HTTP06 — ControlTower_Timeout_ShouldProduceTimeout

`PD7/E5 → AC-D6/AC-EDGE-5 → VB-D06/VB-E05`

HTTP boundary/client / Fact.

Oracle:

A request that exceeds the configured client timeout maps to `ControlTowerErrorKind.Timeout`.

The automated test uses a controlled test HTTP handler/delay rather than waiting for the production timeout duration.

### HTTP07 — ControlTower_InvalidResponse_ShouldProduceInvalidResponse

`PD6/E5 → AC-D5/AC-EDGE-5 → VB-D05/VB-E05`

HTTP boundary/client / Fact.

The automated coverage is split by response contract: weather cases cover malformed/missing/unsupported weather data, while route/restriction cases cover missing or negative `maxCheckpoints`.

Oracle:

Malformed JSON, missing required data, or invalid response values map to `InvalidResponse`.

### HTTP08 — ControlTower_UnknownDrone_ShouldProduceNotFound

`E4 → AC-EDGE-4 → VB-E04`

HTTP boundary/client / Fact.

Oracle:

An unknown route name results in `NotFound`.

### HTTP11 — ControlTower_SequentialAndConcurrentResults_ShouldMatch

`PD11 → AC-D10 → VB-D10`

HTTP boundary/client / Fact.

Oracle:

`ControlTowerOrchestrator.LoadSequentialAsync` and `LoadConcurrentAsync` produce equivalent final simulation configurations for the same successful control-tower responses.

The concurrent verification uses controlled request gates to establish that the independent requests can begin before combined completion is awaited. It does not use elapsed wall-clock duration as the correctness oracle.

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

Verify finalized contracts, including:

- endpoint shapes;
- JSON contracts;
- deterministic route mapping;
- weather mapping;
- restriction mapping;
- error categories;
- `ControlTowerOrchestrator` sequential/concurrent boundary;
- configured HTTP timeout.

### M04 — ControlTower_SequentialAndConcurrentModes_ShouldBeCompared

`PD10/PD11 → AC-D9/AC-D10 → VB-D09/VB-D10`

Observe:

- each HTTP call logs start and completion/failure;
- equivalent functional results are produced;
- independent requests overlap in the concurrent mode;
- sequential mode does not overlap the independent requests;
- relative execution behaviour can be compared.

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

- at least two drones with distinct names;
- one selected failure target;
- failure immediately after checkpoint `1`;
- failure scenario requires `MaxCheckpoints >= 1`.

### Part C

- at least two drones with distinct names;
- one selected failure target;
- failure immediately after checkpoint `1`;
- failure scenario requires `MaxCheckpoints >= 1`.

### Route

Use the deterministic project fixtures defined in `03-domain-and-rules.md`:

- `Alpha` → `MaxCheckpoints = 3`;
- `Beta` → `MaxCheckpoints = 5`;
- `Gamma` → `MaxCheckpoints = 2`;
- unknown drone name → `404` / `NotFound`.

Also test response validation with:

- zero checkpoint count;
- negative invalid data;
- malformed or missing `maxCheckpoints`.

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
- captured `FlightEvent` values, including `DroneName`;
- Task state;
- expected exception/error kind;
- expected mapped HTTP data;
- final simulation configuration returned by the orchestration;
- observable orchestration completion/failure.

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

    DroneFlight_ValidConfiguration_ShouldBeAccepted

    DroneFlight_NegativeMaxCheckpoints_ShouldBeRejected

    DroneFlight_Checkpoints_ShouldProgressFromZeroToMax

    DroneFlight_ZeroDelay_ShouldCompleteSuccessfully

    ThreadRace_WithJoin_ShouldWaitForAllDrones

    ThreadRace_MultipleDrones_ShouldEnterRunningPhaseConcurrently

    TaskFlight_FaultedTask_ShouldExposeExpectedException

    AsyncFlight_MultipleDrones_ShouldMakeOverlappingProgress

    ControlTower_UnknownDrone_ShouldProduceNotFound

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
- [ ] Flight events identify their originating drone.
- [ ] Part D automated HTTP tests use controllable dependencies; `HTTP00` is verified separately as a smoke/integration check.
- [ ] Final simulation configuration is asserted directly rather than reconstructed by tests.
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

Test files may be created when:

1. all mandatory contracts in `01–05` are closed;
2. every mandatory requirement has a verification path;
3. every acceptance criterion has a verification path;
4. every mandatory vertical behaviour has verification;
5. every automated test has a clear oracle and test level;
6. concurrency tests have deterministic observation strategies;
7. Part D automated tests use controllable HTTP dependencies;
8. no planned test depends on an unspecified contract;
9. the `FlightEvent` contract identifies the originating drone;
10. the `ControlTowerOrchestrator` returns the final simulation configuration;
11. the Part C failure-handling boundary is explicitly defined;
12. the HTTP client timeout is explicitly configured.

The readiness check establishes that planning and test design are internally consistent. It does not require the unfinished production implementation to make the test suite pass.

After the test files are aligned with the locked contract, the implementation follows the intended TDD sequence:

    Red → Green → Refactor → Final verification

The TestPlan is the verification map. The actual xUnit code is developed iteratively against the locked design contract.
## 18. First TDD target

`VB05 — Report checkpoint 0`

Traceability:

`R3 → AC-CORE-3 → B2 → VB05 → T05`

Scenario:

    Given a valid drone with MaxCheckpoints = 0

    When the basic flight executes

    Then a FlightEvent for that drone is observed with Type = CheckpointReached and Checkpoint = 0

Observation boundary:

`Action<FlightEvent>`

---

## 19. Status

Planning/test design is being finalized against the locked public API. Production implementation has intentionally not started.

The next TDD-stage actions are:

- align each existing test file with the locked design contract;
- complete the Definition of Ready consistency check;
- begin production implementation only after the test contracts are stable;
- run Red → Green → Refactor → Final verification.