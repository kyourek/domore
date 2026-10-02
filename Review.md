# Domore.Logs review

Reviewed on 2026-10-02 at commit `830f962c92523328345a6424a4fb4f3fc0477875`.

Scope: all production files in `source/Domore.Logs`, its imported `Domore.Sharing` sources, the logging tests and sample, and the adjacent `Domore.Logs.Conf` integration. Issues 22–23 belong to that companion project. After the review, issues 1–16 were fixed and covered by regression tests in [LoggingTest.cs](tests/Domore.Logs.Tests/Logs/LoggingTest.cs), [FileLogTest.cs](tests/Domore.Logs.Tests/Logs/Services/FileLogTest.cs), and [PathFormatterTest.cs](tests/Domore.Logs.Tests/IO/PathFormatterTest.cs).

Severity: **High** means a hang, application failure, lost messages, or destructive behavior under the stated trigger; **Medium** means a reliability or correctness problem; **Low** means a narrower formatting problem. Numbers are stable reference IDs, not a severity ranking.

## Validation

- Domore.Logs built successfully, with no warnings or errors, for all nine declared targets: `net40`, `net45`, `net462`, `net48`, `netstandard2.0`, `netcoreapp3.1`, `net6.0`, `net8.0`, and `net10.0`.
- Each of `net462`, `net8.0`, and `net10.0` ran 150 test cases: 149 passed and the Unix-root case was skipped because these tests ran on Windows (447 passes and three skips total).
- The two issue 1 regression tests failed against the old threshold calculation, then both passed against the fix on `net10.0`.
- The issue 2 callback-completion regression test failed against the old implementation, then passed against the fix on `net10.0`.
- The issue 3 completion-failure regression test failed against the old implementation because a later service was skipped, the next session lost messages, and retry failed. It passes with the fix on `net10.0`.
- The issue 4 shutdown-window regression test failed against the old implementation because a subscription accepted during completion was cleared before the next session. It passes with the fix on `net10.0`.
- Both issue 5 regression tests failed against the old delivery loop: one used a throwing `Log()` callback and one a service type resolution failure. Both pass with per-service exception handling.
- The issue 6 event-handler regression test failed against the old implementation because the throwing handler propagated and blocked later handlers and delivery. It passes with per-handler exception isolation.
- Both issue 7 subscription-cleanup regression tests failed against the old implementation: unsubscribe left handlers attached, and clear did not detach them. They pass with idempotent cleanup.
- All four issue 8 reentrant-callback regression tests failed against the live-dictionary implementation and pass with snapshot iteration. The issue 9 in-flight cache-fill regression test failed against the old invalidation and passes with the cache replacement fix. Issues 10 and 11's invalid limit and interval regression cases failed against the unvalidated properties; all pass with validation and retryable startup. Issue 12's in-flight service replacement regression test failed against the old proxy because the old service was never completed; it passes with serialized replacement and exactly-once completion. Issue 13's timestamp regression test failed because filenames used local clock fields that were parsed as UTC; it passes with local clock fields plus an explicit numeric offset and correct UTC age calculations. The complete logging suite has 150 test cases per target: 149 pass and the Unix-root case is skipped on this Windows runner; the library builds without warnings or errors for all nine declared targets.
- Issue 14's retention regression test failed against the old parser on an invalid legacy date, which threw and prevented deletion of a valid expired archive. It passes with exact invariant parsing and explicit prefix/extension validation; the malformed archives remain untouched while the expired archive is removed.
- Issue 16's current-drive and UNC regressions failed against the root-stripping formatter and pass after root-aware reconstruction. A token-in-UNC-root regression exposed that the first root-preserving change skipped token expansion there; the final implementation formats those components too. The Unix absolute-path test is present but was skipped on this Windows runner.
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

**Regression coverage:** [LoggingTest.cs](tests/Domore.Logs.Tests/Logs/LoggingTest.cs) verifies that `Logging.Complete()` returns from a service callback and that service completion follows. It failed on the old code after the guarded timeout, then passed with the fix. The full .NET 10 suite has 150 test cases: 149 pass and the Unix-root case is skipped on this Windows runner.

**Location:** [BackgroundQueue.cs](shared/Domore.Sharing/Threading/BackgroundQueue.cs), lines 8–16 and 46–56; [Logging.cs](source/Domore.Logs/Logs/Logging.cs), lines 66–79 and 181–207; [LogServiceCollection.cs](source/Domore.Logs/Logs/LogServiceCollection.cs), lines 14–16; [LogManager.cs](source/Domore.Logs/Logs/LogManager.cs), lines 19–21.

Before the fix, `ILogService.Log` ran on the background worker and a call to `Logging.Complete()` from that callback called `Thread.Join()` on the executing worker itself. The worker could not return from its callback to exit. A child-process reproduction entered completion and never reached the return marker; the parent terminated it after a timeout.

**Fix:** Completion requested from a service callback is deferred until the callback returns; the normal completion path then drains queued work, joins the worker, and completes services.

### 3. High — A completion exception leaves logging attached to a disposed manager

**Status: Fixed.** [LogServiceCollection.cs](source/Domore.Logs/Logs/LogServiceCollection.cs) now attempts every service completion; [LogManager.cs](source/Domore.Logs/Logs/LogManager.cs) continues through subscription completion and clearing; [Logging.cs](source/Domore.Logs/Logs/Logging.cs) retires the manager in a `finally` block. Failures are collected and reported after cleanup.

**Regression coverage:** [LoggingTest.cs](tests/Domore.Logs.Tests/Logs/LoggingTest.cs) verifies that a failing service does not prevent a healthy service from completing and that a subsequent logging session delivers messages. The test failed against the old implementation, then passed with the fix on `net10.0`.

**Location:** [Logging.cs](source/Domore.Logs/Logs/Logging.cs), lines 181–207; [LogServiceCollection.cs](source/Domore.Logs/Logs/LogServiceCollection.cs), lines 115–132; [LogSubscriptionCollection.cs](source/Domore.Logs/Logs/LogSubscriptionCollection.cs), lines 50–69; [LogManager.cs](source/Domore.Logs/Logs/LogManager.cs), lines 85–109.

Before the fix, if one custom service's `Complete()` threw, later services were not completed and subscription cleanup was skipped. Although the manager's queue was disposed, the assignment clearing `Instance.Manager` was never executed. Subsequent enabled log calls were silently discarded by the disposed queue, and retrying completion threw `ObjectDisposedException`. The skipped completion, dropped later message, and failed retry were reproduced.

**Fix:** Attempt every service and subscription completion independently, always clear subscriptions, and retire the manager even when a completion step fails. Accumulate errors and report them after cleanup so callers still learn that completion was unsuccessful.

### 4. High — Shutdown accepts new work into the manager it is retiring

**Status: Fixed.** [Logging.cs](source/Domore.Logs/Logs/Logging.cs) now leases the active manager for each logging and subscription operation. Completion atomically detaches the active manager, waits for previously accepted operations to finish, then drains and retires it. New operations create or use the next manager. Calls from a retiring service worker remain recognized so the issue 2 deadlock protection is preserved.

**Regression coverage:** [LoggingTest.cs](tests/Domore.Logs.Tests/Logs/LoggingTest.cs) pauses service completion, subscribes and logs during the shutdown window, then verifies that the subscription is still registered and receives messages in the next session. The test failed against the old implementation and passed with the fix on `net10.0`. The existing callback-completion regression also passes. The full .NET 10 suite has 150 test cases: 149 pass and the Unix-root case is skipped on this Windows runner.

**Location:** [Logging.cs](source/Domore.Logs/Logs/Logging.cs), lines 12–101 and 106–207; [LogManager.cs](source/Domore.Logs/Logs/LogManager.cs), lines 26–32; [BackgroundQueue.cs](shared/Domore.Sharing/Threading/BackgroundQueue.cs), lines 70–85.

Before the fix, completion was serialized only against other completion calls. The old manager remained globally accessible while its queue had stopped accepting work and its services were completing. A service paused in `Complete()`, `Logging.Subscribe()` concurrently returned true, and completion then cleared that newly accepted subscription. The next manager had no subscriber. Log calls in the same window could also reach a queue that silently rejected additions.

**Fix:** Lease manager access and use one synchronized handoff to detach the old manager before its queue is completed. Work accepted before the handoff finishes against the old manager; calls after it use the next manager.

### 5. High — One failing service prevents delivery to the remaining services

**Status: Fixed.** [LogServiceCollection.cs](source/Domore.Logs/Logs/LogServiceCollection.cs) now catches and reports failures around each service's complete log path, then continues with the remaining services.

**Regression coverage:** [LoggingTest.cs](tests/Domore.Logs.Tests/Logs/LoggingTest.cs) verifies delivery continues after a service's `Log()` throws and after service type resolution throws during initialization. Both tests failed against the old delivery loop and passed with the fix on `net10.0`. The full suite has 150 test cases per target: 149 pass and the Unix-root case is skipped on this Windows runner.

**Location:** [LogServiceCollection.cs](source/Domore.Logs/Logs/LogServiceCollection.cs), lines 98–112; [LogServiceProxy.cs](source/Domore.Logs/Logs/LogServiceProxy.cs), lines 15–24 and 64–82; [BackgroundQueue.cs](shared/Domore.Sharing/Threading/BackgroundQueue.cs), lines 34–41.

Before the fix, exception handling surrounded the entire queued action rather than each service invocation. If the first service threw from `Log()` or while its type was being resolved, iteration stopped and healthy services later in the collection missed that entry. The worker continued with later actions, so repeated failures could continually starve those services. Both cases were reproduced.

**Fix:** Catch failures around each proxy's `Log()` call, which includes formatting and lazy service initialization. Report the exception and continue iterating so one service cannot block delivery to the others.

### 6. Medium — A throwing log-event handler interrupts all later delivery

**Status: Fixed.** [LogManager.cs](source/Domore.Logs/Logs/LogManager.cs) now snapshots the event handler delegate and invokes each subscribed handler independently. Exceptions are reported through `Logging.Notify`, while subsequent handlers, subscriptions, and services still receive the log.

**Regression coverage:** [LoggingTest.cs](tests/Domore.Logs.Tests/Logs/LoggingTest.cs) verifies the logging call returns, a later event handler runs, and a subscription and service both receive the entry. The test failed against the old code and passed with the fix on `net10.0`. The full suite has 150 test cases per target: 149 pass and the Unix-root case is skipped on this Windows runner.

**Location:** [LogManager.cs](source/Domore.Logs/Logs/LogManager.cs), lines 57–82.

Before the fix, `LogEvent?.Invoke(...)` ran before subscriptions and services without exception isolation. A throwing `Logging.Event` handler propagated into the application's logging call, prevented later event handlers from running, and skipped the subscription and service paths for that message.

**Fix:** Invoke each handler from a snapshot, catch and report failures individually, then continue normal delivery.

### 7. Medium — Unsubscribe leaves the subscription's threshold handler attached

**Status: Fixed.** [LogSubscriptionCollection.cs](source/Domore.Logs/Logs/LogSubscriptionCollection.cs) now completes proxies on removal and clearing. [LogSubscriptionProxy.cs](source/Domore.Logs/Logs/LogSubscriptionProxy.cs) makes detachment idempotent.

**Regression coverage:** [LoggingTest.cs](tests/Domore.Logs.Tests/Logs/LoggingTest.cs) verifies handler counts across repeated add/remove cycles, clear, and complete-then-clear, including that each proxy detaches only once. Both tests failed against the old collection and passed with the fix on `net10.0`.

**Location:** [LogSubscriptionCollection.cs](source/Domore.Logs/Logs/LogSubscriptionCollection.cs), lines 89–132; [LogSubscriptionProxy.cs](source/Domore.Logs/Logs/LogSubscriptionProxy.cs), lines 7–71.

Before the fix, removing a subscription detached the collection from the proxy but never called the proxy's `Complete()` to detach it from `Agent.ThresholdChanged`. A long-lived subscription retained the removed proxy and its cached types; repeated subscribe/unsubscribe cycles accumulated handlers. `Clear()` also relied on callers having separately completed every proxy.

**Fix:** Complete each proxy when removing or clearing it. Make proxy completion idempotent so the existing complete-then-clear path removes the agent handler only once, and continue cleanup for other proxies if one removal fails.

### 8. High — Subscription callbacks can invalidate live collection enumeration

**Status: Fixed.** [LogSubscriptionCollection.cs](source/Domore.Logs/Logs/LogSubscriptionCollection.cs) snapshots proxies for threshold queries, delivery, and completion, then invokes callbacks outside the collection lock. [LogServiceCollection.cs](source/Domore.Logs/Logs/LogServiceCollection.cs) does the same for queued delivery and service completion. The subscription threshold cache uses a generation check before storing a snapshot result.

**Regression coverage:** [LoggingTest.cs](tests/Domore.Logs.Tests/Logs/LoggingTest.cs) covers subscriptions added from threshold and receive callbacks, plus services added from log and completion callbacks. All four tests failed against the old iteration and passed with the fix on `net10.0`. The full suite has 150 test cases per target: 149 pass and the Unix-root case is skipped on this Windows runner.

**Location:** [LogSubscriptionCollection.cs](source/Domore.Logs/Logs/LogSubscriptionCollection.cs), lines 19–46 and 50–153; [LogServiceCollection.cs](source/Domore.Logs/Logs/LogServiceCollection.cs), lines 98–132.

Before the fix, subscription threshold and delivery paths invoked user callbacks while enumerating the live subscription dictionary under a reentrant lock. A callback that added a subscription invalidated the enumerator. Service delivery and completion similarly invoked services while enumerating the live service dictionary; reentrant configuration that added a service could invalidate those iterations. The resulting exception interrupted subsequent callbacks or skipped later service work.

**Fix:** Snapshot the relevant collections under their locks, then invoke callbacks outside the locks. Version the aggregate threshold cache so a callback-triggered subscription change cannot store a result computed from an outdated snapshot.

### 9. Medium — A threshold change can be overwritten by an in-flight cache fill

**Status: Fixed.** [LogSubscriptionProxy.cs](source/Domore.Logs/Logs/LogSubscriptionProxy.cs) now atomically replaces the threshold cache when the subscription reports a change. A query captures the current cache instance atomically, so a query already filling the retired instance cannot overwrite the new cache.

**Regression coverage:** [LoggingTest.cs](tests/Domore.Logs.Tests/Logs/LoggingTest.cs) gates a query that captured `Warn`, changes the subscription to `Debug`, and releases the old query. The test failed before the fix because subsequent queries still returned `Warn`; it passes after the fix on `net10.0`. The full suite has 150 test cases per target: 149 pass and the Unix-root case is skipped on this Windows runner.

**Location:** [LogSubscriptionProxy.cs](source/Domore.Logs/Logs/LogSubscriptionProxy.cs), lines 26–47.

**Before the fix:** `ThresholdChanged` cleared the cache, but an already executing `GetOrAdd` factory could subsequently insert the old threshold into the cleared cache. The old value then persisted until another change event. A gated reproduction paused a query returning `Warn`, changed the subscription to `Debug` and raised its event, then released the old query; subsequent queries still returned `Warn`. Delivery and enablement could therefore continue using obsolete filtering.

**Fix:** Swap the proxy cache instance atomically on invalidation so in-flight queries populate only the retired cache. The regression test deterministically interleaves a query and threshold change.

### 10. High — Nonpositive log batch limits can spin or fail outside the timer catch

**Status: Fixed.** [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs) rejects nonpositive `LogCountLimit` values before changing the current limit, synchronizes valid updates with the worker, and checks the limit defensively before allocating or dequeuing a batch. Its timer callback now catches failures across the complete callback.

**Regression coverage:** [FileLogTest.cs](tests/Domore.Logs.Tests/Logs/Services/FileLogTest.cs) tests zero and negative limits and verifies an invalid change leaves a running file service's valid setting and queued log intact. All three assertions failed against the original property and pass with validation on `net10.0`. The full suite has 150 test cases per target: 149 pass and the Unix-root case is skipped on this Windows runner.

**Location:** [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs), lines 173–233, 277–294, and 358–369; [FileLogTest.cs](tests/Domore.Logs.Tests/Logs/Services/FileLogTest.cs), lines 92–120.

**Before the fix:** `LogCountLimit` accepted any integer. At zero, a timer callback with queued data repeatedly allocated an empty list and consumed no entries, producing a busy loop. A negative value threw when constructing the list, before the callback's local exception handler. On the timer path that left an unhandled callback exception. Both paths were reproduced by invoking the old callback directly.

**Fix:** Require a positive limit in the setter and leave the existing value untouched on invalid input. Synchronize updates with batch processing, retain a worker-side validity check before allocation/dequeue, and contain exceptions across the timer callback.

### 11. High — Invalid flush intervals can strand the file queue and lose the first entry

**Status: Fixed.** [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs) validates `FlushInterval` as whole milliseconds from 1 through `Int32.MaxValue` before changing the configured value. Startup rechecks the value, queues the entry before creating the timer, and marks the service started only after timer creation succeeds. Each callback schedules another timer unless completion has begun; if restart fails, it clears `Started` so a later log call can retry, while completion still flushes queued data.

**Regression coverage:** [FileLogTest.cs](tests/Domore.Logs.Tests/Logs/Services/FileLogTest.cs) covers rejected negative, disabled, zero, sub-millisecond, fractional, and overflow delays; accepted timer bounds; correction after a failed timer start; and preservation of queued entries. The invalid-value and corrected-startup cases failed against the old implementation. All ten regression tests pass, and the full suite has 150 test cases per target: 149 pass and the Unix-root case is skipped on this Windows runner.

**Location:** [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs), lines 173–255, 262–275, and 358–369; [FileLogTest.cs](tests/Domore.Logs.Tests/Logs/Services/FileLogTest.cs), lines 123–188.

**Before the fix:** `FlushInterval` was cast directly to an integer timer delay. A `-2 ms` delay threw after `Started` had been set and before the first entry was enqueued; subsequent calls then queued messages without starting another timer. A `-1 ms` delay disabled the timer, and large values could not be represented by its integer delay. Timer restart failures were caught after issue 10 but left the service marked started, preventing later log calls from retrying.

**Fix:** Validate interval bounds and whole-millisecond precision at assignment and startup. Queue before attempting timer creation and set `Started` only after successful creation. Catch callback and restart failures, resetting `Started` after a failed restart so later logging can retry; completion drains any retained entries.

### 12. High — Changing a service's type abandons the previous service

**Status: Fixed.** [LogServiceProxy.cs](source/Domore.Logs/Logs/LogServiceProxy.cs) now serializes type replacement with log delivery and service retrieval, drains any in-flight callback before retiring the old instance, completes each instance at most once, and exposes the locked service getter used by configuration to apply service settings. Type changes requested reentrantly from a service callback are applied after that callback returns.

**Regression coverage:** [LoggingTest.cs](tests/Domore.Logs.Tests/Logs/LoggingTest.cs) blocks an old service's `Log()` callback while requesting a type change, then verifies the old service completes once after delivery ends, the new service receives the next message, and shutdown completes the new service once. It failed against the old proxy because the old service was never completed, then passed with the fix. On each of `net462`, `net8.0`, and `net10.0`, 149 tests pass and the Unix-root case is skipped on this Windows runner.

**Location:** [LogServiceProxy.cs](source/Domore.Logs/Logs/LogServiceProxy.cs), lines 7–15, 23–108, and 122–154; regression test: [LoggingTest.cs](tests/Domore.Logs.Tests/Logs/LoggingTest.cs), line 385.

**Before the fix:** Runtime reconfiguration set `_Service = null` without completing the previous instance. A buffered file service or custom service could still have pending work, timers, or other resources, but later `Logging.Complete()` could only reach the replacement. Exit-time flushing was no longer guaranteed for the old instance. A recording service confirmed that changing `Type` never called its completion method.

**Fix:** Hold the proxy lock across service retrieval, callbacks, and replacement. Expose service retrieval to configuration through the locked `Service` property. Detach the old service, complete it before accepting delivery through the replacement, and guard completion with per-instance state. Defer reentrant type changes until the current callback has returned.

### 13. Medium — Archive age calculations mix local time and UTC

**Status: Fixed.** [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs) now writes the archive timestamp in local wall time and appends its numeric UTC offset. The parser uses that offset to recover the exact UTC instant for age calculations. It reads older names without an offset as local timestamps.

**Regression coverage:** [FileLogTest.cs](tests/Domore.Logs.Tests/Logs/Services/FileLogTest.cs) verifies the visible filename clock fields match local time, the appended offset round-trips to a recent UTC instant, and unmarked legacy timestamps parse as local. The test failed against the old implementation and passed with the fix. The complete suite has 150 test cases per target: 149 pass and the Unix-root case is skipped on this Windows runner.

**Location:** [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs), lines 48–96 and 124–132; regression test: [FileLogTest.cs](tests/Domore.Logs.Tests/Logs/Services/FileLogTest.cs), line 396.

**Before the fix:** Archive names used `DateTime.Now`, then were parsed as `DateTimeKind.Utc` and subtracted from `DateTime.UtcNow`. The timezone offset became part of the calculated age. In this environment a newly generated archive parsed as approximately five hours old. Archives could be deleted too early in timezones behind UTC or retained too long in timezones ahead of UTC; daylight-saving changes also made local timestamps ambiguous.

**Fix:** Generate new names from `DateTimeOffset.Now`, keeping their clock fields local and appending the local offset as `±HHmm`. Parse the wall time with that explicit offset, then compare it in UTC. Parse older unmarked names as local for same-timezone compatibility. Since legacy names have no offset, an older filename from a different timezone or the repeated daylight-saving hour cannot be reconstructed exactly; the explicit offset removes that ambiguity for new archives.

### 14. Medium — An invalid archive date aborts retention for otherwise valid files

**Status: Fixed.** [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs) now checks the archive prefix and extension, parses the timestamp with an exact invariant format, and returns null for invalid calendar dates or times. Offset components are parsed invariantly and validated before conversion.

**Regression coverage:** [FileLogTest.cs](tests/Domore.Logs.Tests/Logs/Services/FileLogTest.cs) creates invalid month, day, leap-date, and time archives beside a valid expired archive, and also checks that wrong prefixes and extensions are rejected. The test failed against the old parser because rotation threw; it now passes, confirming retention deletes the expired archive and ignores the malformed files. On each of `net462`, `net8.0`, and `net10.0`, 149 tests pass and the Unix-root case is skipped on this Windows runner.

**Location:** [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs), lines 57–96 and 124–132; regression test: [FileLogTest.cs](tests/Domore.Logs.Tests/Logs/Services/FileLogTest.cs), line 428.

**Before the fix:** Numeric components were parsed and passed directly to a `DateTime` constructor for legacy archive names. Invalid months, days, leap dates, or times threw during `Rotate()` list materialization, preventing cleanup of other valid archives. The regression test reproduced this with four malformed archives and an expired valid archive.

**Fix:** Use `DateTime.TryParseExact` with the invariant `yyyyMMdd-HHmmss-fff` format to validate the calendar and clock fields without throwing. Verify the configured archive prefix and extension before extracting the date, and parse the explicit offset digits with invariant numeric rules. Invalid names return null and are excluded from retention processing.

### 15. Medium — The final flush bypasses rotation and retention

**Status: Fixed.** [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs) now sends timer batches and the completion batch through the same `Flush()` method, which writes the entries and then rotates the file so age and total-size retention both run.

**Regression coverage:** [FileLogTest.cs](tests/Domore.Logs.Tests/Logs/Services/FileLogTest.cs) queues an entry with a one-hour timer, completes before the timer fires, and verifies the entry is archived and a pre-existing expired archive is removed. It failed against the old completion path, which left the active file and expired archive in place; it passes with the shared flush path. On each of `net462`, `net8.0`, and `net10.0`, 149 tests pass and the Unix-root case is skipped on this Windows runner.

**Location:** [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs), lines 180–181, 214–215, and 352–367; regression test: [FileLogTest.cs](tests/Domore.Logs.Tests/Logs/Services/FileLogTest.cs), line 192.

**Before the fix:** Timer batches called `Log(lines)` and `Rotate()`, but `Complete()` only called `Log(lines)`. A process that completed before its first timer callback left an oversized active log and skipped cleanup of existing expired archives. The regression test reproduced both failures with a pending entry and an expired archive.

**Fix:** Route both timer and completion batches through `Flush(lines)`, which performs the write and rotation under their existing locks and exception handlers. Completion now applies the same age and total-size retention as a timer flush.

### 16. High — Path formatting strips UNC and rooted-path prefixes

**Status: Fixed.** [PathFormatter.cs](shared/Domore.Sharing/IO/PathFormatter.cs) now retains the native root structure while formatting all path components, including UNC server and share components. It preserves drive roots, drive-relative paths, current-drive-rooted paths, UNC shares, and extended Windows roots.

**Regression coverage:** [PathFormatterTest.cs](tests/Domore.Logs.Tests/IO/PathFormatterTest.cs) verifies current-drive-rooted, UNC, extended UNC, and extended drive paths, plus token expansion inside a UNC root. Current-drive and UNC cases failed against the old formatter; the token test failed against the first root-preserving change. All Windows cases pass with the final implementation. The Unix absolute-root test is conditional and was skipped on this Windows runner. On each of `net462`, `net8.0`, and `net10.0`, 149 tests pass and the Unix-root case is skipped.

**Location:** [PathFormatter.cs](shared/Domore.Sharing/IO/PathFormatter.cs), lines 30–99; [PathFormatterTest.cs](tests/Domore.Logs.Tests/IO/PathFormatterTest.cs), lines 232–278; [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs), lines 27–34.

**Before the fix:** Splitting on separators and removing empty components discarded leading separators. UNC paths such as `\\server\share\logs` became `server\share\logs`, and current-drive-rooted paths such as `\logs\app` became relative. Unix absolute paths lost their leading slash as well; that platform behavior was identified by inspection.

**Fix:** Preserve `Path.GetPathRoot(path)` independently while formatting path components. Recombine the formatted components with the original root, retaining drive-relative semantics and adding a separator only when the root needs one. Test Windows root variants and Unix absolute paths on their respective platforms.

### 17. Medium — Absolute file names rotate into a different directory

**Status: Fixed.** [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs) now derives directory creation, rotation destinations, and archive retention searches from the resolved active file path. Absolute names remain supported, and relative names with subdirectories are created and maintained in their resolved parent directory.

**Location:** [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs), lines 27–28, 98–112, and 133–144.

An absolute `Name` overrides `Directory` when the active path is combined, but rotation and archive discovery always use `DirectoryInfo.FullName`. With `Directory=A` and `Name=B/app.log`, the active log is written in B and the archive is moved into A. This was reproduced. The destination directory and archive retention scope therefore differ from the active log's location. Relative names containing subdirectories also need an explicit policy because creation currently prepares only the configured base directory.

**Fix:** Use the resolved `FileInfo` parent directory for creation, rotation, archive discovery, and retention. This keeps those operations beside the active file even when `Name` is absolute or contains relative subdirectories.

### 18. Low — Repeated path tokens in a component are only partly replaced

**Location:** [PathFormatter.cs](shared/Domore.Sharing/IO/PathFormatter.cs), lines 52–67.

The replacement loop calls `IndexOf` once per token per path component. For `{Thread.ManagedThreadId}-{thread.managedthreadid}.log`, only the first occurrence is replaced and the second remains literal. This was reproduced with mixed token casing, producing a file name that differs from the configured template's intended result.

**Fix plan:** Replace every occurrence case-insensitively, evaluating and sanitizing each token value once. Advance through the original template so inserted text is not reinterpreted. Test repeated tokens in one file name and one directory component.

### 19. Medium — Observers can mutate the log data received by other observers

**Location:** [LogEntry.cs](source/Domore.Logs/Logs/LogEntry.cs), lines 46–53 and 67; [LogManager.cs](source/Domore.Logs/Logs/LogManager.cs), lines 57–82.

`ILogEntry.LogList` exposes the actual mutable `string[]` through an `IEnumerable<string>`. An event handler or subscription can cast it back to an array and change messages before later observers or the asynchronous services consume them. A reproduction logged `original`, changed the array in an event handler, and delivered `changed` to the subscription. Retaining and editing the array can also affect queued service output later.

**Fix plan:** Keep the formatted array private and expose a read-only view that does not reveal it; copy incoming arrays if ownership is not exclusive. Test attempted mutation and consistent content across events, subscriptions, and queued services.

### 20. High — A missing-file check can lead to truncating another writer's data

**Status: Fixed.** [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs) now appends directly to the active path after ensuring its parent directory exists. The append operation creates a missing file without truncating a file another writer created.

**Location:** [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs), lines 148–156.

The file writer calls `FileInfo.Create()` if the cached `Exists` value is false, then appends. `Create()` truncates a file if another writer creates it between the existence check and creation, or if that cached result has become stale. A controlled reproduction cached a missing-file result, wrote existing content through another writer, and then flushed the logger: the existing content disappeared and only the new log line remained.

**Fix:** Remove the separate existence check and `Create()` call. `File.AppendAllLines` now handles both creating a missing file and appending to an existing one, while the existing I/O retry behavior remains in place.

### 21. Medium — Both logging queues can grow without a bound

**Location:** [BackgroundQueue.cs](shared/Domore.Sharing/Threading/BackgroundQueue.cs), lines 11 and 70–73; [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs), lines 13 and 363; [LogManager.cs](source/Domore.Logs/Logs/LogManager.cs), lines 57–82.

The service queue uses an unbounded `BlockingCollection<Action>`, and the file writer uses an unbounded `ConcurrentQueue<string>`. A slow or blocked service holds up the single service worker while producers continue retaining formatted entries. Slow file I/O can independently accumulate strings in the file queue. `LogCountLimit` limits batch size rather than backlog. Entries below every configured service threshold are also queued because filtering occurs in service delivery. Sustained producer/consumer imbalance has no memory limit or overload policy. This is a source-confirmed capacity issue; no destructive memory stress test was run.

**Fix plan:** Define configurable queue capacity, overload behavior, and observable drop/backlog counters. Apply filtering before enqueue where it preserves the intended configuration semantics, and account for large message sizes as well as item counts. Test overload using a gated slow service and verify the documented policy.

### 22. Medium — Failed configuration-file setup permanently reports success state

**Location:** [Log.cs](source/Domore.Logs.Conf/Logs/Log.cs), lines 7–20.

`Log.Conf.Configure()` publishes `File` before `File.Configure(watch: true)` succeeds. If setup throws, `Configured` still returns true and every later call returns false. A reproduction used a path in a nonexistent directory: watcher setup threw, the configured flag became true, and retrying with a valid file returned false. Failed setup can also leave partially initialized resources retained by the static field.

**Fix plan:** Construct and configure a local candidate, publish it only after success, and dispose it on failure. Keep the one-time initialization lock but allow retry after a failed attempt. Test watcher/setup failures followed by a valid retry.

### 23. Medium — The configuration watcher keeps targeting a retired manager

**Status: Fixed.** [LogConfFile.cs](source/Domore.Logs.Conf/Logs/LogConfFile.cs) now gives `ConfFile` a target proxy whose `Log` property resolves through `Logging.Config` each time the file is applied. Reloads therefore configure the current manager after logging restarts.

**Location:** [LogConfFile.cs](source/Domore.Logs.Conf/Logs/LogConfFile.cs), lines 5–26; [Logging.cs](source/Domore.Logs/Logs/Logging.cs), lines 21–28, 157–158, and 181–207; [Log.cs](source/Domore.Logs.Conf/Logs/Log.cs), lines 5–20.

The watched `ConfFile` captures one `Logging.Config` object at construction, including its specific manager. `Logging.Complete()` disposes and retires that manager, but the static watcher remains configured. If logging is subsequently restarted, configuration-file changes still update the old manager, while the new manager remains unconfigured; calling `Log.Conf.Configure()` again returns false. An integration reproduction verified that a changed file enabled Debug on the captured old manager while Debug remained disabled on the current one.

**Fix:** Keep the watcher active and resolve the root `Log` object from the current `Logging.Config` whenever `ConfFile` applies its contents. The watcher can then update the active manager after `Logging.Complete()` creates a new session.
