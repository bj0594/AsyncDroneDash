# Async Drone Dash — Test Plan

## 1. Purpose

This document defines the verification plan for the project.

The goal is that every mandatory requirement has a concrete verification path.

Verification may be:

- automated test;
- implementation inspection;
- manual observation;
- documentation/delivery verification.

Part D is optional in the assignment but is included as a planned target.

---

# 2. Requirement verification

| Requirement | Verification |
|---|---|
| `R1` Runnable C# console application | `D01` |
| `R2` Required `DroneModel` properties | `I01`, `T01` |
| `R3` Progress `0..MaxCheckpoints` | `T04`, `T05` |
| `R4` Configured delay | `T07`, `I02` |
| `R5` Start/progress/completion reporting | `T08`, `T13` |
| `R6` Separate Part A Threads | `T10`, `I03` |
| `R7` Part A Join | `T11`, `I04` |
| `R8` Part A no-Join demonstration | `M01`, `I05` |
| `R9` Part A non-deterministic output | `M02` |
| `R10` Part B Task | `T15`, `I06` |
| `R11` One TCS per drone | `T16`, `I07` |
| `R12` Part B Task.WhenAll | `T17`, `I08` |
| `R13` Part B failure scenario | `T18` |
| `R14` Part B failure propagation | `T19` |
| `R15` Task.Exception | `T20`, `I09` |
| `R16` Part C async flight | `T22`, `I10` |
| `R17` Part C Task.Delay | `T23`, `I11` |
| `R18` Multiple async flights | `T24` |
| `R19` Part C Task.WhenAll | `T25`, `I12` |
| `R20` Part C try/catch | `T26`, `I13` |
| `R21` B/C comparison | `D05` |
| `R22` Menu A–D | `M03` |
| `R23` GitHub repository | `D06` |
| `R24` README requirements | `D07`, `D08` |
| `R25` Reflection requirements | `D09` |

---

# 3. Core test matrix

| Behaviour | Happy | Boundary | Equivalence | Empty/null | Missing | Duplicate | State | Data variation | Dependency failure |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `VB01` Valid configuration | ✓ | — | — | — | — | — | — | ✓ | — |
| `VB02` Negative MaxCheckpoints | — | ✓ | ✓ | — | — | — | — | ✓ | — |
| `VB03` Negative DelayMs | — | ✓ | ✓ | — | — | — | — | ✓ | — |
| `VB04` Missing/blank name | — | ✓ | ✓ | ✓ | ✓ | — | — | ✓ | — |
| `VB05` Checkpoint 0 | ✓ | ✓ (`Max=0`) | — | — | — | — | Running | ✓ | — |
| `VB06` Checkpoint progression | ✓ | ✓ | ✓ | — | — | — | Running | ✓ | — |
| `VB07` Completion | ✓ | ✓ (`Max=0`) | — | — | — | — | Running→Completed | ✓ | — |
| `VB08` Part A concurrency | ✓ | — | — | — | — | — | Running | ≥2 drones | — |
| `VB09` Part A Join | ✓ | — | — | — | — | — | Running→Completed | ≥2 drones | — |
| `VB10` Part A no-Join | — | — | — | — | — | — | Running | ≥2 drones | — |
| `VB11` Concurrent output | — | — | — | — | — | — | — | ≥2 drones | — |
| `VB12` Task completion | ✓ | — | — | — | — | — | Running→Completed | ≥2 drones | — |
| `VB13` Individual TCS | ✓ | — | — | — | — | — | — | ≥2 drones | — |
| `VB14` Task.WhenAll | ✓ | — | — | — | — | — | Running→Completed | ≥2 tasks | — |
| `VB15` Part B failure | — | — | — | — | — | — | Running→Faulted | — | — |
| `VB16` Failure propagation | — | — | — | — | — | — | Running→Faulted | — | — |
| `VB17` Task.Exception | — | — | — | — | — | — | Faulted | — | — |
| `VB18` Async flight | ✓ | — | — | — | — | — | Running→Completed | ≥1 drone | — |
| `VB19` Async delay | ✓ | — | — | — | — | — | Running | ✓ | — |
| `VB20` Multiple async flights | ✓ | — | — | — | — | — | Running→Completed | ≥2 drones | — |
| `VB21` Async Task.WhenAll | ✓ | — | — | — | — | — | Running→Completed | ≥2 tasks | — |
| `VB22` Async failure | — | — | — | — | — | — | Running→Faulted | — | — |

Duplicate testing is not currently relevant because no duplicate behaviour exists in the domain.

Missing/not-found and null cases are only required where the final public contract actually permits them.

---

# 4. Core test inventory

## T01 — Valid configuration

`VB01`

A valid drone configuration can be used for a normal flight.

Level: Unit  
Form: Fact  
Oracle: Configuration is accepted.

---

## T02 — Negative MaxCheckpoints

`VB02`

Representative negative values.

Level: Unit  
Form: Theory  
Oracle: Final validation contract.

Status: dependent on the unresolved validation decision.

---

## T03 — Negative DelayMs

`VB03`

Representative negative values.

Level: Unit  
Form: Theory  
Oracle: Final validation contract.

Status: dependent on the unresolved validation decision.

---

## T04 — Checkpoint progression

`VB06`

Representative values:

- `0`
- `1`
- `3`
- another representative positive value if useful

Oracle:

Exact sequence `0..MaxCheckpoints`.

Level: Unit  
Form: Theory.

---

## T05 — Zero checkpoints

`VB05`

With `MaxCheckpoints = 0`, checkpoint `0` is reported and the flight can complete.

Level: Unit  
Form: Fact.

---

## T06 — Checkpoint order

`VB06`

All checkpoints appear once and in ascending order.

Level: Unit  
Form: Fact.

---

## T07 — Configured delay

`VB06` / `B4`

A valid configured delay is applied between checkpoint steps.

Do not use exact wall-clock duration as the oracle.

Level: Unit/component depending on the observation mechanism.

---

## T08 — Core lifecycle reporting

`VB05–VB07`

A single successful drone reports:

- start;
- every checkpoint;
- completion.

Level: Component  
Form: Fact.

A single-drone case keeps the output deterministic.

---

# 5. Part A

## T10 — Multiple drone flights

`VB08`

At least two drones complete using separate thread execution.

Level: Component  
Form: Fact.

---

## T11 — Join waits

`VB09`

Overall completion occurs only after all required drone threads have completed.

Level: Component  
Form: Fact.

No arbitrary wall-clock threshold.

---

## T12 — Per-drone checkpoint progression

`VB06`

At least two drones with different checkpoint counts each produce their own correct sequence.

Level: Component  
Form: Fact.

---

## T13 — Part A logging

`R5`

For every drone:

- start;
- all checkpoints;
- completion.

Exact cross-thread ordering is not asserted.

Level: Component  
Form: Fact.

### I03 — Thread usage

`R6`

Inspect that Part A creates one `Thread` per drone.

### I04 — Join usage

`R7`

Inspect that the normal run uses `Join`.

### I05 — No-Join implementation

`R8`

Inspect that a deliberate no-Join path exists.

### M01 — No-Join observation

`R8`

Observe the main thread continuing before all drone threads finish.

### M02 — Concurrent output

`R9`

Observe interleaved/reordered output.

---

# 6. Part B

## T15 — Task-based flight

`VB12`

A successful drone flight is represented by a completed `Task`.

Level: Component  
Form: Fact.

---

## T16 — Independent per-drone completion

`VB13`

At least two drones can complete independently.

Level: Component  
Form: Fact.

---

## T17 — Task.WhenAll

`VB14`

Combined completion occurs only after all participating tasks have completed.

Level: Component  
Form: Fact.

---

## T18 — Deterministic failure

`VB15`

The selected failure scenario faults the affected operation.

Level: Component  
Form: Fact.

The exact failure trigger remains TBD in the current planning documents.

---

## T19 — Failure propagation

`VB16`

The failed operation reaches the orchestration boundary.

Level: Component  
Form: Fact.

---

## T20 — Task.Exception

`VB17`

The relevant fault information is observable through `Task.Exception`.

Level: Component  
Form: Fact.

---

## T21 — Failure during Task.WhenAll

One participating task faults while another has not yet completed.

The combined operation must correctly represent the final task outcomes.

This is an additional risk-based test of the coordination behaviour.

### I06 — Task usage

`R10`

Inspect that Part B uses `Task`.

### I07 — One TCS per drone

`R11`

Inspect that each participating drone has its own `TaskCompletionSource`.

### I08 — Task.WhenAll

`R12`

Inspect that Part B coordinates tasks with `Task.WhenAll`.

### I09 — Task.Exception

`R15`

Inspect that the required failure demonstration explicitly observes `Task.Exception`.

---

# 7. Part C

## T22 — Async flight

`VB18`

A valid drone completes through an async flight operation.

Level: Component  
Form: Fact.

---

## T23 — Async checkpoint delay

`VB19`

Checkpoint waiting uses `await Task.Delay`.

Level: Component plus inspection as needed.

Do not use exact elapsed milliseconds as the oracle.

---

## T24 — Multiple async flights

`VB20`

At least two async drone flights complete without deliberately serializing independent work.

Level: Component  
Form: Fact.

---

## T25 — Await Task.WhenAll

`VB21`

Overall completion occurs only after the combined async operation completes.

Level: Component  
Form: Fact.

---

## T26 — Async failure handling

`VB22`

A failed async flight reaches orchestration and is handled by `try/catch`.

Level: Component  
Form: Fact.

### I10 — Async flight method

`R16`

Inspect that the flight operation is implemented as an async method.

### I11 — await Task.Delay

`R17`

Inspect that checkpoint delays use `await Task.Delay`.

### I12 — await Task.WhenAll

`R19`

Inspect that Part C uses `await Task.WhenAll`.

### I13 — try/catch

`R20`

Inspect orchestration for the required `try/catch`.

### I14 — No synchronous blocking

Inspect that `.Wait()` and `.Result` are not used in the Part C async flow.

---

# 8. Part B / Part C comparison

## D05 — Comparison evidence

`R21` / `AC-C5`

After both parts exist, review the implementation and reflection for comparison of:

- boilerplate;
- complexity;
- readability;
- maintainability.

This is not a correctness unit test.

---

# 9. Menu and delivery

### M03 — Menu for A–D

`R22`

Manual check:

- menu appears;
- A, B, C and D are represented;
- selections reach the intended path;
- final return/exit behaviour works.

Part D may be represented as optional/unimplemented while it remains out of MVP implementation scope.

### D01 — Build

`R1`

`dotnet build` succeeds.

### D02 — Test runner

`dotnet test` discovers and runs the current suite.

### D06 — GitHub repository

`R23`

Repository exists, current project state is pushed, and no secrets are committed.

### D07 — README instructions

`R24`

README explains prerequisites, build, run and test commands.

### D08 — README testing

`R24`

README explains how Parts A–D are tested/observed.

### D09 — Reflection

`R25`

`reflection.md` contains the required observations, short answers and relevant thoughts.

---

# 10. Part D — Optional target

Part D is optional and its exact design is not yet locked by the current planning documents.

The test plan therefore defines the required verification categories without inventing the final API contract.

| ID | Behaviour | Verification |
|---|---|---|
| `D10` | `VB-D01` route retrieval | Integration |
| `D11` | `VB-D02` weather retrieval | Integration |
| `D12` | `VB-D03` restriction retrieval | Integration |
| `D13` | `VB-D04` retrieved data affects simulation | Integration |
| `D14` | `VB-D05` HTTP failure | Integration |
| `D15` | `VB-D06` timeout | Integration |
| `D16` | `VB-D07` HTTP logging | Integration/manual |
| `D17` | `VB-D08` sequential/concurrent comparison | Integration/manual |

Automated HTTP tests should use a controllable HTTP boundary rather than depend on an uncontrolled external service.

Exact endpoints, response models, failure types and data mapping remain open until Part D design is finalized.

---

# 11. Part D scenario categories

Where relevant, evaluate:

- valid route response;
- valid weather response;
- valid restrictions response;
- no restriction;
- invalid response data;
- HTTP non-success;
- timeout;
- service unavailable;
- sequential requests;
- concurrent requests;
- observable HTTP logging.

Only scenarios supported by the final Part D contract should become mandatory tests.

---

# 12. Fact / Theory decisions

### Fact

Use for distinct meaningful scenarios such as:

- zero checkpoints;
- successful completion;
- Part B failure;
- `Task.Exception`;
- orchestration behaviour;
- HTTP timeout.

### Theory

Use where the same rule genuinely applies to several datasets:

- numeric validation;
- name validation;
- checkpoint progression.

Do not parameterize merely to reduce the number of test methods.

---

# 13. Oracle rules

Preferred automated-test oracles:

- exact checkpoint sequence;
- captured flight events;
- expected state;
- task completion/failure state;
- expected exception;
- expected mapped Part D data.

Avoid:

- exact thread scheduling order;
- exact wall-clock duration;
- duplicated production calculations inside tests.

---

# 14. Contract blockers

The following must be resolved before their dependent tests are finalized:

- negative `MaxCheckpoints`;
- negative `DelayMs`;
- missing/blank name;
- unknown/missing drone;
- Part B failure trigger;
- Part D API/data contract.

No test should invent an expected result for an unresolved contract.

---

# 15. Final test-design review

Before implementation:

- [ ] Every `R1–R25` has a verification path.
- [ ] Every mandatory behaviour has a verification path.
- [ ] Relevant boundaries are tested.
- [ ] Relevant state transitions are tested.
- [ ] Implementation-specific requirements have inspection checks.
- [ ] Non-deterministic demonstrations have manual checks.
- [ ] Every automated test has a clear oracle.
- [ ] Timing tests do not depend on fragile wall-clock thresholds.
- [ ] Concurrency tests do not assume a fixed scheduling order.
- [ ] No unnecessary duplicate tests remain.
- [ ] Part D remains clearly optional.
- [ ] No mandatory test depends on unresolved contract decisions.

---

# 16. First TDD behaviour

`VB05 — Report checkpoint 0`

Traceability:

`R3 → AC-CORE-2 → VB05 → T05`

Scenario:

```text
Given a valid drone with MaxCheckpoints = 0
When the basic flight starts
Then checkpoint 0 is observable
```

The first test should use the final public observation boundary defined in the design.

No production implementation should be written until the planning and test-design package has been completed and reviewed.