# Reflection

## 1. What happened when Join was removed in Part A?

`Join` makes the main thread wait for each drone thread to complete before `RunWithJoin` returns. In the no-`Join` variant, the main thread continues immediately while the drone threads are still working. This means the main-thread message can appear before all checkpoint messages have been written. The order between drone threads is also non-deterministic.

This also shows why `Console` is not a good synchronization mechanism. Multiple threads can write at the same time, and which event appears first depends on thread scheduling.

## 2. Thread/Join, Task/TCS, and async/await

`Thread` provides direct control over threads and makes concurrent execution easy to see, but the model requires explicit creation, starting, and `Join` of the threads. A thread also represents a dedicated execution resource for the lifetime of the flight. Error handling must be organized separately because an exception inside a thread is not automatically propagated to the main thread.

`Task` provides a higher-level abstraction for completion, while `TaskCompletionSource` makes it possible to control when each operation is marked as completed or faulted. In this implementation, the drone operations are started with `Task.Run`, so the work is scheduled through the thread pool rather than requiring one explicitly created `Thread` per drone. One TCS was used per drone, and `Task.WhenAll` was used to coordinate them. The model clearly demonstrates explicit signaling and task composition, but it requires more code than async/await because completion and errors must be controlled manually.

An important observation is that `Task.Exception` is an `AggregateException`. This makes task state and the underlying error explicitly observable.

`async`/`await` expresses the same type of coordination with less boilerplate. The flight itself can read like a sequence — report a checkpoint, wait asynchronously, continue — while multiple flights can still overlap. `await Task.WhenAll` makes overall coordination readable without manual TCS management. Unlike the Thread-based implementation, an async flight waiting in `Task.Delay` does not need to keep a thread occupied just to represent that waiting period.

This makes the resource model different as well as the syntax. `Thread` gives direct control over dedicated execution threads. The Task/TCS implementation abstracts execution and, in this project, uses thread-pool scheduling. The async/await implementation is the most suitable of the three for this waiting-heavy simulation because the delays are asynchronous and do not require a thread to remain occupied while the delay is in progress.

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

The control tower actually changes the simulator's configuration. For `Alpha`, it returned a route of 3 checkpoints, `storm` weather added 500 ms, and an active restriction of 2 checkpoints reduced the final route to 2. An original drone configuration with a 100 ms delay therefore became 600 ms.

In the observed run, the sequential configuration took approximately 1107 ms, while the concurrent configuration took approximately 383 ms. The important observation was not the exact values, but that the independent HTTP calls overlapped in the concurrent version while producing the same final functional configuration.

The variable response delays also made the difference visible in the console logs. The request start messages for route, weather, and restrictions could appear before the corresponding requests had completed, demonstrating that the concurrent orchestration was not simply executing the calls one after another.

These timings are environment-dependent and are therefore treated as runtime observations rather than correctness criteria.

## Conclusion

The three execution models show increasing abstraction over the same basic problem. `Thread` gives the clearest direct view of thread creation and joining, Task/TCS demonstrates explicit task completion and error signaling, and async/await provides the clearest expression of asynchronous control flow.

The project also showed that the most useful abstraction depends on the problem being solved. Explicit Thread and TCS handling are valuable for demonstrating how the underlying mechanisms work, while async/await is easier to maintain when the workflow is naturally asynchronous and dominated by waiting.

The Control Tower portion reinforced the same lesson at the HTTP level: independent I/O operations can be started together and composed with `Task.WhenAll`, allowing the program to make progress without unnecessarily serializing work.