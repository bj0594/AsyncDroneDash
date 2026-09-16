# Async Drone Dash — Design and Traceability

## 1. Design direction

The solution should remain small and make the required differences between Parts A–C visible.

The common scenario is drone flight; the execution and coordination mechanisms differ:

| Part | Execution | Coordination |
|---|---|---|
| A | `Thread` | `Join` |
| B | `Task` + `TaskCompletionSource` | `Task.WhenAll` |
| C | `async`/`await` | `await Task.WhenAll` |

The design should not hide these differences behind abstractions that make the assignment's comparison harder to understand.

The architecture should be allowed to evolve when the first behaviours are implemented and tested. No class structure is considered final until it has a concrete responsibility.

---

## 2. Responsibility map

### Console / Menu

Handles menu display, user selection, starting the chosen demonstration, and top-level presentation of results/errors.

It does not contain the underlying drone-flight or concurrency logic.

### DroneModel

Represents the required configuration of one drone:

- `Name`
- `MaxCheckpoints`
- `DelayMs`

### Drone flight

Performs one drone's checkpoint progression, applies the configured delay, reports progress, and reaches a successful or failed outcome.

### Part A orchestration

Creates and starts the required threads, performs the normal `Join`-based run, and supports the separate no-`Join` demonstration.

### Part B orchestration

Creates the per-drone TCS/task operations, coordinates them with `Task.WhenAll`, and exposes failure propagation and task exception behaviour.

### Part C orchestration

Starts async drone flights, coordinates them with `await Task.WhenAll`, and handles orchestration errors with `try/catch`.

### Optional HTTP component

Obtains control-tower information for Part D if it is implemented.

---

## 3. Dependency direction

The initial conceptual structure is:

`Console / Menu → Part A/B/C orchestration → Drone flight → DroneModel`

For Part D:

`Orchestration → HTTP component → Control Tower API`

No additional layers are currently justified.

---

## 4. Part A design

The normal Part A run should:

1. prepare at least two drones;
2. create one `Thread` per drone;
3. start the threads;
4. let each thread perform its drone's checkpoint progression;
5. call `Join` for the required threads;
6. report overall completion after the joins finish.

The no-`Join` demonstration should use the same basic flight scenario so that the effect of removing the wait mechanism can be compared directly.

The exact way the demonstration is exposed to the console is not yet fixed.

---

## 5. Part B design

The Part B run should:

1. prepare at least two drones;
2. create one `TaskCompletionSource` per drone;
3. start the corresponding drone work;
4. complete or fault each TCS according to the drone outcome;
5. coordinate the tasks with `Task.WhenAll`;
6. make the required task failure and `Task.Exception` behaviour observable.

The failure trigger must be deterministic. Its exact condition is still open.

---

## 6. Part C design

The Part C run should:

1. start multiple asynchronous drone flights;
2. progress each drone through its checkpoints;
3. use `await Task.Delay` between steps;
4. coordinate the flights with `await Task.WhenAll`;
5. use `try/catch` around the orchestration.

The async flow must not introduce `.Wait()` or `.Result`.

Part C should remain similar enough to Part B at the domain level that their implementation and orchestration can be compared meaningfully.

---

## 7. Preliminary component structure

The MVP should begin with only the responsibilities actually needed:

- console/menu entry point;
- shared `DroneModel`;
- execution/orchestration for Parts A–C;
- supporting flight logic where this creates a clear responsibility or improves testability.

The exact class breakdown is intentionally not locked before the first behaviours are developed.

---

## 8. Preliminary public API

The public API should expose behaviour needed by the application and tests rather than implementation details.

These are **preliminary contracts**. They define the expected interaction strongly enough to design the first behaviours, but may be refined when the first test exposes a better boundary.

| Area | Public member | Parameters | Return type | Exceptions / failure | Side effects |
|---|---|---|---|---|---|
| Drone configuration | `DroneModel` constructor | `Name`, `MaxCheckpoints`, `DelayMs` | `DroneModel` | Invalid configuration: exact exception TBD | Creates drone configuration |
| Part A | `Run` | Collection of drones | `void` | Execution failure: TBD | Starts threads, writes progress, waits with `Join` |
| Part B | `RunAsync` | Collection of drones | `Task` | Task/TCS failure propagates | Starts task-based flights and coordinates them with `Task.WhenAll` |
| Part C | `RunAsync` | Collection of drones | `Task` | Flight failure propagates to orchestration | Starts async flights and awaits `Task.WhenAll` |

The same method name may eventually be avoided across separate classes if that makes the Parts A–C distinction clearer. The table describes the interaction needed, not the final class naming scheme.

### Part A — `Run`

The operation represents the normal thread-based demonstration.

Its contract is that it does not report overall completion until the required `Join` operations have completed.

The no-`Join` demonstration should be a separate interaction or explicit variant rather than silently changing the behaviour of the normal `Run` operation.

### Part B — `RunAsync`

The operation represents the TCS/task demonstration.

Its contract includes task completion and failure propagation. The final decision about whether the caller observes the fault directly or receives it through a result object is still open.

### Part C — `RunAsync`

The operation represents asynchronous drone orchestration.

Its contract includes asynchronous completion, `Task.WhenAll`, and propagation of flight failure to the orchestration boundary.

### DroneModel validation contract

The model must not allow invalid configuration to enter normal flight execution once the validation decision is finalized.

The exact validation location and exception types remain open.

---

## 9. Observability

The application needs enough output to demonstrate:

- drone start;
- checkpoint progress;
- drone completion;
- failure;
- overall completion;
- the effect of `Join`;
- the effect of removing `Join`;
- concurrent/interleaved progress.

Exact ordering of concurrent console output is not a contract.

Where a behaviour needs deterministic testing, the design should allow the test to observe a stable result without depending on thread scheduling.

---

## 10. Testability implications

The design should keep deterministic domain behaviour separable from inherently timing-dependent demonstrations.

Likely automated-test candidates include:

- drone configuration validation;
- checkpoint progression and ordering;
- successful completion;
- deterministic Part B failure;
- task failure propagation;
- async completion/failure behaviour.

Primarily manual/inspection-based candidates include:

- exact concurrent console ordering;
- the visual difference between `Join` and no `Join`;
- implementation use of a specifically required technology when the behaviour itself cannot prove that detail.

Detailed test levels and scenarios belong in `testplan.md`.

---

## 11. Requirement traceability

The traceability chain for mandatory requirements is:

`Requirement → Acceptance Criterion → Behaviour → Test`

The requirement IDs below cover the mandatory core, Parts A–C, and project delivery requirements. Optional Part D requirements are tracked separately because they are not part of the MVP.

| ID | Requirement | Acceptance criteria | Behaviour |
|---|---|---|---|
| R1 | Runnable C# console application | AC-DLV-1 | Project runs |
| R2 | Drone has `Name`, `MaxCheckpoints`, and `DelayMs` | AC-CORE-1 | B1 |
| R3 | Drone progresses from `0` to `MaxCheckpoints` | AC-CORE-2 | B2 / B3 |
| R4 | Configured delay is applied between checkpoint steps | AC-A3 | B4 |
| R5 | Drone progress, start, and completion are reported | AC-CORE-3 | B5 |
| R6 | Part A starts at least two drones concurrently on separate `Thread` instances | AC-A1 | B6 |
| R7 | Part A uses `Join` before reporting overall completion | AC-A2 | B7 |
| R8 | Part A demonstrates the effect of removing `Join` | AC-A4 | B8 |
| R9 | Part A demonstrates interleaved/non-deterministic concurrent output | AC-A5 | B9 |
| R10 | Part B represents drone flights with `Task` | AC-B1 | B10 |
| R11 | Part B uses one `TaskCompletionSource` per drone | AC-B2 | B11 |
| R12 | Part B coordinates drone tasks with `Task.WhenAll` | AC-B3 | B12 |
| R13 | Part B demonstrates a failure scenario | AC-B4 | B13 |
| R14 | Part B propagates failure through the task/TCS model | AC-B5 | B14 |
| R15 | Part B demonstrates task exception observation including `Task.Exception` | AC-B6 | B15 |
| R16 | Part C uses an async drone-flight method | AC-C1 | B16 |
| R17 | Part C uses `await Task.Delay` between checkpoint steps | AC-C2 | B17 |
| R18 | Part C allows multiple drone flights to progress concurrently | AC-C3 | B18 |
| R19 | Part C coordinates flights with `await Task.WhenAll` | AC-C3 | B19 |
| R20 | Part C uses `try/catch` for orchestration errors | AC-C4 | B20 |
| R21 | Part C allows comparison with Part B | AC-C5 | Reflection / implementation comparison |
| R22 | Application provides a menu for Parts A–D | AC-DLV-2 | Menu behaviour |
| R23 | Project is stored in GitHub | AC-DLV-5 | Repository verification |
| R24 | `README.md` contains running and testing instructions | AC-DLV-3 | Documentation verification |
| R25 | `reflection.md` contains the required observations and answers | AC-DLV-4 | Documentation verification |

Each requirement must ultimately be represented by at least one meaningful acceptance criterion or an explicit verification method.

The detailed relationship between behaviours and concrete tests is established later in `testplan.md`.

---

## 12. Design risks

| Risk | Consequence | Current response |
|---|---|---|
| Thread scheduling is non-deterministic | Exact order cannot be asserted reliably | Keep race demonstration primarily observational |
| Concurrent console output | Brittle output tests | Avoid exact ordering assertions |
| Real delays slow tests | Slow test suite | Separate timing demonstration from fast deterministic tests |
| TCS failure semantics are misunderstood | Incorrect Part B implementation | Verify with official documentation or a focused spike |
| `Task.WhenAll` failure behaviour is misunderstood | Incorrect failure handling | Verify before finalizing related tests |
| Async exception flow is misunderstood | Incorrect Part C behaviour | Verify with official .NET documentation |
| Async code becomes blocking | Violates Part C objective | Do not use `.Wait()` or `.Result` in the async path |
| Part D expands scope | MVP delayed | Defer D until MVP is complete |
| Architecture becomes over-engineered | Time spent without improving the solution | Add only concrete responsibilities |

---

## 13. Design decisions

### Keep Parts A–C visibly different

The required execution mechanisms are part of the learning objective. The architecture should therefore not abstract them into a single implementation that hides their differences.

### Separate orchestration from flight work

The component coordinating multiple flights should not also own all drone-specific execution logic.

### Keep the architecture proportional

No additional application/domain/infrastructure layers are introduced unless a real responsibility requires them.

### Keep async flow asynchronous

Part C should propagate asynchronous execution upward rather than blocking synchronously.

### Preserve a simple domain

The domain remains focused on drone configuration and checkpoint flight behaviour. Optional API concepts do not enter the MVP domain unless Part D creates a concrete need.

---

## 14. Open design decisions

- Exact production class names.
- Exact final method names and signatures.
- Exact validation location.
- Exact exception types for invalid configuration.
- Exact Part B failure trigger.
- Exact no-`Join` implementation/entry point.
- Whether a shared abstraction between Parts A–C is useful.
- Whether an interface is justified.
- Final project folder structure.
- Part D architecture if Part D is implemented.

These decisions are intentionally open where fixing them now would amount to designing beyond the information currently available.

---

## 15. First behaviour readiness

A suitable first behaviour is:

> Given a valid drone configuration, when a basic drone flight is executed, the expected checkpoint progression is observable.

The first implementation should establish a small, deterministic boundary around this behaviour before the more concurrency-specific orchestration is built.

Before the first test is written, the production owner, required input, observable result, and public interaction should be clear.

---

# Phase 4–5 status

## Phase 4 — Traceability and risk

- [x] Mandatory requirements have IDs.
- [x] Requirements are linked to acceptance criteria.
- [x] Acceptance criteria are linked to behaviours.
- [x] Important project risks are identified.
- [x] Risk priorities are understood.

## Phase 5 — Solution design

- [x] Major responsibilities have plausible owners.
- [x] Dependency direction is defined.
- [x] Parts A–C have distinct execution responsibilities.
- [x] A preliminary public API exists with parameters, return types, failure/exception considerations and side effects.
- [x] Observability has been considered.
- [x] Unnecessary architecture has been deliberately avoided.
- [ ] Final API contracts are established through the later behaviour/test design.
- [ ] Remaining open design decisions are resolved when sufficient information exists.
