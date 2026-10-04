# Branch comparison: logs vs logs2

Reviewed 2026-10-04, using local branch heads:

| Branch | Commit |
|---|---|
| dev (common base) | 830f962c92523328345a6424a4fb4f3fc0477875 |
| logs | 339131cae7aff8c8d778c3867d53ae3c522cec2d |
| logs2 | 99e0e4c56a47af519db977e5d041f673e246ccae |

## Recommendation

**Use logs as the merge base, and fix its remaining defects before merging. Neither branch is ready to merge unchanged.**

logs provides the stronger foundation for manager lifetime, service replacement, file preservation, and compatibility. Manager leases let operations already in progress finish before disposal. Replacement services are coordinated with active callbacks. File rotation uses the actual file's parent directory, appending preserves existing contents, and existing short type settings continue to work.

logs2 fixes more general robustness problems: exception containment, recursive event/subscriber delivery, early threshold checks, formatter isolation, invariant timestamps, console synchronization, diagnostics, and normal process-exit handling. These are useful changes to bring into logs selectively. However, its shutdown and replacement design permits unsafe disposal and loss of cleanup work. Timed shutdown crashed the .NET 8 test host twice during this comparison. Its file service also retains defects that truncate existing contents or delete matching files in the wrong directory.

This recommendation weights process safety, preservation of files, service lifetime, and compatibility more heavily than fix counts. The branches' existing review catalogs have different issue numbers; resolved labels in those reports do not establish current correctness.

## Approach comparison

| Area | logs | logs2 | Assessment |
|---|---|---|---|
| Manager lifetime | Leases operations; waits for active calls; defers completion requested by owned callbacks | Atomic manager exchange without operation leases | Prefer logs |
| Service replacement | Coordinates active calls and pending replacements | Queues old-service completion alongside bounded message traffic | Prefer logs; cleanup must not be droppable |
| Shutdown bounds | Unbounded wait | Bounds queue join, then completes and disposes still-active components | Neither complete; logs2 unsafe |
| Type configuration | Existing short names and explicit None preserved | Full names; explicit None omitted from aggregate snapshot | Prefer logs |
| Subscriptions | Callback snapshots and replaced threshold caches | Published snapshots and generation-tagged caches | Both improve dev substantially |
| Formatting | Mostly unchanged from dev | Per-item failure isolation, null fallback, 100-item expansion budget | Prefer focused logs2 fixes |
| Entries | Clones caller-owned arrays and exposes read-only messages | Read-only messages, invariant timestamps, undefined-severity fallback | Combine focused improvements |
| File service | Actual parent, direct append, final-flush rotation, offset-bearing archive timestamps | UTC names and isolated cleanup errors, but file-integrity gaps remain | Prefer logs as base |
| Console/settings | Plain dictionaries and several unsynchronized settings remain | Concurrent dictionaries, color fallback, locked wide settings | Prefer logs2 |
| File configuration | Watcher survives; reload leases the current manager | Global completion event disposes watcher | Different semantics; logs2 has a cross-session race |
| Diagnostics | Reload errors unreported; Notify writes stdout | Reload errors reported; Notify writes stderr | Prefer logs2 |
| Scope | 22 changed files | 45 changed files, including generic Conf, SDK/editor settings, and public API | logs has less integration exposure |

logs2 also adds public Logging.Complete(TimeSpan), changes LogName and configuration keys to full type names, and sorts all configuration pairs by key depth. Sorting fixes service-before-type ordering, and the Conf tests pass, but this affects every configured object. These changes require deliberate compatibility and release decisions.

## Validation

Both branches were exported with git archive into separate temporary directories. Production code and branch references were not changed. The same temporary NUnit fixture was added to each export.

Windows, .NET SDK 10.0.112, Release configuration:

| Check | logs | logs2 |
|---|---|---|
| Existing logging suite, net462 | 184 passed, 1 skipped | 180 passed |
| Existing logging suite, net8.0 | 184 passed, 1 skipped | 180 passed |
| Existing logging suite, net10.0 | 184 passed, 1 skipped | 181 passed |
| 23 comparisons, net462 | 12 passed, 11 failed | 11 passed, 12 failed |
| 23 comparisons, net8.0 | 12 passed, 11 failed | Full run crashed twice during timed shutdown |
| Earlier net8.0 focused groups | Same 11 failures established | All 12 failures established |
| Logging libraries and Conf dependency, all 9 library targets | Build passed | Build passed |
| Existing Domore.Conf suite, net8.0 | Not repeated; population unchanged | 373 passed |

Library targets: net40, net45, net462, net48, netstandard2.0, netcoreapp3.1, net6.0, net8.0, net10.0. Builds had zero errors and SourceLink warnings caused by exporting without Git metadata. SDK-path access initially failed in the sandbox; approved build/test permissions allowed the checks to complete.

The first concurrent logs net10.0 baseline run failed because one test deletes the common parent directory while other test processes are using it. Rerunning the suite alone passed. This was not counted as an implementation failure.

No full-solution build/test/pack, Unix runtime checks, or runtime checks on the other targets were performed. Comparison failures demonstrate defects or policy differences; the counts are not a severity score.

### Identical comparison results

| Behavior checked | logs | logs2 |
|---|---|---|
| Null type log does not throw | Fail | Pass |
| Nested event logging avoids synchronous recursion | Fail | Pass |
| Disabled logging avoids formatting | Fail | Pass |
| Formatter failure preserves surrounding arguments | Fail | Pass |
| Null formatter result falls back | Fail | Pass |
| Undefined severity still formats | Fail | Pass |
| Gregorian timestamps under th-TH | Fail | Pass |
| Existing short type threshold honored | Pass | Fail |
| Explicit None reported disabled | Pass | Fail |
| Nested file name creates actual parent | Pass | Fail |
| Final file flush applies rotation | Pass | Fail |
| Root-relative path stays rooted | Pass | Fail |
| Repeated path tokens fully expand | Pass | Fail |
| Completion preserves already-started entry | Pass | Fail |
| Completion does not overlap active service call | Pass | Fail |
| Full queue does not lose replacement cleanup | Pass | Fail |
| Null params array produces empty entry | Fail | Pass |
| Enumerable expansion has a budget | Fail | Pass |
| Service properties before type retained | Fail | Pass |
| New configuration survives old-manager shutdown | Pass | Fail |
| Accepted item drains despite first-worker startup race | Fail | Pass |
| File created after cached miss keeps existing contents | Pass | Fail |
| Absolute name rotates in actual parent, preserving other directories | Pass | Fail |

The null-array and enumeration-budget checks expose policy choices as well as implementation differences. Their desired behavior should be decided explicitly when completing the fixes.

## Findings

Numbers below identify this comparison's findings, independently of either branch's original review numbers. Locations refer to the named branch. Paths abbreviated as Logs/... are under source/Domore.Logs/.

### 1. High — logs2 disposes an active queue and completes active services after timeout

Locations: logs2 Logs/Logging.cs:164, Logs/LogServiceCollection.cs:131, Logs/LogServiceProxy.cs:105, and shared/Domore.Sharing/Threading/BackgroundQueue.cs:26.

After Queue.Complete(timeout) returns false, service completion still runs and manager disposal still disposes the collection. A gated service observed Complete running during its Log call. The worker can catch an end-of-queue exception and then read IsAddingCompleted after disposal, throwing outside the protective catch.

Two complete .NET 8 probe runs crashed with an unhandled ObjectDisposedException at BackgroundQueue.ThreadStart, line 26, immediately after the 50 ms completion timeout.

**Resolve:** Separate retirement from disposal. Keep workers, queues, and services alive until active work ends; arrange guaranteed deferred cleanup after timeout. Make shutdown exception handling safe during disposal.

### 2. High — logs2 loses an entry already being logged when completion starts

Locations: logs2 Logs/Logging.cs:26 and :166; Logs/LogManager.cs:79 and :123.

Atomic manager publication prevents the double-read null race but does not preserve manager lifetime. An entry blocked in a synchronous event has not yet entered the service queue. Concurrent completion disposes its manager, and the resumed callback submits to the disposed queue. The probe delivered zero messages in logs2 and one in logs. Registrations/configuration obtained just before the exchange have similar exposure.

**Resolve:** Lease active manager operations or safely redirect/wait for them during retirement. logs already has a lease mechanism.

### 3. High — logs2 drops service cleanup when its message queue is full or closed

Locations: logs2 Logs/LogServiceCollection.cs:31, Logs/LogServiceProxy.cs:57, and BackgroundQueue.cs:92.

Old-service completion is ordinary Queue.Add work. A full queue drops it, and Add swallows shutdown/disposal rejection. The surrounding fallback catch therefore never runs. The proxy has detached the old service, so shutdown cannot recover it. With a gated worker and 1,100 submissions, its completion count was zero in logs2 and one in logs.

**Resolve:** Give lifecycle actions a guaranteed path separate from droppable messages. Retain ownership of detached services until their cleanup is handled.

### 4. High — logs2 can truncate a file created after a cached missing-file result

Location: logs2 Logs/Service/FileLog.cs:174.

FileInfo.Exists can retain false. If another writer creates the file afterward, FileInfo.Create truncates it before appending. The probe lost existing data in logs2; logs preserved it through direct append. This is a retained dev defect that logs fixes.

**Resolve:** Append directly and create the actual parent. Never choose truncating creation from cached existence state.

### 5. High — logs2 rotates in the wrong directory and can delete unrelated files

Locations: logs2 Logs/Service/FileLog.cs:108 and :125.

A rooted Name overrides Directory for the active path, but rotation and retention still use the configured Directory. The probe wrote into one directory while configuring another. logs2 moved the archive to the configured directory and deleted a pre-existing matching archive there. logs used the actual parent and preserved the other file. This is another retained dev defect that logs fixes.

**Resolve:** Derive archive paths and retention searches from the actual active file's parent.

### 6. Medium — logs2 breaks existing short-name configuration and output

Locations: logs2 Logs/LogEntry.cs:47, Logs/LogServiceCollection.cs:110, and source/Domore.Logs/README.md:7.

A setting such as log[x].config[Sample].severity = debug stops applying to a namespaced Sample. Short-name fallback occurs only when FullName is unavailable, rather than when a full-name setting is absent. This also changes per-type formats, custom service names, event LogName, and {log} output.

**Resolve:** Preserve compatibility or implement full-name lookup with a short-name settings fallback and a deliberate output migration.

### 7. Medium — logs2 aggregate thresholds forget explicit None

Location: logs2 Logs/LogServiceCollection.cs:65.

When all effective per-type thresholds are None, that type is omitted from the snapshot. Enabled falls back to a non-None default, reporting true even though no service accepts the type. It also defeats early formatting/queue filtering.

**Resolve:** Preserve the distinction between an absent type override and an explicit None. logs does so.

### 8. Medium — logs2 can dispose a newer session's configuration watcher

Locations: logs2 Logs/Logging.cs:185 and source/Domore.Logs.Conf/Logs/Log.cs:13.

While an old manager drains, a caller can configure a new manager and publish its watcher. The old completion raises an unqualified global Completed event; its handler clears whichever File is installed, including the new one. The probe observed Configured becoming false after successful new-session configuration.

**Resolve:** Associate watcher lifetime and completion notification with a particular session.

### 9. Medium — logs2 retains path defects fixed by logs

Locations: logs2 shared/Domore.Sharing/IO/PathFormatter.cs:34 and :61.

Root-relative and UNC prefixes are lost during split/recombine. Only the first token occurrence in a path component is replaced. Both focused path probes fail on logs2 and pass on logs.

**Resolve:** Bring across logs' root-preserving formatting and complete token replacement.

### 10. Medium — logs2 misses nested file parents and final-flush rotation

Locations: logs2 Logs/Service/FileLog.cs:170 and :403.

Name nested/output.log fails because only the configured Directory is created. Completion calls Log(lines) without Rotate, letting the last batch bypass rotation and retention. Both checks pass on logs and fail on logs2.

**Resolve:** Create the actual parent and share the append-and-rotate operation between timer and completion flushes.

### 11. High — logs still throws from logging with a null type

Locations: logs Logs/LogManager.cs:61, Logs/LogEntry.cs:60, and Logs/Logging.cs:82.

Logging.For(null) returns a logger whose Info("message") throws ArgumentNullException during entry construction. There is no outer logging exception boundary.

**Resolve:** Bring across the null-type/severity guard and outer exception containment from logs2.

### 12. High — logs still permits synchronous logging recursion

Location: logs Logs/LogManager.cs:74.

A handler that logs re-enters itself on the same thread. A bounded probe observed six invocations in logs and one in logs2; unconditional recursion can overflow the stack. Subscriber, formatter, and threshold callbacks also need a recursion policy.

**Resolve:** Guard extensible logging paths, accounting for formatting and enabled checks as well as events/subscribers.

### 13. Medium — logs loses an accepted item during first-worker startup

Locations: logs BackgroundQueue.cs:46 and :73.

Add enqueues before starting/publishing the worker. Complete can see no Thread and return success despite pending work. Subsequent disposal loses it. A deterministic probe held the startup lock and observed one accepted item but zero executions.

**Resolve:** Synchronize admission, worker startup, and completion. logs2's startup ordering addresses this case.

### 14. Medium — logs retains formatter failure and unbounded-work defects

Locations: logs Logs/LogFormatter.cs:25, :31, :53; Logs/LogManager.cs:65.

One failed argument replaces the entire message. A null custom result becomes an exception dump. Enumerables have no expansion budget. Disabled calls still format all data.

**Resolve:** Bring across per-item failure isolation and null fallback; add a correct enabled gate and documented enumeration policy. A budget cannot bound a blocking individual MoveNext or custom formatter.

### 15. Medium — logs retains culture-sensitive timestamps and undefined-severity failure

Location: logs Logs/LogEntry.cs:27.

Under th-TH, the 2026 timestamp used Buddhist year 2569. An undefined severity threw KeyNotFoundException during LogData. Both checks pass on logs2.

**Resolve:** Use invariant culture for timestamp tokens and a fallback for undefined severity.

### 16. Medium — logs loses service settings listed before explicit type

Locations: logs Logs/LogServiceProxy.cs:70 and source/Domore.Logs.Conf/Conf/Logs/LogConfContainer.cs:10.

Accessing Service before Type can instantiate the wrong service. Type replacement then discards its settings. The probe's label survived on logs2 but was lost on logs.

**Resolve:** Apply service types before dependent logging settings or buffer those settings. Keep the ordering change within logging unless generic Conf sorting is deliberately accepted.

### 17. Medium — logs retains unsafe console dictionaries and unsynchronized file settings

Locations: logs Logs/Service/ConsoleLog.cs:21 and :34; Logs/Service/FileLog.cs:272.

Hot reload can mutate color dictionaries while the service reads them. Several long/TimeSpan file settings remain unsynchronized and can tear on 32-bit runtimes. Color failures can suppress console output.

**Resolve:** Bring across logs2's concurrent dictionaries, wide-setting synchronization, and color fallback while preserving logs' file integrity changes. Established by inspection; these races were not stress-tested here.

### 18. Medium — logs drops reload diagnostics and lacks automatic exit flushing

Locations: logs source/Domore.Logs.Conf/Logs/LogConfFile.cs:29 and Logs/Logging.cs:86.

ConfFile error events are not hooked up. Notify writes stdout. There is no ProcessExit handler. logs2 reports reload failures, writes stderr, and passed its normal-exit integration test on net10.0.

**Resolve:** Bring across event hookup and stderr reporting. Add normal-exit flushing with safe retirement. Neither branch guarantees flushing on forced termination.

### 19. Medium — logs publishes Default before attaching its threshold handler

Location: logs Logs/LogServiceConfig.cs:43.

Another thread can change the published threshold before the handler is attached, leaving aggregate thresholds stale. logs2 attaches before atomic publication.

**Resolve:** Attach first and publish safely. Established by inspection; no added stress test covered this window.

### 20. Low/Medium — logs constructs unused services during shutdown

Location: logs Logs/LogServiceProxy.cs:163.

Complete calls GetServiceUnsafe, creating a service solely to complete it. Constructors can fail or cause side effects during shutdown. logs2 completes only existing instances.

**Resolve:** Avoid lazy construction in Complete, while retaining logs' handling of replacements installed by completion callbacks.

### 21. Medium — neither branch fully resolves capacity and shutdown bounds

Locations: logs BackgroundQueue.cs:11, Logs/LogServiceCollection.cs:129, Logs/Service/FileLog.cs:14; logs2 BackgroundQueue.cs:9, Logs/LogServiceCollection.cs:151, Logs/Service/FileLog.cs:14.

logs has unbounded queues and indefinite completion waits. logs2 caps background actions but leaves the file queue unbounded and calls service Complete synchronously after its queue timeout. A blocking Complete still hangs the caller; an action-count cap does not bound message bytes.

**Resolve:** Define capacity/overload policy for the complete pipeline. Keep lifecycle actions guaranteed and arrange safe deferred retirement when work exceeds a timeout.

### 22. Medium — neither branch fully prevents logging feedback loops

Locations: both branches' Logs/LogServiceCollection.cs Send worker and Logs/Service/TraceLog.cs; logs2 Logs/LogManager.cs:85.

A service or Trace listener forwarding every message back into logging creates further queue iterations. logs2's depth guard handles synchronous event/subscriber delivery, not a fresh service-worker iteration. Reentrant formatter and threshold callbacks also exceed that guard's scope.

**Resolve:** Define origin/reentrancy handling across sinks, formatting, thresholds, and diagnostics. Established by inspection; no infinite feedback loop was executed.

### 23. Medium — logs retention stops after one failed archive deletion

Location: logs Logs/Service/FileLog.cs:136.

A locked archive throws from Delete and aborts the remainder of that rotation's cleanup. logs2 isolates deletion errors and continues.

**Resolve:** Isolate each archive failure and maintain consistent size accounting, preserving logs' actual-parent selection and offset-aware parsing.

## Integration order

1. Start from logs, preserving leases, service replacement ownership, actual-parent rotation, direct append, final-flush rotation, and path formatting.
2. Fix null-type exceptions, synchronous recursion, first-worker startup, and default-threshold publication.
3. Bring across focused logs2 formatting, timestamp/severity, console/settings, reload diagnostic, and stderr improvements.
4. Resolve service-before-type settings within logging, unless broader Conf ordering is deliberately selected.
5. Design capacity and shutdown together: preserve cleanup work and keep active workers/services alive after timeout.
6. Preserve short-name settings or introduce a deliberate compatibility migration.
7. Add the comparison regressions to the selected branch and require them to pass before merging.

## Reproduction artifacts

Temporary export root: C:/Users/ken/AppData/Local/Temp/domore-branch-comparison-20261004.

Both exports contain identical tests at tests/Domore.Logs.Tests/Logs/BranchComparisonTest.cs. Results are in tests/Domore.Logs.Tests/TestResults. logs2's comparison-23-net8.0.trx and comparison-23-net8-retry.trx preserve the crash details.

From each exported branch:

    dotnet test tests/Domore.Logs.Tests/Domore.Logs.Tests.csproj -f <framework> --configuration Release
    dotnet test tests/Domore.Logs.Tests/Domore.Logs.Tests.csproj -f <framework> --no-restore --configuration Release --filter FullyQualifiedName~BranchComparisonTest
    dotnet build source/Domore.Logs.Conf/Domore.Logs.Conf.csproj --no-restore --configuration Release

Only this report was added to the original working tree.
