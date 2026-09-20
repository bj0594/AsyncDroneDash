# Reflection

## 1. What happened when Join was removed in Part A?

With `Join`, the main thread waits until the drone threads have finished, so `All drones have finished.` is printed only after both drones report completion. In my run, the joined flight showed interleaved Alpha and Beta checkpoint output, making concurrent execution visible while the final completion message still appeared after both flights had completed.

In the no-`Join` run, the main thread continued immediately after starting the drone threads. The message explaining that the main thread continues appeared immediately after the two start events. I then returned to the menu, so I did not use the later background output as a correctness observation. The output order between the drone threads is not deterministic because thread scheduling is not deterministic.

The console should therefore be treated as an observation surface, not as a synchronization mechanism. Multiple threads can write to it concurrently, so the order of lines is not a reliable correctness contract.

## 2. Thread/Join, Task/TCS, and async/await

`Thread` gives the most direct control over execution threads. That makes concurrency easy to see, but it also requires explicit thread creation, starting, and joining. Exceptions raised on a worker thread do not become a normal return path to the calling thread, so error handling has to be designed separately.

`Task` is a higher-level abstraction for asynchronous work. `TaskCompletionSource` adds explicit control over when a task is completed or faulted. In this project, each drone gets its own TCS and `Task.WhenAll` coordinates the flights. This makes completion and failure signalling explicit, but it also adds boilerplate that async/await does not need for the same basic scenario.

The failure demo also makes `Task.Exception` visible as an `AggregateException` containing the underlying `InvalidOperationException`.

`async`/`await` expresses the flight flow more directly: report progress, await the delay, and continue. Multiple flights can overlap without manual thread management or explicit TCS completion calls. `await Task.WhenAll` provides the overall coordination boundary with less code and a clearer control flow.

The main maintenance difference I observed is that the async/await version separates the flight sequence from the mechanics of task completion more cleanly. Task/TCS remains useful when completion must be controlled explicitly by an external event, callback, or adapter boundary.

## 3. What was challenging about asynchronous HTTP?

The main challenge was keeping the entire request path asynchronous while also converting several failure modes into a small, predictable error vocabulary. The client uses one reusable `HttpClient`, asynchronous HTTP calls and asynchronous response reading. Failures are translated into `ControlTowerException` categories such as `RequestFailed`, `NotFound`, `Timeout`, and `InvalidResponse`.

The local control tower also had to process requests asynchronously and introduce variable response time without making timing part of correctness. The concurrent orchestration starts the independent route, weather, and restriction requests before awaiting them together; the sequential version waits for each request before starting the next.

This made the effect of concurrency visible without relying on exact elapsed times as a test oracle.

## 4. When would I choose Task/TCS over pure async/await?

I would use `TaskCompletionSource` when a completion or failure signal comes from something that is not already represented by a natural `Task`-based API. Examples include adapting an event or callback to a `Task`, or exposing explicit completion signalling between components.

For ordinary asynchronous workflows, especially when the operations already return tasks and can be composed with `Task.WhenAll`, async/await is usually simpler and easier to maintain.

## 5. Two concrete problems caused by blocking in asynchronous methods

First, `.Result`, `.Wait()`, and similar blocking calls occupy a thread while the asynchronous operation is waiting. In environments with limited thread-pool capacity, repeated blocking can reduce throughput and can contribute to deadlock problems in synchronization-sensitive environments.

Second, blocking around I/O reduces the benefit of asynchronous execution. A thread can remain occupied while an HTTP request or other I/O operation waits for an external resource, which can increase queuing and reduce responsiveness as concurrent work grows.

## Part D — Concrete learning point

The control tower changed the actual simulation configuration. In the verified run, `Alpha` received a route of 3 checkpoints. `storm` added 500 ms to the original 100 ms delay, and the active restriction reduced the route to 2 checkpoints. The resulting configuration was therefore `MaxCheckpoints = 2` and `DelayMs = 600`.

The sequential HTTP run took about 1107 ms, while the concurrent run took about 383 ms in that run. The important observation was not the exact numbers, but that the independent requests overlapped in the concurrent version and produced the same final functional configuration. Exact elapsed time depends on the local environment and is therefore treated as an observation rather than a correctness requirement.
