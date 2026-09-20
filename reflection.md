# Reflection

## 1. What happened when Join was removed in Part A?

`Join` makes the main thread wait for each drone thread to complete before `RunWithJoin` returns. In the no-`Join` variant, the main thread continues immediately while the drone threads are still working. This means the main-thread message can appear before all checkpoint messages have been written. The order between drone threads is also non-deterministic.

This also shows why `Console` is not a good synchronization mechanism. Multiple threads can write at the same time, and which event appears first depends on thread scheduling.

## 2. Thread/Join, Task/TCS, and async/await

`Thread` provides direct control over threads and makes concurrent execution easy to see, but the model requires explicit creation, starting, and `Join` of the threads. Error handling must also be organized separately because an exception inside a thread is not automatically propagated to the main thread.

`Task` provides a higher-level abstraction for completion, while `TaskCompletionSource` makes it possible to control when each operation is marked as completed or faulted. In this part, one TCS was used per drone, and `Task.WhenAll` was used to coordinate them. The model clearly demonstrates how explicit signaling works, but it requires more code than async/await because completion and errors must be set manually.

An important observation is that `Task.Exception` is an `AggregateException`. This makes task state and the underlying error explicitly observable.

`async`/`await` expresses the same type of coordination with less boilerplate. The flight itself can read like a sequence — report a checkpoint, wait asynchronously, continue — while multiple flights can still overlap. `await Task.WhenAll` makes overall coordination readable without manual TCS management.

For maintainability, the main difference is that async/await makes the control flow more directly visible. Task/TCS is still useful when a program needs an explicit external signal for completion or failure, but in this project such manual control was only needed because the assignment was intended to demonstrate the mechanism.

## 3. What was challenging about asynchronous HTTP?

The most important part was keeping the HTTP flow asynchronous while the client had to translate several types of errors into a stable domain vocabulary. `ControlTowerClient` uses one reused `HttpClient`, `GetAsync`, and asynchronous response reading. HTTP errors are translated into `ControlTowerException` with `RequestFailed`, `NotFound`, `Timeout`, or `InvalidResponse`.

The local control tower uses `HttpListener.GetContextAsync` and processes received requests asynchronously. Response delays vary by endpoint and include random variation, so the difference between sequential and concurrent calls can be observed without using timing as a test oracle.

The concurrent client flow starts the route, weather, and restriction calls before awaiting `Task.WhenAll`. The sequential flow awaits each call before starting the next. Both use the same mapping to the final configuration.

## 4. When would I choose Task/TCS over pure async/await?

I would choose `TaskCompletionSource` when completion of a `Task` must be controlled by an event or callback that does not already have a natural async API. A classic example is adapting an event-based API to a `Task`, or exposing explicit completion signaling between components.

For ordinary sequential async logic, independent async operations, and `Task.WhenAll`, pure async/await normally provides less code and clearer control flow. TCS should therefore have a concrete role in the design rather than being used simply because it is possible.

## 5. Two concrete problems caused by blocking in asynchronous methods

First, `.Result`, `.Wait()`, or `GetAwaiter().GetResult()` can block a thread when it should instead be available for other work. In environments with limited thread-pool capacity, this can lead to poor throughput and, in some synchronization models, deadlocks.

Second, blocking around I/O can keep resources unnecessarily occupied for longer. A synchronously waiting HTTP call can tie up a thread-pool thread while the network takes time to respond. With multiple concurrent calls, this can cause queuing, lower responsiveness, and poorer scalability.

## Part D — Concrete learning point

The control tower actually changes the simulator's configuration. For `Alpha`, it returns a route of 3 checkpoints, `storm` weather adds 500 ms, and an active restriction of 2 checkpoints reduces the final route to 2. An original drone configuration with a 100 ms delay therefore becomes 600 ms.

Sequential and concurrent HTTP execution should produce the same final configuration for the same responses, but the independent calls can overlap in the concurrent variant. Exact timing varies with the machine and network/listener conditions and is therefore treated as an observation, not proof of correctness.

## Before submission

This reflection describes the planned and implemented model. It should be supplemented with the concrete observations from an actual run of Part A without `Join` and Part D with sequential versus concurrent execution, especially if measured times are to be reported.
