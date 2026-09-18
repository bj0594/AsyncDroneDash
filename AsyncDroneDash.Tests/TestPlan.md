# Async Drone Dash — Test Plan

## 1. Purpose

This document defines how Async Drone Dash will be verified before and during TDD development.

The goal is not to force every assignment requirement into an xUnit test.

Instead, each requirement is assigned the verification method that can actually prove it:

- automated tests;
- component/integration tests;
- implementation inspection;
- manual observation;
- documentation/delivery verification.

The mandatory requirements are the completion contract.

Part D is optional in the assignment but is a project target. Its verification is therefore planned separately and becomes active if Part D remains in final scope.

---

# 2. Verification types

| Prefix | Type | Purpose |
|---|---|---|
| `T` | Automated test | Deterministic behaviour |
| `I` | Implementation inspection | Explicit technology/mechanism requirements |
| `M` | Manual verification | Non-deterministic or demonstration-oriented behaviour |
| `D` | Documentation / delivery | README, reflection, GitHub and project-delivery requirements |

A requirement may have more than one verification item.

---

# 3. Completion rule

The mandatory assignment is fully verified when:

- every mandatory requirement `R1–R25` has a passing verification;
- all automated tests pass;
- all required implementation inspections pass;
- all required manual demonstrations pass;
- all required documentation/delivery checks pass;
- no unresolved contract affects mandatory behaviour.

Passing xUnit tests alone is therefore not sufficient where the assignment explicitly requires a specific implementation mechanism or manual demonstration.

---

# 4. Requirement verification map

The requirement IDs are taken from `Planning/04-design-and-traceability.md`.

| Requirement | Requirement | Verification |
|---|---|---|
| `R1` | Runnable C# console application | `D01` |
| `R2` | `DroneModel` has `Name`, `MaxCheckpoints`, `DelayMs` | `I01`, `T01` |
| `R3` | Drone progresses from `0` to `MaxCheckpoints` | `T04`, `T05` |
| `R4` | Configured delay is applied between checkpoint steps | `T07`, `I02` |
| `R5` | Drone start, checkpoint progress and completion are reported | `T08`, `T13` |
| `R6` | Part A starts at least two drones concurrently using separate `Thread` instances | `T10`, `I03` |
| `R7` | Part A uses `Join` before overall completion | `T11`, `I04` |
| `R8` | Part A demonstrates the effect of removing `Join` | `M01`, `I05` |
| `R9` | Part A demonstrates interleaved/non-deterministic console output | `M02` |
| `R10` | Part B represents drone flights using `Task` | `T15`, `I06` |
| `R11` | Part B uses one `TaskCompletionSource` per drone | `I07` |
| `R12` | Part B uses `Task.WhenAll` | `T17`, `T21`, `I08` |
| `R13` | Part B demonstrates a failure scenario | `T18` |
| `R14` | Part B propagates failure through the task/TCS model | `T19` |
| `R15` | Part B demonstrates task exception handling including `Task.Exception` | `T20`, `I09` |
| `R16` | Part C uses an async drone-flight method | `T22`, `I10` |
| `R17` | Part C uses `await Task.Delay` | `T23`, `I11` |
| `R18` | Part C allows multiple drone flights to progress concurrently | `T24` |
| `R19` | Part C uses `await Task.WhenAll` | `T25`, `I12` |
| `R20` | Part C uses `try/catch` around orchestration | `T26`, `I13` |
| `R21` | Part C allows comparison with Part B | `D05` |
| `R22` | Application provides a menu for Parts A–D | `M03` |
| `R23` | Project is stored in GitHub | `D06` |
| `R24` | README contains running/testing instructions | `D07`, `D08` |
| `R25` | `reflection.md` contains required observations/answers | `D09` |

---

# 5. Scenario matrix

| Behaviour | Happy | Boundary | Equivalence | Empty/null | Missing | Duplicate | State | Data variation | Dependency failure |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `VB01` valid configuration | ✓ | — | — | — | — | — | — | ✓ | — |
| `VB02` negative `MaxCheckpoints` | — | ✓ | ✓ | — | — | — | — | ✓ | — |
| `VB03` negative `DelayMs` | — | ✓ | ✓ | — | — | — | — | ✓ | — |
| `VB04` missing/blank name | — | ✓ | ✓ | ✓ | ✓ | — | — | ✓ | — |
| `VB05` first checkpoint | ✓ | ✓ (`Max=0`) | — | — | — | — | Running | ✓ | — |
| `VB06` checkpoint progression | ✓ | ✓ | ✓ | — | — | — | Running | ✓ | — |
| `VB07` successful completion | ✓ | ✓ (`Max=0`) | — | — | — | — | Running→Completed | ✓ | — |
| `VB08` Part A concurrency | ✓ | — | — | — | — | — | Running | ≥2 drones | — |
| `VB09` Part A Join | ✓ | — | — | — | — | — | Running→Completed | ≥2 drones | — |
| `VB10` Part A no-Join | — | — | — | — | — | — | Running | ≥2 drones | — |
| `VB11` concurrent output | — | — | — | — | — | — | — | ≥2 drones | — |
| `VB12` Part B task completion | ✓ | — | — | — | — | — | Running→Completed | ≥2 drones | — |
| `VB13` one TCS per drone | ✓ | — | — | — | — | — | — | ≥2 drones | — |
| `VB14` Part B `Task.WhenAll` | ✓ | — | — | — | — | — | Running→Completed | ≥2 tasks | — |
| `VB15` Part B failure | — | — | — | — | — | — | Running→Faulted | — | — |
| `VB16` failure propagation | — | — | — | — | — | — | Running→Faulted | — | — |
| `VB17` `Task.Exception` | — | — | — | — | — | — | Faulted | — | — |
| `VB18` async flight | ✓ | — | — | — | — | — | Running→Completed | ≥1 drone | — |
| `VB19` async delay | ✓ | — | — | — | — | — | Running | ✓ | — |
| `VB20` multiple async flights | ✓ | — | — | — | — | — | Running→Completed | ≥2 drones | — |
| `VB21` async `Task.WhenAll` | ✓ | — | — | — | — | — | Running→Completed | ≥2 tasks | — |
| `VB22` async failure handling | — | — | — | — | — | — | Running→Faulted | — | — |

Duplicate testing is not currently applicable because the domain defines no duplicate behaviour.

Missing/not-found testing is only relevant if a lookup/registry is introduced.

Null testing is only relevant where the final public API can actually receive `null`.

---

# 6. Core test inventory

## T01 — Valid configuration

Behaviour: `VB01`

Scenario:

A drone has a valid name, non-negative `MaxCheckpoints`, and non-negative `DelayMs`.

Expected result:

The configuration is accepted for normal flight.

Level: Unit

Form: Fact

Oracle: Configuration is accepted and retains the supplied values.

---

## T02 — Negative MaxCheckpoints

Behaviour: `VB02`

Representative values:

- `-1`
- additional negative value if useful

Expected result:

The final validation contract.

Level: Unit

Form: Theory

Oracle: Exact documented validation result.

Status: blocked until the validation contract is finalized.

---

## T03 — Negative DelayMs

Behaviour: `VB03`

Representative values:

- `-1`
- additional negative value if useful

Expected result:

The final validation contract.

Level: Unit

Form: Theory

Oracle: Exact documented validation result.

Status: blocked until the validation contract is finalized.

---

## T04 — Checkpoint progression

Behaviour: `VB06`

Representative values:

- `MaxCheckpoints = 0`
- `MaxCheckpoints = 1`
- `MaxCheckpoints = 3`
- another representative positive value if useful

Expected sequence:

`0..MaxCheckpoints`

Expected result:

No checkpoint is skipped, duplicated, reordered or reported above `MaxCheckpoints`.

Level: Unit

Form: Theory

Oracle: Captured checkpoint sequence equals the expected sequence.

---

## T05 — Zero checkpoints

Behaviour: `VB05`

Scenario:

`MaxCheckpoints = 0`

Expected result:

Checkpoint `0` is reported and the flight can complete.

Level: Unit

Form: Fact

Oracle: Checkpoint `0` is observable and completion occurs.

---

## T06 — Checkpoint order

Behaviour: `VB06`

Scenario:

A drone has multiple checkpoints.

Expected result:

All checkpoints appear exactly once and in ascending order.

Level: Unit

Form: Fact

Oracle: Observed sequence matches the expected ordered sequence.

---

## T07 — Configured delay

Core delay behaviour / `VB06`

Scenario:

A drone uses a positive `DelayMs`.

Expected result:

The configured delay mechanism is applied between checkpoint steps.

Level: Unit if a deterministic delay seam exists; otherwise component/inspection.

Oracle: Configured delay value is applied at the correct point.

Do not assert arbitrary wall-clock duration.

---

## T08 — Core flight logging

Behaviours: `VB05`, `VB06`, `VB07`

Scenario:

One valid drone completes a normal flight.

Expected output/events:

1. start;
2. every checkpoint;
3. completion.

Level: Component

Form: Fact

Oracle: All required events are present for the correct drone.

A single-drone scenario is used so concurrency does not affect ordering.

---

## I01 — DroneModel contract

Requirement: `R2`

Inspect the production model.

Pass condition:

- `Name : string`
- `MaxCheckpoints : int`
- `DelayMs : int`

Any justified deviation must be documented.

---

## I02 — Delay implementation

Requirement: `R4`

Inspect production code.

Pass condition:

The configured delay is applied between checkpoint steps and not unnecessarily before the first or after the final checkpoint.

---

# 7. Part A — Thread Race

## T10 — Multiple concurrent drone flights

Behaviour: `VB08`

Scenario:

At least two valid drones are supplied.

Expected result:

All required drone flights complete.

Level: Component

Form: Fact

Oracle: Each required drone reaches completion.

Do not assert a particular scheduling order.

---

## T11 — Join waits for all drones

Behaviour: `VB09`

Scenario:

At least two drone flights have controlled completion.

Expected result:

Overall completion cannot occur before all required drone flights have completed.

Level: Component

Form: Fact

Oracle: Overall completion follows every required drone completion.

Use deterministic synchronization rather than arbitrary sleep durations.

---

## T12 — Part A checkpoint progression per drone

Behaviour: `VB06`

Scenario:

At least two Part A drones have different checkpoint counts.

Expected result:

Each drone reports its own complete `0..MaxCheckpoints` sequence.

Level: Component

Form: Fact

Oracle: Each drone's captured sequence matches its own configuration.

---

## T13 — Part A logging

Requirement: `R5`

Scenario:

At least two drones complete the normal Part A run.

Expected result:

For every drone:

- start is logged;
- every checkpoint is logged;
- completion is logged.

Level: Component

Form: Fact

Oracle: Required lifecycle/progress entries are present for every drone.

Exact cross-thread ordering is not asserted.

---

## M01 — No-Join demonstration

Requirement: `R8`

Behaviour: `VB10`

Manual verification.

Expected observation:

The main thread can continue before all drone threads have completed.

---

## M02 — Non-deterministic output

Requirement: `R9`

Behaviour: `VB11`

Manual verification.

Expected observation:

Concurrent console output can become interleaved or change order between runs.

Exact ordering is not a contract.

---

## I03 — Part A uses Thread

Requirement: `R6`

Pass condition:

Each Part A drone is executed on a separate `Thread`.

---

## I04 — Part A uses Join

Requirement: `R7`

Pass condition:

The normal Part A orchestration calls `Join` for the required drone threads.

---

## I05 — Real no-Join variant exists

Requirement: `R8`

Pass condition:

A deliberate Part A path demonstrates execution without `Join`.

---

# 8. Part B — Task + TaskCompletionSource

## T15 — Successful Task-based flight

Behaviour: `VB12`

Requirement: `R10`

Scenario:

A valid drone completes Part B without failure.

Expected result:

The corresponding task completes successfully.

Level: Component

Form: Fact

Oracle: Task reaches successful completion.

---

## T16 — Independent per-drone completion

Behaviour: `VB13`

Scenario:

At least two drones are executed.

Expected result:

Each drone has an independently controlled completion outcome.

Level: Component

Form: Fact

Oracle: Each drone can complete independently.

Implementation inspection separately proves the required TCS mechanism.

---

## T17 — Task.WhenAll waits for all tasks

Behaviour: `VB14`

Requirement: `R12`

Scenario:

Multiple drone tasks have controlled completion.

Expected result:

Combined completion occurs only after all participating tasks have completed.

Level: Component

Form: Fact

Oracle: Combined completion follows all participating tasks.

---

## T18 — Deterministic failure

Behaviour: `VB15`

Requirement: `R13`

Scenario:

The chosen failure trigger occurs.

Expected result:

The affected operation becomes faulted.

Level: Component

Form: Fact

Oracle: Expected fault state and exception.

Status: blocked until the failure contract is finalized.

---

## T19 — Failure propagation

Behaviour: `VB16`

Requirement: `R14`

Scenario:

One drone operation fails.

Expected result:

The failure reaches the orchestration boundary and is not silently swallowed.

Level: Component

Form: Fact

Oracle: Expected failure reaches the documented boundary.

---

## T20 — Task.Exception

Behaviour: `VB17`

Requirement: `R15`

Scenario:

The configured failure occurs.

Expected result:

The relevant exception is available through `Task.Exception`.

Level: Component

Form: Fact

Oracle: `Task.Exception` contains the expected exception information.

---

## T21 — WhenAll failure coordination

Requirements: `R12`, `R14`

Scenario:

One participating task faults while another participating task has not yet completed.

Expected result:

The combined operation correctly represents the terminal outcome of all participating tasks.

Level: Component

Form: Fact

Oracle:

- failed task reaches faulted state;
- remaining task can complete;
- combined operation does not falsely report success before the required tasks have reached terminal states.

This is an additional risk-based verification of the coordination contract.

---

## I06 — Part B uses Task

Requirement: `R10`

Pass condition:

Drone-flight operations are represented with `Task`.

---

## I07 — One TCS per drone

Requirement: `R11`

Pass condition:

Each participating drone has its own `TaskCompletionSource`.

---

## I08 — Part B uses Task.WhenAll

Requirement: `R12`

Pass condition:

The participating tasks are coordinated using `Task.WhenAll`.

---

## I09 — Part B demonstrates Task.Exception

Requirement: `R15`

Pass condition:

The failure demonstration explicitly observes `Task.Exception`.

---

# 9. Part C — Async/Await

## T22 — Successful async flight

Behaviour: `VB18`

Requirement: `R16`

Scenario:

A valid drone completes a normal async flight.

Expected result:

The asynchronous operation completes successfully.

Level: Component

Form: Fact

Oracle: Successful completion and expected flight result.

---

## T23 — Async checkpoint delay

Behaviour: `VB19`

Requirement: `R17`

Scenario:

A drone has multiple checkpoints.

Expected result:

Checkpoint delays use asynchronous delay behaviour.

Level: Component where a deterministic seam exists; otherwise inspection plus behavioural verification.

Form: Fact

Oracle: Required async delay occurs without synchronous blocking.

Do not assert exact wall-clock duration.

---

## T24 — Multiple async flights

Behaviour: `VB20`

Requirement: `R18`

Scenario:

At least two drones are started.

Expected result:

All participating async flights complete and are not intentionally serialized.

Level: Component

Form: Fact

Oracle: All flights complete successfully.

---

## T25 — Async Task.WhenAll

Behaviour: `VB21`

Requirement: `R19`

Scenario:

Multiple async flights have controlled completion.

Expected result:

Overall completion occurs only after the combined operation completes.

Level: Component

Form: Fact

Oracle: Overall completion follows all participating async flights.

---

## T26 — Async failure handling

Behaviour: `VB22`

Requirement: `R20`

Scenario:

A controlled async flight failure occurs.

Expected result:

The failure reaches the orchestration boundary and is handled by the documented `try/catch`.

Level: Component

Form: Fact

Oracle: Expected failure is observed by orchestration.

---

## I10 — Part C uses async flight method

Requirement: `R16`

Pass condition:

The drone-flight operation is implemented as an actual `async` method.

---

## I11 — Part C uses await Task.Delay

Requirement: `R17`

Pass condition:

Checkpoint delays use `await Task.Delay`.

---

## I12 — Part C uses await Task.WhenAll

Requirement: `R19`

Pass condition:

Multiple flights are coordinated with `await Task.WhenAll`.

---

## I13 — Part C uses try/catch

Requirement: `R20`

Pass condition:

Required orchestration-level `try/catch` exists and handles/report failures.

---

## I14 — No synchronous blocking in async flow

Pass condition:

No `.Wait()` or `.Result` blocks the Part C async execution path.

---

# 10. Part B / Part C comparison

## D05 — Comparison evidence

Requirement: `R21`

Acceptance criterion: `AC-C5`

Verification:

Documentation review after Parts B and C exist.

Pass condition:

The comparison addresses:

- boilerplate;
- complexity;
- readability;
- maintainability.

The comparison should be based on the actual implementations rather than general assumptions about the technologies.

---

# 11. Menu and delivery verification

## M03 — Parts A–D are accessible from the menu

Requirement: `R22`

Manual verification.

Pass conditions:

- menu appears;
- Part A is selectable;
- Part B is selectable;
- Part C is selectable;
- Part D has a menu path;
- each selection reaches the intended path;
- return/exit behaviour works according to the final design.

If Part D is not implemented at the time of MVP completion, the menu must clearly represent its optional/unavailable state rather than silently failing.

---

## D01 — Production application builds

Requirement: `R1`

Run:

`dotnet build`

Pass condition:

Solution builds without errors.

---

## D02 — Test infrastructure runs

Run:

`dotnet test`

Pass condition:

Test project is discovered and the current suite executes.

---

## D06 — GitHub repository

Requirement: `R23`

Pass conditions:

- repository exists;
- current project state is pushed;
- required files are present;
- no secrets are committed.

---

## D07 — README running instructions

Requirement: `R24`

Pass conditions:

README explains:

- prerequisites;
- build;
- run;
- tests.

---

## D08 — README testing instructions

Requirement: `R24`

Pass condition:

README explains how Parts A–D are tested/observed.

---

## D09 — Reflection requirements

Requirement: `R25`

Pass condition:

`reflection.md` contains the required observations, short answers and relevant thoughts.

---

# 12. Part D — Optional target

Part D is optional in the assignment but is currently a project target.

Automated HTTP tests should not depend on an uncontrolled external Internet service.

Use a controllable local endpoint or test HTTP boundary for automated verification.

## D10 — Route information retrieval

Valid route response is correctly retrieved and mapped.

Level: Integration

Oracle: Expected route data.

---

## D11 — Weather information retrieval

Valid weather response is correctly retrieved and mapped.

Level: Integration

Oracle: Expected weather data.

---

## D12 — Temporary restrictions retrieval

Valid restriction response is correctly retrieved and mapped.

Level: Integration

Oracle: Expected restriction data.

A no-restriction case should be included if the chosen contract represents one.

---

## D13 — Control-tower data affects simulation

Scenario:

Retrieved data should modify simulation parameters.

Expected result:

Documented rules change `DelayMs` and/or `MaxCheckpoints`.

Level: Integration

Oracle: Exact documented parameter change.

---

## D14 — HTTP non-success response

Scenario:

HTTP service returns a non-success status.

Expected result:

The error is handled according to the final contract.

Level: Integration

Oracle: Expected error result/exception.

---

## D15 — HTTP timeout

Scenario:

HTTP service does not respond within the configured timeout.

Expected result:

Timeout is handled according to the final contract.

Level: Integration

Oracle: Expected timeout result/exception.

---

## D16 — HTTP request lifecycle logging

Scenario:

An HTTP request starts and finishes/fails.

Expected result:

Required request lifecycle information is observable.

Level: Integration/manual

---

## D17 — Concurrent HTTP calls

Scenario:

Independent control-tower calls are available.

Expected result:

Calls can be executed concurrently and all required results are collected.

Level: Integration

Oracle: All expected data is returned correctly.

---

## D18 — Sequential/concurrent equivalence

Scenario:

The same logical control-tower data is obtained sequentially and concurrently.

Expected result:

Both modes produce equivalent functional data.

Performance difference is observed separately and is not a brittle correctness assertion.

Level: Integration + manual

---

## D19 — Selected HTTP technology

Conditional inspection.

Pass condition:

The final implementation uses the technology selected in the Part D design, such as:

- `HttpClient`;
- asynchronous HTTP APIs;
- `ReadFromJsonAsync`;
- `HttpListener` if the local-service option is chosen.

---

## Part D additional failures

Add tests only where the final contract makes them meaningful:

- connection failure;
- invalid JSON;
- missing response fields;
- invalid control-tower values;
- service unavailable.

---

# 13. Contract blockers

These must be resolved before dependent tests are locked.

## Validation

- exact negative `MaxCheckpoints` behaviour;
- exact negative `DelayMs` behaviour;
- exact blank/missing `Name` behaviour;
- exact exception/result types;
- exact validation location.

## Part B

- exact deterministic failure trigger;
- exact exception type;
- exact propagation contract.

## Part D

- external API or local service;
- response models;
- exact effect of route/weather/restrictions;
- timeout policy;
- logging contract;
- failure-data contract.

No test should invent a contract simply to make the implementation compile.

---

# 14. Final test-design review

Before writing the first meaningful test code:

- [ ] Every `R1–R25` has a verification path.
- [ ] Every mandatory acceptance criterion has a verification path.
- [ ] Every mandatory vertical behaviour has a verification path.
- [ ] Every automated test has a clear oracle.
- [ ] Every explicit technology requirement has an inspection item.
- [ ] Every intentionally non-deterministic requirement has manual verification.
- [ ] Relevant boundaries are covered.
- [ ] Relevant equivalence partitions are covered.
- [ ] Relevant state transitions are covered.
- [ ] Relevant dependency failures are covered.
- [ ] Timing tests do not depend on fragile wall-clock thresholds.
- [ ] Concurrency tests do not depend on exact scheduling.
- [ ] Expected results do not duplicate production algorithms.
- [ ] Test levels match what they can actually prove.
- [ ] Test data is representative.
- [ ] Duplicate tests have been removed.
- [ ] Part D remains separate from mandatory MVP completion.
- [ ] No mandatory verification depends on an unresolved `TBD`.

---

# 15. First TDD behaviour

The first candidate remains:

`VB05 — Report the first checkpoint`

Traceability:

`R3 → AC-CORE-2 → VB05 → T05`

The first test should prove:

> Given a valid drone with `MaxCheckpoints = 0`, when its flight is executed, checkpoint `0` is observable.

Before writing the test, the final public interaction and deterministic observation boundary must be established.

Then:

`Choose behaviour → write test → RED → minimum GREEN → REFACTOR → update inventory → next behaviour`

---

# 16. Definition of fully verified

The mandatory project is fully verified when:

- all required automated tests pass;
- all required inspections pass;
- all required manual demonstrations pass;
- all required documentation/delivery checks pass;
- every mandatory requirement `R1–R25` has passing verification;
- no mandatory contract remains unresolved.

At that point, the verification plan functions as the project's evidence-based specification for the mandatory assignment.