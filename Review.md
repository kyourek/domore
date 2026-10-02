# Domore.Logs review

Reviewed on 2026-10-02 at commit `830f962c92523328345a6424a4fb4f3fc0477875`.

Scope: all production files in `source/Domore.Logs`, its imported `Domore.Sharing` sources, the logging tests and sample, and the adjacent `Domore.Logs.Conf` integration. Issues 22–23 belong to that companion project. After the review, issues 1–7 were fixed and covered by regression tests in [LoggingTest.cs](tests/Domore.Logs.Tests/Logs/LoggingTest.cs).

Severity: **High** means a hang, application failure, lost messages, or destructive behavior under the stated trigger; **Medium** means a reliability or correctness problem; **Low** means a narrower formatting problem. Numbers are stable reference IDs, not a severity ranking.

## Validation

- Domore.Logs built successfully, with no warnings or errors, for all nine declared targets: `net40`, `net45`, `net462`, `net48`, `netstandard2.0`, `netcoreapp3.1`, `net6.0`, `net8.0`, and `net10.0`.
- All 112 existing logging tests passed on each of `net462`, `net8.0`, and `net10.0` (336 passing executions; none skipped).
- The two issue 1 regression tests failed against the old threshold calculation, then both passed against the fix on `net10.0`.
- The issue 2 callback-completion regression test failed against the old implementation, then passed against the fix on `net10.0`.
- The issue 3 completion-failure regression test failed against the old implementation because a later service was skipped, the next session lost messages, and retry failed. It passes with the fix on `net10.0`.
- The issue 4 shutdown-window regression test failed against the old implementation because a subscription accepted during completion was cleared before the next session. It passes with the fix on `net10.0`.
- Both issue 5 regression tests failed against the old delivery loop: one used a throwing `Log()` callback and one a service type resolution failure. Both pass with per-service exception handling.
- The issue 6 event-handler regression test failed against the old implementation because the throwing handler propagated and blocked later handlers and delivery. It passes with per-handler exception isolation.
- Both issue 7 subscription-cleanup regression tests failed against the old implementation: unsubscribe left handlers attached, and clear did not detach them. They pass with idempotent cleanup. The complete logging suite passes all 122 tests on `net462`, `net8.0`, and `net10.0`; the library builds without warnings or errors for all nine declared targets.
- A temporary .NET 10 harness compiled the unchanged logging and shared sources and confirmed 22 targeted checks. Two further checks used the built configuration assemblies. These covered the report's reproduced failures, including controlled cache/shutdown interleavings and a worker shutdown hang isolated in a child process.
- Issue 21 is established by inspection of the queue implementations; an out-of-memory stress test was not performed. The other findings have executable reproductions. Some reproductions used internal types or reflection to isolate the failing path rather than relying on a scheduling race.
- Runtime reproductions were on Windows. The library was built for all declared targets and the tests ran on `net462`, `net8.0`, and `net10.0`; Unix runtime behavior and test execution on `net40`, `net45`, and `net48` were not independently exercised. Passing baseline tests do not cover the edge cases below.

## Findings

### 1. High — Service enablement does not use each service's effective threshold

**Status: Fixed.** The implementation is in [LogServiceCollection.cs](source/Domore.Logs/Logs/LogServiceCollection.cs). The two regression tests in [LoggingTest.cs](tests/Domore.Logs.Tests/Logs/LoggingTest.cs) failed against the old calculation and passed after the fix on `net10.0`.

**Location:** [LogServiceCollection.cs](source/Domore.Logs/Logs/LogServiceCollection.cs), lines 23–58 and 86–94; [LogServiceProxy.cs](source/Domore.Logs/Logs/LogServiceProxy.cs), lines 74–80.

Before the fix, the aggregate type threshold considered only explicit type overrides, whereas delivery used each service's override **or its default**. With service A overriding type T to `Warn` and service B inheriting default `Debug`, `Enabled(Info)` returned false even though B would receive an Info message. The recommended `if (log.Info()) log.Info(...)` pattern therefore dropped valid messages. Conversely, a sole service with default `Debug` and a T override of `None` reported Info enabled but delivered nothing, because the aggregate removed the `None` entry and fell back to the default. Both cases were reproduced.

**Fix:** Aggregate each service's `Config[type].Threshold ?? Config.Default.Threshold`. Retain a type entry with a `None` threshold when a type override exists but every service is effectively disabled, preventing fallback to the aggregate default. Clearing the override restores default fallback.

### 2. High — Completing logging from a service callback deadlocks the worker

**Status: Fixed.** When completion is requested on the service worker thread, [Logging.cs](source/Domore.Logs/Logs/Logging.cs) queues the regular completion operation to the thread pool and returns, allowing the callback to finish before the worker is joined. Worker-thread detection is exposed through [BackgroundQueue.cs](shared/Domore.Sharing/Threading/BackgroundQueue.cs) and the logging manager.

**Regression coverage:** [LoggingTest.cs](tests/Domore.Logs.Tests/Logs/LoggingTest.cs) verifies that `Logging.Complete()` returns from a service callback and that service completion follows. It failed on the old code after the guarded timeout, then passed with the fix. The full .NET 10 suite now passes all 122 tests.

**Location:** [BackgroundQueue.cs](shared/Domore.Sharing/Threading/BackgroundQueue.cs), lines 8–16 and 46–56; [Logging.cs](source/Domore.Logs/Logs/Logging.cs), lines 66–79 and 181–207; [LogServiceCollection.cs](source/Domore.Logs/Logs/LogServiceCollection.cs), lines 14–16; [LogManager.cs](source/Domore.Logs/Logs/LogManager.cs), lines 19–21.

Before the fix, `ILogService.Log` ran on the background worker and a call to `Logging.Complete()` from that callback called `Thread.Join()` on the executing worker itself. The worker could not return from its callback to exit. A child-process reproduction entered completion and never reached the return marker; the parent terminated it after a timeout.

**Fix:** Completion requested from a service callback is deferred until the callback returns; the normal completion path then drains queued work, joins the worker, and completes services.

### 3. High — A completion exception leaves logging attached to a disposed manager

**Status: Fixed.** [LogServiceCollection.cs](source/Domore.Logs/Logs/LogServiceCollection.cs) now attempts every service completion; [LogManager.cs](source/Domore.Logs/Logs/LogManager.cs) continues through subscription completion and clearing; [Logging.cs](source/Domore.Logs/Logs/Logging.cs) retires the manager in a `finally` block. Failures are collected and reported after cleanup.

**Regression coverage:** [LoggingTest.cs](tests/Domore.Logs.Tests/Logs/LoggingTest.cs) verifies that a failing service does not prevent a healthy service from completing and that a subsequent logging session delivers messages. The test failed against the old implementation, then passed with the fix on `net10.0`.

**Location:** [Logging.cs](source/Domore.Logs/Logs/Logging.cs), lines 181–207; [LogServiceCollection.cs](source/Domore.Logs/Logs/LogServiceCollection.cs), lines 113–129; [LogSubscriptionCollection.cs](source/Domore.Logs/Logs/LogSubscriptionCollection.cs), lines 37–54; [LogManager.cs](source/Domore.Logs/Logs/LogManager.cs), lines 85–109.

Before the fix, if one custom service's `Complete()` threw, later services were not completed and subscription cleanup was skipped. Although the manager's queue was disposed, the assignment clearing `Instance.Manager` was never executed. Subsequent enabled log calls were silently discarded by the disposed queue, and retrying completion threw `ObjectDisposedException`. The skipped completion, dropped later message, and failed retry were reproduced.

**Fix:** Attempt every service and subscription completion independently, always clear subscriptions, and retire the manager even when a completion step fails. Accumulate errors and report them after cleanup so callers still learn that completion was unsuccessful.

### 4. High — Shutdown accepts new work into the manager it is retiring

**Status: Fixed.** [Logging.cs](source/Domore.Logs/Logs/Logging.cs) now leases the active manager for each logging and subscription operation. Completion atomically detaches the active manager, waits for previously accepted operations to finish, then drains and retires it. New operations create or use the next manager. Calls from a retiring service worker remain recognized so the issue 2 deadlock protection is preserved.

**Regression coverage:** [LoggingTest.cs](tests/Domore.Logs.Tests/Logs/LoggingTest.cs) pauses service completion, subscribes and logs during the shutdown window, then verifies that the subscription is still registered and receives messages in the next session. The test failed against the old implementation and passed with the fix on `net10.0`. The existing callback-completion regression also passes. The full .NET 10 suite passes all 122 tests.

**Location:** [Logging.cs](source/Domore.Logs/Logs/Logging.cs), lines 12–101 and 106–207; [LogManager.cs](source/Domore.Logs/Logs/LogManager.cs), lines 26–32; [BackgroundQueue.cs](shared/Domore.Sharing/Threading/BackgroundQueue.cs), lines 70–85.

Before the fix, completion was serialized only against other completion calls. The old manager remained globally accessible while its queue had stopped accepting work and its services were completing. A service paused in `Complete()`, `Logging.Subscribe()` concurrently returned true, and completion then cleared that newly accepted subscription. The next manager had no subscriber. Log calls in the same window could also reach a queue that silently rejected additions.

**Fix:** Lease manager access and use one synchronized handoff to detach the old manager before its queue is completed. Work accepted before the handoff finishes against the old manager; calls after it use the next manager.

### 5. High — One failing service prevents delivery to the remaining services

**Status: Fixed.** [LogServiceCollection.cs](source/Domore.Logs/Logs/LogServiceCollection.cs) now catches and reports failures around each service's complete log path, then continues with the remaining services.

**Regression coverage:** [LoggingTest.cs](tests/Domore.Logs.Tests/Logs/LoggingTest.cs) verifies delivery continues after a service's `Log()` throws and after service type resolution throws during initialization. Both tests failed against the old delivery loop and passed with the fix on `net10.0`. The full suite passes all 122 tests on `net462`, `net8.0`, and `net10.0`.

**Location:** [LogServiceCollection.cs](source/Domore.Logs/Logs/LogServiceCollection.cs), lines 98–112; [LogServiceProxy.cs](source/Domore.Logs/Logs/LogServiceProxy.cs), lines 15–24 and 64–82; [BackgroundQueue.cs](shared/Domore.Sharing/Threading/BackgroundQueue.cs), lines 34–41.

Before the fix, exception handling surrounded the entire queued action rather than each service invocation. If the first service threw from `Log()` or while its type was being resolved, iteration stopped and healthy services later in the collection missed that entry. The worker continued with later actions, so repeated failures could continually starve those services. Both cases were reproduced.

**Fix:** Catch failures around each proxy's `Log()` call, which includes formatting and lazy service initialization. Report the exception and continue iterating so one service cannot block delivery to the others.

### 6. Medium — A throwing log-event handler interrupts all later delivery

**Status: Fixed.** [LogManager.cs](source/Domore.Logs/Logs/LogManager.cs) now snapshots the event handler delegate and invokes each subscribed handler independently. Exceptions are reported through `Logging.Notify`, while subsequent handlers, subscriptions, and services still receive the log.

**Regression coverage:** [LoggingTest.cs](tests/Domore.Logs.Tests/Logs/LoggingTest.cs) verifies the logging call returns, a later event handler runs, and a subscription and service both receive the entry. The test failed against the old code and passed with the fix on `net10.0`. The full suite passes all 122 tests on `net462`, `net8.0`, and `net10.0`.

**Location:** [LogManager.cs](source/Domore.Logs/Logs/LogManager.cs), lines 57–82.

Before the fix, `LogEvent?.Invoke(...)` ran before subscriptions and services without exception isolation. A throwing `Logging.Event` handler propagated into the application's logging call, prevented later event handlers from running, and skipped the subscription and service paths for that message.

**Fix:** Invoke each handler from a snapshot, catch and report failures individually, then continue normal delivery.

### 7. Medium — Unsubscribe leaves the subscription's threshold handler attached

**Status: Fixed.** [LogSubscriptionCollection.cs](source/Domore.Logs/Logs/LogSubscriptionCollection.cs) now completes proxies on removal and clearing. [LogSubscriptionProxy.cs](source/Domore.Logs/Logs/LogSubscriptionProxy.cs) makes detachment idempotent.

**Regression coverage:** [LoggingTest.cs](tests/Domore.Logs.Tests/Logs/LoggingTest.cs) verifies handler counts across repeated add/remove cycles, clear, and complete-then-clear, including that each proxy detaches only once. Both tests failed against the old collection and passed with the fix on `net10.0`.

**Location:** [LogSubscriptionCollection.cs](source/Domore.Logs/Logs/LogSubscriptionCollection.cs), lines 73–117; [LogSubscriptionProxy.cs](source/Domore.Logs/Logs/LogSubscriptionProxy.cs), lines 7–63.

Before the fix, removing a subscription detached the collection from the proxy but never called the proxy's `Complete()` to detach it from `Agent.ThresholdChanged`. A long-lived subscription retained the removed proxy and its cached types; repeated subscribe/unsubscribe cycles accumulated handlers. `Clear()` also relied on callers having separately completed every proxy.

**Fix:** Complete each proxy when removing or clearing it. Make proxy completion idempotent so the existing complete-then-clear path removes the agent handler only once, and continue cleanup for other proxies if one removal fails.

### 8. High — Subscription callbacks can invalidate live collection enumeration

**Location:** [LogSubscriptionCollection.cs](source/Domore.Logs/Logs/LogSubscriptionCollection.cs), lines 23–29 and 133–143; [LogServiceCollection.cs](source/Domore.Logs/Logs/LogServiceCollection.cs), lines 98–129.

Subscription delivery enumerates the live dictionary while calling user code under its lock. Locks are reentrant: a `Receive()` callback can call `Logging.Subscribe()` and add another entry during that enumeration. The next enumerator step throws `InvalidOperationException` outside the proxy's callback catch, interrupting the logging call and later delivery. This was reproduced. Service delivery/finalization similarly invokes external callbacks while enumerating the live service dictionary; reentrant configuration that adds a service has the same structural risk.

**Fix plan:** Snapshot the relevant proxies under the collection lock, then invoke callbacks outside that lock. Apply the same approach to threshold callbacks and service delivery/finalization, while preserving lifecycle coordination. Test callbacks that register new subscriptions/services and coordinate with another thread.

### 9. Medium — A threshold change can be overwritten by an in-flight cache fill

**Location:** [LogSubscriptionProxy.cs](source/Domore.Logs/Logs/LogSubscriptionProxy.cs), lines 15–17 and 22–34.

`ThresholdChanged` clears the cache, but an already executing `GetOrAdd` factory can subsequently insert the old threshold into the cleared cache. The old value then persists until another change event. A gated reproduction paused a query returning `Warn`, changed the subscription to `Debug` and raised its event, then released the old query; subsequent queries still returned `Warn`. Delivery and enablement can therefore continue using obsolete filtering.

**Fix plan:** Swap the cache instance atomically on invalidation so old queries populate only the retired cache, or use a generation check and retry stale computations. Coordinate the aggregate cache as well. Add a deterministic in-flight-query/change-event test.

### 10. High — Nonpositive log batch limits can spin or fail outside the timer catch

**Location:** [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs), lines 166–200 and 212.

`LogCountLimit` accepts any integer. At zero, a timer callback with queued data repeatedly allocates an empty list and consumes no entries, producing a busy loop. A negative value throws when constructing the list, before the callback's exception handler. On the actual timer path this leaves an unhandled callback exception with application-failure consequences. The zero-limit loop and negative-capacity exception were reproduced by invoking the unchanged callback directly; the loop was released by correcting the limit.

**Fix plan:** Validate `LogCountLimit > 0` in its setter and retain a defensive check in the worker. Cover the whole timer callback with safe error handling. Reject invalid configuration before it can alter a running writer; test zero, negative, and live updates.

### 11. High — Invalid flush intervals can strand the file queue and lose the first entry

**Location:** [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs), lines 200–207, 216, and 282–291.

`FlushInterval` is cast from a `TimeSpan` to an integer timer delay without validation. For example, `-2 ms` causes timer construction to throw after `Started` has already been set, and before the first entry is enqueued. Later entries queue without another startup attempt, even after the interval is corrected. This was reproduced. A delay of `-1 ms` disables the timer entirely, and sufficiently large intervals exceed the integer delay representation. Restarting the timer from the callback is also outside its catch.

**Fix plan:** Validate a documented positive, representable interval; set `Started` only after successful startup; and make startup/restart failures recoverable. Test invalid negative values, conversion bounds, disabled-timer values, and correction after a failed startup.

### 12. High — Changing a service's type abandons the previous service

**Location:** [LogServiceProxy.cs](source/Domore.Logs/Logs/LogServiceProxy.cs), lines 49–57 and 84–85.

Runtime reconfiguration sets `_Service = null` without completing the previous instance. A buffered file service or custom service may still have pending work, timers, or other resources, but later `Logging.Complete()` can only reach the replacement. Exit-time flushing is no longer guaranteed for the old instance. A recording service confirmed that changing `Type` never called its completion method.

**Fix plan:** Serialize service replacement with delivery, retain the old instance until its pending work has drained, and complete it exactly once. Coordinate type changes and service retrieval so logging cannot observe a partially replaced service. Test replacement of a buffered service followed immediately by shutdown.

### 13. Medium — Archive age calculations mix local time and UTC

**Location:** [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs), lines 36–39, 81, and 110–120.

Archive names are generated from `DateTime.Now`, then parsed as `DateTimeKind.Utc` and subtracted from `DateTime.UtcNow`. The timezone offset becomes part of the calculated age. In this environment a newly generated archive parsed as approximately five hours old. Archives can be deleted too early in timezones behind UTC or retained too long in timezones ahead of UTC; daylight-saving changes add ambiguity.

**Fix plan:** Generate and parse archive timestamps consistently in UTC. Decide how existing names written in local time will be handled during migration. Test retention boundaries with positive and negative timezone offsets and a daylight-saving transition.

### 14. Medium — An invalid archive date aborts retention for otherwise valid files

**Location:** [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs), lines 42–81 and 113–118.

The parser checks numeric components but constructs `DateTime` without checking their ranges. A matching file such as `app_20261301-000000-000.log` throws because month 13 is invalid. Materializing the retention list then fails, so neither age nor total-size cleanup reaches valid archives. A reproduction included that file and an expired archive; rotation threw and the expired archive remained.

**Fix plan:** Parse the timestamp using an exact invariant format and a nonthrowing parse, returning null for invalid names. Validate the expected prefix and extension explicitly. Test invalid months, days, leap dates, and times alongside valid expired archives.

### 15. Medium — The final flush bypasses rotation and retention

**Location:** [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs), lines 188–191 versus 258–277.

Timer flushes call `Log(lines)` and `Rotate()`, but `Complete()` only calls `Log(lines)`. A short-lived process that completes before the first timer callback never applies the rotation/retention path to its pending batch. A reproduction with both size limits set to one byte left a 21-byte active file and no rotation. Existing expired archives are likewise not cleaned by that final flush.

**Fix plan:** Share the flush-and-rotation path between timer and completion, preserving its locking and error handling. Test completion before the first timer tick with a tiny size limit and pre-existing expired archives.

### 16. High — Path formatting strips UNC and rooted-path prefixes

**Location:** [PathFormatter.cs](shared/Domore.Sharing/IO/PathFormatter.cs), lines 34–45 and 70; [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs), lines 27–34.

Splitting on separators and removing empty components discards leading separators. Only drive-letter roots are reconstructed. On Windows, `\\server\share\logs` becomes `server\share\logs`, and `\logs\app` becomes `logs\app`. Both transformations were reproduced. The file writer resolves those as relative paths, sending logs to a different location instead of the configured share or rooted directory. The same algorithm drops the initial separator of Unix absolute paths; that platform case was identified by inspection rather than runtime execution.

**Fix plan:** Preserve `Path.GetPathRoot(path)` independently from formatted components, including UNC and extended Windows roots, and recombine without changing rootedness. Test UNC, drive, drive-relative, current-drive-rooted, and Unix paths on their respective platforms.

### 17. Medium — Absolute file names rotate into a different directory

**Location:** [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs), lines 27–28, 98–112, and 133–144.

An absolute `Name` overrides `Directory` when the active path is combined, but rotation and archive discovery always use `DirectoryInfo.FullName`. With `Directory=A` and `Name=B/app.log`, the active log is written in B and the archive is moved into A. This was reproduced. The destination directory and archive retention scope therefore differ from the active log's location. Relative names containing subdirectories also need an explicit policy because creation currently prepares only the configured base directory.

**Fix plan:** Resolve the complete active path once and derive creation, rotation, and retention directories from that path. Alternatively, explicitly restrict `Name` to a file name and reject path-bearing values. Preserve supported absolute-name behavior deliberately; add differing-base-directory coverage.

### 18. Low — Repeated path tokens in a component are only partly replaced

**Location:** [PathFormatter.cs](shared/Domore.Sharing/IO/PathFormatter.cs), lines 52–67.

The replacement loop calls `IndexOf` once per token per path component. For `{Thread.ManagedThreadId}-{thread.managedthreadid}.log`, only the first occurrence is replaced and the second remains literal. This was reproduced with mixed token casing, producing a file name that differs from the configured template's intended result.

**Fix plan:** Replace every occurrence case-insensitively, evaluating and sanitizing each token value once. Advance through the original template so inserted text is not reinterpreted. Test repeated tokens in one file name and one directory component.

### 19. Medium — Observers can mutate the log data received by other observers

**Location:** [LogEntry.cs](source/Domore.Logs/Logs/LogEntry.cs), lines 46–53 and 67; [LogManager.cs](source/Domore.Logs/Logs/LogManager.cs), lines 57–82.

`ILogEntry.LogList` exposes the actual mutable `string[]` through an `IEnumerable<string>`. An event handler or subscription can cast it back to an array and change messages before later observers or the asynchronous services consume them. A reproduction logged `original`, changed the array in an event handler, and delivered `changed` to the subscription. Retaining and editing the array can also affect queued service output later.

**Fix plan:** Keep the formatted array private and expose a read-only view that does not reveal it; copy incoming arrays if ownership is not exclusive. Test attempted mutation and consistent content across events, subscriptions, and queued services.

### 20. High — A missing-file check can lead to truncating another writer's data

**Location:** [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs), lines 139–144.

The file writer calls `FileInfo.Create()` if the cached `Exists` value is false, then appends. `Create()` truncates a file if another writer creates it between the existence check and creation, or if that cached result has become stale. A controlled reproduction cached a missing-file result, wrote existing content through another writer, and then flushed the logger: the existing content disappeared and only the new log line remained.

**Fix plan:** Remove the separate check/create sequence and append using a create-if-missing append operation, such as the existing `File.AppendAllLines` call. Keep retries appropriate for sharing failures. Test a second writer creating the file after the logger's initial missing-file observation.

### 21. Medium — Both logging queues can grow without a bound

**Location:** [BackgroundQueue.cs](shared/Domore.Sharing/Threading/BackgroundQueue.cs), lines 11 and 70–73; [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs), lines 13 and 291; [LogManager.cs](source/Domore.Logs/Logs/LogManager.cs), lines 57–82.

The service queue uses an unbounded `BlockingCollection<Action>`, and the file writer uses an unbounded `ConcurrentQueue<string>`. A slow or blocked service holds up the single service worker while producers continue retaining formatted entries. Slow file I/O can independently accumulate strings in the file queue. `LogCountLimit` limits batch size rather than backlog. Entries below every configured service threshold are also queued because filtering occurs in service delivery. Sustained producer/consumer imbalance has no memory limit or overload policy. This is a source-confirmed capacity issue; no destructive memory stress test was run.

**Fix plan:** Define configurable queue capacity, overload behavior, and observable drop/backlog counters. Apply filtering before enqueue where it preserves the intended configuration semantics, and account for large message sizes as well as item counts. Test overload using a gated slow service and verify the documented policy.

### 22. Medium — Failed configuration-file setup permanently reports success state

**Location:** [Log.cs](source/Domore.Logs.Conf/Logs/Log.cs), lines 7–20.

`Log.Conf.Configure()` publishes `File` before `File.Configure(watch: true)` succeeds. If setup throws, `Configured` still returns true and every later call returns false. A reproduction used a path in a nonexistent directory: watcher setup threw, the configured flag became true, and retrying with a valid file returned false. Failed setup can also leave partially initialized resources retained by the static field.

**Fix plan:** Construct and configure a local candidate, publish it only after success, and dispose it on failure. Keep the one-time initialization lock but allow retry after a failed attempt. Test watcher/setup failures followed by a valid retry.

### 23. Medium — The configuration watcher keeps targeting a retired manager

**Location:** [LogConfFile.cs](source/Domore.Logs.Conf/Logs/LogConfFile.cs), lines 14–19; [Logging.cs](source/Domore.Logs/Logs/Logging.cs), lines 21–28, 157–158, and 181–207; [Log.cs](source/Domore.Logs.Conf/Logs/Log.cs), lines 5–20.

The watched `ConfFile` captures one `Logging.Config` object at construction, including its specific manager. `Logging.Complete()` disposes and retires that manager, but the static watcher remains configured. If logging is subsequently restarted, configuration-file changes still update the old manager, while the new manager remains unconfigured; calling `Log.Conf.Configure()` again returns false. An integration reproduction verified that a changed file enabled Debug on the captured old manager while Debug remained disabled on the current one.

**Fix plan:** Define watcher lifecycle alongside logging lifecycle. Either dispose/reset it on completion and support explicit reconfiguration, or make each reload acquire the current manager and apply initial configuration to a new session. Test configuration, completion, restart, and a subsequent watched-file change.
