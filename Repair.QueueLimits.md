# Queue limit repair

This change implements the capacity portion of C21 on the `logs3` comparison
baseline `74ba99c`. It leaves the shared `BackgroundQueue` generic and uncapped;
`Conf` and other non-logging users keep their existing behavior.

Each named logging destination now has its own service-dispatch queue. The
built-in `FileLog` has a second, independent queue. Both default to 1,024
pending items and 8 MiB of retained UTF-16 message text. Configure a service
dispatch queue with `log[name].queue item limit` and
`log[name].queue byte limit`; configure a `FileLog` queue with
`log[name].service.queue item limit` and
`log[name].service.queue byte limit`. Every configured limit must be positive.
Changing a limit never removes work already accepted.

Admission rejects the newest message immediately when either limit would be
exceeded, including a single message larger than the byte limit. Accepted work
keeps FIFO order. Dispatch capacity counts each input line as `2 *
string.Length` bytes; the service queue measures its actual queued `FileLog`
string the same way. These totals exclude object overhead and in-flight work.
Formatted delivery strings are generated for delivery rather than cached on a
shared entry, so a fast destination cannot retain an unaccounted formatted copy
inside another destination's pending queue.

`Logging.GetQueueStatus(name)` returns an immutable dispatch snapshot and, when
the current service implements `ILogQueueStatusProvider`, a service-queue
snapshot. Unknown names return `null`; passing a null name throws
`ArgumentNullException`. A status read does not create a proxy or service, and
provider failures are contained. Pending counts and bytes exclude in-flight
callbacks and timer batches. Drop counters are cumulative and saturate at
`long.MaxValue`.

Overload diagnostics are best-effort, asynchronous, and limited to one report
per second across concurrent rejections. A slow, throwing, or recursively
logging stderr writer cannot block queue admission. Each destination drains
independently; retirement closes every destination queue before draining any
of them, then invokes service completion. `FileLog` uses a short admission lock
separate from its I/O/settings lock; completion closes admission, disposes its
timer, waits for any in-flight flush, and flushes all accepted items.

## Red/green evidence

The original shared service queue admitted 1,025 messages behind a gated
in-flight callback, leaving 1,025 pending items against the required 1,024
default. The pre-change default-count probe failed with the same queue depth.

Targeted fault injections then demonstrated that each new assertion detects
the missing behavior:

- Disabling dispatch byte admission made the 8 MiB + 2 byte test fail with one
  pending item, 8,388,610 pending bytes, and zero drops.
- Disabling `FileLog` byte admission made the same test fail with one pending
  item, 8,388,610 pending bytes, and zero drops.
- Disabling dispatch item admission made the default-count test fail at 1,025
  pending items and zero drops.
- Disabling `FileLog` item admission made its default-count test fail at 1,025
  pending items and zero drops.
- Injecting an I/O-lock acquisition before `FileLog` admission made the
  held-I/O-lock rejection test time out while the queue was full.
- The first `FileLogCompletionWaitsForInflightTimerWriteAndConcurrentCompletion`
  draft failed because a second `Complete` returned before the timer's accepted
  in-flight write finished. Completion now always waits on the I/O lock and
  drains remaining accepted work under that lock.
- Removing the callback guard around the optional status-provider getter made
  `OptionalStatusProviderRunsUnderCallbackGuardAndReadDoesNotCreateUnusedService`
  fail: both enablement checks returned true and four nested events escaped.
  Restoring the guard made the focused test pass.

Each temporary fault was restored immediately. The capacity suite passed
19/19 on net462, net8.0, and net10.0. The related replacement-shutdown,
`FileLog`, and formatter fixtures passed 56/56 on each of those targets. The
Release library build passed all nine targets: net40, net45, net462, net48,
netstandard2.0, netcoreapp3.1, net6.0, net8.0, and net10.0, with zero warnings
and errors.
