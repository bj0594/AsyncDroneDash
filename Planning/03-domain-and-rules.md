# Async Drone Dash — Domain and Rules

## Domain overview

Async Drone Dash models multiple delivery drones progressing through simple checkpoint-based routes.

The core domain is intentionally small because the project primarily demonstrates concurrent and asynchronous execution.

The core domain does not model physical movement, geography, fuel, battery, packages, customers, or route optimization.

---

## DroneModel

| Property | Type | Meaning |
|---|---|---|
| `Name` | `string` | Identifies the drone |
| `MaxCheckpoints` | `int` | Highest checkpoint the drone should reach |
| `DelayMs` | `int` | Delay between checkpoint steps in milliseconds |

No additional properties are required by the MVP.

---

## FlightEvent

A flight produces observable events:

- `Started`
- `CheckpointReached`
- `Completed`
- `Faulted`

The observable event shape is:

    DroneName  : string
    Type       : FlightEventType
    Checkpoint : int?
    Exception  : Exception?

Every `FlightEvent` identifies the drone that produced it through `DroneName`.

Event data rules:

- `Started` has no checkpoint and no exception.
- `CheckpointReached` contains the reached checkpoint number and no exception.
- `Completed` has no checkpoint and no exception.
- `Faulted` contains the relevant exception and no checkpoint requirement.

A `CheckpointReached` event contains the checkpoint number.

The event model provides a deterministic observation boundary for tests and orchestration. Core flight logic does not write directly to the console.

---

## Flight behaviour

A valid drone flight progresses from checkpoint `0` through `MaxCheckpoints`, inclusive.

`MaxCheckpoints = 3` → `0 → 1 → 2 → 3`

`DelayMs` applies between consecutive checkpoint steps.

There is no required delay before checkpoint `0` or after the final checkpoint.

---

## Validation rules

| Situation | Contract |
|---|---|
| `Name == null` | `ArgumentException` |
| `Name == ""` | `ArgumentException` |
| whitespace-only `Name` | `ArgumentException` |
| `MaxCheckpoints < 0` | `ArgumentOutOfRangeException` |
| `MaxCheckpoints == 0` | Valid |
| `DelayMs < 0` | `ArgumentOutOfRangeException` |
| `DelayMs == 0` | Valid |
| `DelayMs > 0` | Valid |

Validation is enforced at the flight boundary.

---

## Contract ownership

This document is the source of truth for domain data, validation rules, endpoint contracts, response shapes, error categories, and simulation mapping.

Observable behaviour IDs (`B*` and `VB*`) are defined in `05-behaviour-design.md`. Requirement-to-verification traceability is defined in `04-design-and-traceability.md` and `AsyncDroneDash.Tests/TestPlan.md`. This separation prevents the same contract from being rewritten in several planning documents.

# Part D — Optional project target

Part D uses the local `HttpListener` alternative supplied by the assignment and `HttpClient` for the client.

## Local service

The selected local service prefix is:

`http://localhost:8080/`

The service runs in the same demonstration application process.

The request loop uses asynchronous request handling rather than blocking `GetContext()`.

The project chooses `HttpListener` because the assignment explicitly offers it as an option. It is not treated as a general recommendation for new production HTTP services.

## Endpoints

    GET /route?drone=Navn
    GET /weather
    GET /restrictions

`/restrictions` is the project's selected extension of the minimum local API described by the assignment.

The route handler reads the requested drone name from the request URL/query data. The implementation may inspect `RawUrl` as part of this demonstration because the assignment explicitly calls out that learning point.

The selected project uses a small deterministic route fixture based on drone name:

| Drone name | Base `MaxCheckpoints` |
|---|---:|
| `Alpha` | `3` |
| `Beta` | `5` |
| `Gamma` | `2` |

These values are project-level demo data chosen to make route behaviour deterministic and testable. They are not additional requirements from the assignment.

Names outside the fixture are unknown route drones and return HTTP `404`.

## Route JSON

Successful response:

    {
      "maxCheckpoints": 3
    }

Rules:

- `maxCheckpoints` is required;
- it is an integer;
- it must be `>= 0`;
- it is the base route checkpoint count.

Unknown route drone:

- HTTP `404`;
- client maps this to `ControlTowerException` with kind `NotFound`.

## Weather JSON

Successful response:

    {
      "condition": "storm"
    }

Supported values:

| Condition | Delay adjustment |
|---|---:|
| `clear` | `+0 ms` |
| `wind` | `+250 ms` |
| `storm` | `+500 ms` |

Unknown values are invalid responses.

## Restriction JSON

With active restriction:

    {
      "maxCheckpoints": 2
    }

Without active restriction:

    {
      "maxCheckpoints": null
    }

Rules:

- property is required;
- value is either `null` or a non-negative integer;
- it may reduce the route maximum but never increase it.

## Final simulation configuration

Without restriction:

`FinalMaxCheckpoints = RouteMaxCheckpoints`

With restriction:

`FinalMaxCheckpoints = min(RouteMaxCheckpoints, RestrictionMaxCheckpoints)`

`FinalDelayMs = Drone.DelayMs + Weather.DelayAdjustmentMs`

The final values must satisfy the core validation rules.

## ControlTowerException

The public error categories are:

| Kind | Meaning |
|---|---|
| `RequestFailed` | Non-success response except `404`, or connection-level request failure |
| `NotFound` | Requested route/drone does not exist |
| `Timeout` | Request exceeds configured timeout |
| `InvalidResponse` | Malformed, incomplete, or semantically invalid response |

Public shape:

    ControlTowerException : Exception

    ControlTowerErrorKind Kind { get; }

The exception message explains the failure. The original exception is preserved as `InnerException` where useful.

## Variable response time

The local service deliberately supports varied response times to simulate slow network conditions.

Exact elapsed duration is not a correctness contract.

For automated tests, delays are controlled rather than random. Randomness is reserved for the manual demonstration.

## HTTP client lifetime

`ControlTowerClient` reuses one `HttpClient` for its lifetime.

## Local service lifecycle

The local control-tower listener is stopped and disposed when the Part D demonstration finishes or when the application shuts down. The server must release its listener/resources so the configured localhost port can be reused by a later demonstration.

## Optional features

- cancellation;
- retry/backoff;
- `IAsyncEnumerable`;
- drone registration.