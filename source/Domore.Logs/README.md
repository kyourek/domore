*Domore.Logs* is a lightweight, simple, and very opinionated logging library.

Use `Logging.For(typeof(MyType))` to obtain an `ILog`. All library-provided
emission methods and enabled checks contain ordinary managed failures across
manager acquisition, formatting, thresholds, delivery, and lease release.
Emissions return normally on failure; enabled checks return `false`. Diagnostics
are best-effort writes to stderr. Explicit configuration and completion APIs
continue to validate arguments and report errors to their callers.

Formatter arguments, event handlers, subscriptions, and services are isolated
so one failure does not unnecessarily prevent other arguments or destinations
from being processed. Public logging from a formatter, event, subscription,
service, factory, or diagnostic callback is suppressed, including child work
that carries its execution context. Enabled checks in that context return
`false`. Unrelated logging continues normally.

Disabled entries are checked before formatting. A null params array produces
one empty line. A custom formatter returning null falls back to normal
formatting. Enumerable arguments expand at most 100 items by default and show
a truncation marker when more items remain. Set the positive
`Logging.EnumerableItemLimit` to change that bound. Date and time tokens use
invariant Gregorian formatting.

Each service dispatch queue and each file queue defaults to 1,024 pending
messages and 8 MiB of retained UTF-16 message text. Both limits must be positive.
At capacity, admission rejects the newest message without waiting for space;
accepted messages keep their order. Lowering a limit preserves accepted work.
Pending totals exclude messages already being processed and object overhead.
Dispatch byte totals measure the formatted argument lines; file byte totals
measure the rendered messages waiting for disk I/O.

For example, built-in logging configuration can set independent dispatch and
file limits:

```text
log[file].type = file
log[file].queue item limit = 1024
log[file].queue byte limit = 8388608
log[file].service.queue item limit = 1024
log[file].service.queue byte limit = 8388608
```

`Logging.GetQueueStatus("file")` returns immutable snapshots in `DispatchQueue`
and, when the service supplies one, `ServiceQueue`. Each snapshot includes
`PendingItemCount`, `PendingMessageBytes`, `DroppedItemCount`, and
`DroppedMessageBytes`, plus the current limits. Drop totals are cumulative for
that queue's lifetime. Unknown destination names return null, and reading status
does not construct an unused service. Overload diagnostics are coalesced,
limited to about one report per second, and written asynchronously to stderr.
File `LogCountLimit` remains the flush batch
size, separate from queue capacity.

`Logging.Complete()` waits for a full flush, including managers already
retiring. `Logging.Complete(timeout)` returns `false` when its single shutdown
deadline expires; safe cleanup continues in the background. Acceptable timeouts
are nonnegative values or `TimeSpan.FromMilliseconds(-1)` for an infinite wait.
Completion detaches the current manager, waits for active leases, drains
accepted entries, completes services and subscriptions, then disposes resources.
Reentrant completion defers the wait and cannot retire a newer logging session.
Normal process exit has a five-second total shutdown budget.

With Domore.Logs.Conf, built-in `ConfigureLogging` and `Log.Conf.Configure`
apply exact `log[service].type` assignments before other expanded configuration
pairs, preserving order within both groups. This includes pairs from included
files and duplicate assignments. Direct generic `Conf.Configure` and third-party
containers retain source order. Built-in logging configuration holds one manager
lease for the whole application; watcher errors go to stderr.

The logging containment contract cannot cover exceptions during argument
evaluation before invocation or unrecoverable runtime termination. Arbitrary
callbacks cannot be safely preempted, and forced termination cannot guarantee
flushing.
