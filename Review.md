# Review: Domore.Logs and Domore.Logs.Conf

Scope: `source/Domore.Logs`, `source/Domore.Logs.Conf`, plus the shared helpers they depend on, `shared/Domore.Sharing/Threading/BackgroundQueue.cs` and `shared/Domore.Sharing/IO/PathFormatter.cs`. This is a review only. No code was changed.

Guiding rule for every suggested fix: **No public `ILog` method (`Data`, `Debug`, `Info`, `Warn`, `Error`, `Critical`, including the `bool` "enabled" overloads) may throw.** Failures should be swallowed and reported through `Logging.Notify`, and should not affect other services or subscribers.

Severity: **High** means a crash, deadlock, hang, or throw from a log method. **Medium** means silent data loss or wrong behavior. **Low** means robustness, performance, or design.

| # | Sev | Area | Summary |
|---|-----|------|---------|
| 1 | High | Logging | Resolved: `Manager` getter double-read races with `Complete()` and causes an NRE thrown from `Log` |
| 2 | High | LogManager | Resolved: `LogEvent` handler exceptions propagate out of `Log` methods |
| 3 | High | Logger/LogManager | Resolved: `Logging.For(null)` gives an `ILog` whose `Data`/`Info`/... throw `ArgumentNullException` |
| 4 | High | LogSubscriptionCollection | Resolved: User callbacks run under `lock (Lookup)`, which can deadlock |
| 5 | High | LogServiceCollection | Resolved: Services run under `Locker`, which is also taken by `Enabled()`. This can deadlock or stall the app |
| 6 | High | Logging/LogManager | Resolved: Re-entrant logging from handlers, subscribers, or services can recurse without bound (stack overflow) or amplify |
| 7 | High | FileLog | Resolved: Bad config values can crash the process from the timer callback or permanently stop file logging |
| 8 | High | Logging/LogServiceCollection | Resolved: One failing `ILogService.Complete()` stops the rest from flushing and leaves a disposed manager installed for good |
| 9 | High | Logging/BackgroundQueue | Resolved: `Complete()` can hang forever, or deadlock when called from a service thread |
| 10 | Medium | LogServiceCollection | Resolved: One throwing service drops the entry for all later services |
| 11 | Medium | LogServiceProxy | Resolved: `Service` double-read races with a `Type` change (NRE). The old service is never completed |
| 12 | Medium | LogServiceConfig | Resolved: `Default` is published before its `ThresholdChanged` handler is attached (lost update) |
| 13 | Medium | LogServiceCollection | Resolved: The aggregate threshold used by `Enabled()` is wrong when type and default thresholds are mixed across services |
| 14 | Medium | LogSubscriptionProxy | Resolved: Threshold cache invalidation race can make a stale threshold permanent |
| 15 | Medium | LogSubscriptionCollection | Resolved: `Remove`/`Clear` leave the proxy subscribed to the agent's `ThresholdChanged` |
| 16 | Medium | BackgroundQueue | Resolved: Item added before the worker thread starts can be lost by a concurrent `Complete()` |
| 17 | Medium | Logs.Conf | Resolved: `Logging.Config` snapshots the manager. After `Logging.Complete()`, file hot-reload configures a dead manager |
| 18 | Medium | Logs.Conf | Resolved: `Log.Conf.Configure` sets `File` before success. A failure leaves `Configured == true`, and it can't be retried |
| 19 | Medium | Logs.Conf | Resolved: Hot-reload errors (`ConfigureError`/`WatchError`) are silently dropped |
| 20 | Medium | FileLog | Resolved: Rotation names use local time but are parsed as UTC, so file ages are skewed by the UTC offset |
| 21 | Medium | FileLog | Resolved: `FileDate` can throw (`new DateTime` with invalid parts), and a failed `Delete` aborts cleanup. Old logs are never pruned |
| 22 | Medium | FileLog/PathFormatter | Resolved: `{Thread.*}` tokens resolve on random thread-pool threads. The file name changes after rotation, and caches go stale |
| 23 | Medium | LogEntry | Resolved: `{dat}`/`{tim}` formatting is culture-sensitive (calendar and time separator) |
| 24 | Medium | ConsoleLog/FileLog | Resolved: Service properties mutated by Conf hot reload race with the logging thread (`Dictionary` corruption) |
| 25 | Medium | Logging | Resolved: Logs queued in memory are lost on process exit or crash unless `Complete()` is called |
| 26 | Medium | LogFormatter | Resolved: One bad item, or a `null` result from a custom formatter, replaces the whole message with an exception dump |
| 27 | Low | LogFormatter | Unbounded `IEnumerable` expansion (infinite sequences, `IQueryable`) can hang or OOM the calling thread |
| 28 | Low | LogManager | Every log call is fully formatted and queued even when nothing is enabled |
| 29 | Low | LogEntry | Undefined `LogSeverity` values cause a `KeyNotFoundException` in the formatter |
| 30 | Low | ConsoleLog | Console color/write calls can throw, aren't atomic with other console writers, and can block |
| 31 | Low | FileLog | Unconfigured `Name`/`Directory` causes a failure and a `Notify` on every flush |
| 32 | Low | LogServiceProxy | `Complete()` instantiates services that were never used |
| 33 | Low | LogServiceProxy/Conf | Configuration is order-dependent: `service.*` lines before `type` are lost |
| 34 | Low | LogEventArgs | The mutable `string[]` behind `LogList` is shared across threads |
| 35 | Low | Misc | Empty finalizers, unbounded queue, `Notify` writes to stdout, type-name collisions, `null` data |

---

## 1. `Logging.Manager` double-read races with `Complete()` (NRE thrown from `Log`). **High** (Resolved)

**File:** `Logs/Logging.cs` lines 15–29, 35–41, 110–117

```csharp
get {
    if (_Manager == null) { ... }
    return _Manager;          // second read
}
```

`Logging.Complete()` sets `Instance.Manager = null`. A thread in `Logging.Log` can see a non-null `_Manager` in the `if`, and then read `null` in `return _Manager`. `Manager.Log(...)` then throws a `NullReferenceException` out of `ILog.Info(...)` and the other log methods, which breaks the library's contract. `Enabled()`, `Logging.Subscribe`, `Logging.Event`, and the rest have the same problem.

There is a related problem. Any caller that obtained the *old* manager just before `Complete()` writes into a completed or disposed manager, and that registration (`Subscribe`, `Event +=`, `Format`) or log entry is silently lost.

**Suggested fix:**
- Read the field once into a local, using `Volatile.Read`/`Interlocked.CompareExchange`:
  ```csharp
  private LogManager Manager {
      get {
          var m = Volatile.Read(ref _Manager);
          if (m != null) return m;
          var created = new LogManager();
          return Interlocked.CompareExchange(ref _Manager, created, null) ?? created;
      }
  }
  ```
  If `CompareExchange` loses, the discarded `created` must not leak a thread. That's fine here because `LogManager` starts nothing until it is used.
- In `Complete()`, swap first and then complete: `var m = Interlocked.Exchange(ref _Manager, null); m?.Complete(); m?.Dispose();`. New callers then go straight to a fresh manager, and the old manager is not handed out while it is being torn down.
- Wrap the bodies of `Logging.Log(Logger, LogSeverity)` and `Logging.Log(Logger, LogSeverity, object[])` in `try { ... } catch (Exception ex) { Notify(ex); }`, returning `false` from the `bool` overload. This is the final safety net for the "never throw" contract.

---

## 2. `LogEvent` handler exceptions propagate out of `Log` methods. **High** (Resolved)

**File:** `Logs/LogManager.cs` line 61

`LogEvent?.Invoke(this, new LogEventArgs(entry));` runs user handlers on the caller's thread with no `try/catch`. If a handler throws:
- the exception escapes `log.Info(...)` and breaks the contract, and
- subscribers and services never receive the entry, because the code after the invoke is skipped.

If one handler in a multicast delegate throws, the later handlers are also skipped.

**Suggested fix:**
```csharp
var handler = LogEvent;
if (handler != null) {
    var args = new LogEventArgs(entry);
    foreach (LogEventHandler h in handler.GetInvocationList()) {
        try { h(this, args); }
        catch (Exception ex) { Logging.Notify(ex); }
    }
}
```

---

## 3. `Logging.For(null)` gives an `ILog` whose log methods throw. **High** (Resolved)

**Files:** `Logs/Logging.cs` line 94, `Logs/Logger.cs` line 9, `Logs/LogManager.cs` lines 53–59, `Logs/LogEntry.cs` line 50

`Logging.For(null)` succeeds. `Enabled()` correctly returns `false` for a null type. But `Data`/`Info`/... call `LogManager.Log(severity, null, data)`, which constructs `new LogEntry(logType: null, ...)`, and that throws `ArgumentNullException` out of the log method.

**Suggested fix:** In `LogManager.Log(LogSeverity, Type, object[])`, return immediately if `type == null` or `severity == LogSeverity.None`, matching the `bool` overload. Optionally map a null type to a placeholder such as `typeof(object)` or an "Unknown" name instead of dropping the message. `Logging.For(null)` could also throw `ArgumentNullException`, since it is not a log method. Keeping it non-throwing and handling `null` in `Log` is the safer path.

---

## 4. Subscription callbacks run under `lock (Lookup)`, which can deadlock. **High** (Resolved)

**File:** `Logs/LogSubscriptionCollection.cs` lines 16–33, 111–122, 10–14

`Send(LogEntry)` holds `lock (Lookup)` while it calls `ILogSubscription.Receive` (user code) synchronously. `Threshold(Type)` holds the same lock while it calls `ILogSubscription.Threshold` (user code). The same lock is taken by:
- `Enabled()` checks on every thread, through `Send(LogSeverity, Type)` → `Threshold`,
- every log call on every thread, through `Send(LogEntry)`,
- `Item_ThresholdChanged`, raised by the subscriber from whatever thread it likes,
- `Subscribe` and `Unsubscribe`.

Here is a realistic deadlock. A UI log viewer's `Receive` does `Dispatcher.Invoke(...)`, or raises `ThresholdChanged` from the UI thread. At the same moment, the UI thread calls `log.Debug()` or `log.Info(...)`. The UI thread blocks on `lock (Lookup)`, which is held by the worker thread, and the worker thread is blocked waiting for the UI thread. The same thing happens with any subscriber that waits on another thread that logs. Holding the lock also serializes all logging threads through the slowest subscriber.

**Suggested fix:**
- Make the subscriber set copy-on-write: keep an immutable array of `LogSubscriptionProxy` in a `volatile` field, and replace it under a small lock in `Add`, `Remove`, and `Clear`. `Send(LogEntry)` takes a snapshot of the array and invokes each proxy **outside** any lock.
- Compute the per-type threshold without holding the lock while calling user code. Snapshot the proxies, compute outside the lock, and store the result in a `ConcurrentDictionary<Type, LogSeverity>` tagged with a generation number (see #14).
- `Item_ThresholdChanged` then only increments the generation number or clears the concurrent dictionary, with no lock contention against user code.

---

## 5. Services run under the same lock as `Enabled()`, which can deadlock or stall the app. **High** (Resolved)

**File:** `Logs/LogServiceCollection.cs` lines 71–90 (also 20–45, 55–66)

The background action holds `Locker` while it calls every `ILogService.Log`. That includes user-provided services, `ConsoleLog` (which can block when stdout is a full pipe), and `Trace`/`Debug` listeners. `Locker` is also taken by:
- `Send(LogSeverity, Type)`, which is every `log.Enabled()`, `log.Debug()`, and similar call on every application thread,
- `SetThresholdChanged`, on the Conf hot-reload thread,
- `this[name]`, used by configuration.

Consequences:
- **Deadlock.** A custom service marshals synchronously to the UI thread (`Dispatcher.Invoke`, `SynchronizationContext.Send`) while the UI thread calls `log.Debug()` (`Enabled`). Each thread waits on the other.
- **App-wide stall.** A slow or blocked sink, such as a console pipe nobody reads, a network sink, or `Debugger` output, blocks every `Enabled()` check in the application and the config reload thread.

**Suggested fix:**
- Make the thresholds used by `Send(LogSeverity, Type)` an immutable snapshot, for example a `volatile` reference to an immutable object holding `DefaultThreshold` plus a read-only dictionary. `SetThresholdChanged` rebuilds and swaps that snapshot, and `Enabled()` reads it lock-free.
- In `Send(LogEntry)`, take a snapshot of `Set.Values` (for example, a copy-on-write array updated in `this[name]`) and call `proxy.Log(entry)` **without** holding `Locker`. Only one background thread calls services, so services still don't need to be thread-safe with respect to each other.
- Each `proxy.Log` call goes in its own `try/catch` (see #10).

---

## 6. Re-entrant logging can recurse without bound or amplify. **High** (Resolved)

**Files:** `Logs/LogManager.cs` lines 53–70, `Logs/Service/TraceLog.cs`, `Logs/Service/DebugLog.cs`, `Logs/Logging.cs` lines 43–47

Several paths re-enter logging:
- A `Logging.Event` handler or `ILogSubscription.Receive` that logs. This is common, for example a subscriber that logs its own failures, or a handler that logs "forwarded entry". The call recurses synchronously on the same thread. Monitors are re-entrant, so this is not a deadlock. It recurses until a `StackOverflowException`, which can't be caught and kills the process.
- A `TraceListener` that forwards to Domore.Logs, combined with a `trace` service, creates an endless asynchronous amplification loop. `Logging.Notify` also writes to `Trace`, so error reporting can loop too.

**Suggested fix:** Add a `[ThreadStatic] static int Depth` guard in `LogManager.Log(..., object[])`, or in `Logging.Log`. If `Depth > 0`, either drop the entry or deliver it to services only, skipping events and subscribers. Always decrement in `finally`. Document that `Trace`/`Debug` listeners must not forward back into Domore.Logs, or tag entries originating from `TraceLog` so they are ignored.

---

## 7. `FileLog` timer: bad config values can crash the process or stop file logging. **High** (Resolved)

**File:** `Logs/Service/FileLog.cs` lines 166–216

- **`LogCountLimit <= 0`.** With `0`, `lines.Count >= limit` is true immediately, so `lines` stays empty while `Queue.Count > 0`. The outer `for (;;)` spins forever on a thread-pool thread (100% CPU), holding `Locker` repeatedly. `Complete()` can still get the lock between iterations, but the timer thread never exits. A negative value makes `new List<string>(capacity: limit)` throw `ArgumentOutOfRangeException`.
- **The exception escapes `TimerCallback`.** Only `Log` and `Rotate` are inside the `try`. Any other exception (the capacity above, `FlushInterval` converted to an invalid `dueTime` in `Start()`) is an unhandled exception on a thread-pool thread, which **terminates the process**.
- **`FlushInterval` is negative or larger than `int.MaxValue` ms.** `(int)FlushInterval.TotalMilliseconds` overflows (unchecked) or is negative, and `new Timer(...)` throws. On the first log this happens inside `ILogService.Log`, after `Started = true` has already been set. File logging is then dead for good, with no retry.
- **`FlushInterval == 0`.** The callback can run before `Timer = new(...)` is assigned. `using (Timer)` then disposes `null` or the *other* timer, the original timer is leaked, and two callback chains can run against each other.

**Suggested fix:**
- Clamp the values in the setters, or at use: `LogCountLimit = Math.Max(1, value)`, `IORetryLimit >= 1`, `IORetryDelay >= 0`, and `FlushInterval` limited to `[1 ms, int.MaxValue ms]`.
- Create the timer disabled and then arm it: `Timer = new Timer(cb, null, Timeout.Infinite, Timeout.Infinite); Timer.Change(due, Timeout.Infinite);`. Better still, keep **one** timer for the lifetime of the service and re-arm it with `Change` at the end of each callback, instead of disposing and recreating it.
- Wrap the whole `TimerCallback` body in `try { ... } catch (Exception ex) { Logging.Notify(ex); } finally { re-arm unless Complete }`, so that a failure never kills the process and never stops future flushes.
- In `ILogService.Log`, set `Started = true` only after `Start()` succeeds, or catch its failure and leave `Started == false` so it is retried.

---

## 8. One failing `ILogService.Complete()` breaks shutdown and leaves a dead manager. **High** (Resolved)

**Files:** `Logs/LogServiceCollection.cs` lines 92–99, `Logs/LogManager.cs` lines 72–77, `Logs/Logging.cs` lines 110–117, `Logs/LogServiceProxy.cs` lines 84–86

- `LogServiceCollection.Complete` calls `item.Value.Complete()` for each service with no `try/catch`. A custom service that throws (or `Factory`-related failures, see #32) stops the loop, so later services, including `FileLog`, never flush their queues. **Data loss on shutdown.**
- The exception propagates out of `LogManager.Complete()`, so `Subscriptions.Complete()`/`Clear()` don't run. The `using` in `Logging.Complete()` disposes the manager, but `Instance.Manager = null;` is **skipped**. From then on, every log call goes to a **disposed** manager. `BackgroundQueue.Add` swallows `ObjectDisposedException`, so all subsequent logging is silently discarded for the rest of the process.

**Suggested fix:**
- In `LogServiceCollection.Complete`, wrap each `proxy.Complete()` in `try/catch` with `Logging.Notify`.
- In `LogManager.Complete`, run each phase in its own `try/finally`.
- In `Logging.Complete`, null or swap the manager **before** completing it, as in #1, or at least in a `finally`.

---

## 9. `Logging.Complete()` can hang forever, or deadlock from a service thread. **High** (Resolved)

**Files:** `Logs/LogServiceCollection.cs` line 93, `shared/.../BackgroundQueue.cs` lines 43–58

`Queue.Complete()` → `Thread.Join()` has **no timeout**.
- If a service is blocked (a console pipe nobody reads, network I/O, a hung `Trace` listener), `Logging.Complete()`, usually called during application shutdown, hangs the process.
- If `Logging.Complete()` is called **from the background thread**, for example by a custom service or by a `Trace` listener invoked by `TraceLog`, the thread joins itself and deadlocks for good.
- `Complete` also holds `ThreadLocker` while joining. That is harmless today, but fragile.

**Suggested fix:**
- Use `Queue.Complete(timeout)` with a bounded, configurable timeout (for example 5–10 seconds). If it expires, `Notify` and continue completing services, so `FileLog` still flushes what it has.
- Detect `Thread.CurrentThread == Thread` inside `BackgroundQueue.Complete` and skip the join in that case.
- Don't hold `ThreadLocker` while joining: copy the reference under the lock, then join outside it.

---

## 10. One throwing service drops the entry for all other services. **Medium** (Resolved)

**File:** `Logs/LogServiceCollection.cs` lines 83–89, `Logs/LogServiceProxy.cs` lines 70–82

The single queued action loops over all services. If any `proxy.Log(entry)` throws, the remaining services never see the entry. Sources of throws include a custom `ILogService`, `ConsoleLog` (see #30), `FileLog.Start()` (see #7), `LogEntry.LogData` (see #23 and #29), and the NRE in #11. `BackgroundQueue` only catches at the outer level, and it writes to Console/Trace without using `Logging.Notify`.

**Suggested fix:** Wrap the work for each service in `try { proxy.Log(entry); } catch (Exception ex) { Logging.Notify(ex); }`. Consider also guarding inside `LogServiceProxy.Log` around `Service.Log(...)` so the proxy is self-protecting.

---

## 11. `LogServiceProxy.Service` races with `Type` changes, and the old service is leaked. **Medium** (Resolved)

**File:** `Logs/LogServiceProxy.cs` lines 17–31, 49–62

- The getter reads `_Service` twice: once in the null check, then again in `return _Service`. A Conf hot reload that changes `log[x].type` sets `_Service = null` between the two reads, so `Service.Log(...)` gets `null` and throws an NRE on the background thread (see #10 for the impact).
- When `Type` changes, the old service is discarded **without calling `Complete()`**. For `FileLog`, its timer chain keeps running indefinitely, its queued lines may never be flushed at shutdown, and it is never collected.
- `Type` is read without a lock inside the getter (`Factory.Create(Type)`), so a type set concurrently can be lost.

**Suggested fix:**
- Read `_Service` into a local and return the local. Use `Volatile.Read`/`Interlocked` for publishing.
- In the `Type` setter, under `Locker`, capture the old service, set `_Service = null`, and then call `old?.Complete()` (inside `try/catch` + `Notify`) **outside** the lock. Alternatively, queue the completion onto the background queue so it is serialized with `Log` calls to that service.

---

## 12. `LogServiceConfig.Default` is published before its event handler is attached. **Medium** (Resolved)

**File:** `Logs/LogServiceConfig.cs` lines 36–51

```csharp
_Default = @default;                                   // published (fast path is lock-free)
_Default.ThresholdChanged += Default_ThresholdChanged; // attached afterwards
```

Another thread on the lock-free fast path can see `_Default != null` and set `Default.Threshold` before the handler is attached. `DefaultThresholdChanged` then never fires, and `LogServiceCollection.DefaultThreshold` stays `None`. `Enabled()` returns `false` even though the proxy would log. Code that checks `if (log.Debug()) log.Debug(...)` loses messages until some other threshold change happens.

**Suggested fix:** Attach the handler to the local first, then publish:
```csharp
var d = new LogTypeConfig();
d.ThresholdChanged += Default_ThresholdChanged;
Volatile.Write(ref _Default, d);
```

---

## 13. The aggregate threshold used by `Enabled()` is wrong for mixed configurations. **Medium** (Resolved)

**File:** `Logs/LogServiceCollection.cs` lines 20–45, 71–80, compared with `Logs/LogServiceProxy.cs` line 76

Each proxy decides with `Config[name].Threshold ?? Config.Default.Threshold`. The aggregate instead computes, for each type name, the minimum **only across services that set that type explicitly**, and it ignores the defaults of the other services.

Example:
- `log[file].config.default.severity = debug`
- `log[console].config[Foo].severity = error`

Then `TypeThreshold["Foo"] = Error`, so `Enabled(Debug)` for `Foo` returns **false**, but the file service *would* log `Foo` at `Debug`. Callers that guard with `if (log.Debug())` silently lose messages.

**Suggested fix:** Compute the effective threshold per service, then take the minimum across services:
```
effective(service, name) = service.Config[name].Threshold ?? service.Config.Default.Threshold
TypeThreshold[name]      = min over services of effective(service, name), ignoring null/None
```
Also recompute when a proxy is added, and publish the result as an immutable snapshot (see #5).

---

## 14. `LogSubscriptionProxy` threshold cache can keep a stale value permanently. **Medium** (Resolved)

**Files:** `Logs/LogSubscriptionProxy.cs` lines 15–35, `Logs/LogSubscriptionCollection.cs` lines 10–33

A thread in `ThresholdCache.GetOrAdd(type, factory)` calls `Agent.Threshold(type)` and gets the **old** value. Before it stores the result, the agent raises `ThresholdChanged`, and `ThresholdCache.Clear()` runs. `GetOrAdd` then inserts the old value after the clear. The collection-level `Thresholds` dictionary has the same race, because it is filled from `proxy.Threshold`. The stale value stays until the next `ThresholdChanged`, which may never come.

**Suggested fix:** Use a generation counter. Increment it in `Agent_ThresholdChanged` (with `Interlocked.Increment`) before clearing. Cache `(generation, severity)` pairs, and treat any entry whose generation doesn't match the current one as a miss. Do the same at the collection level. Alternatively, don't cache at the proxy level and rely on a single generation-tagged cache in the collection.

---

## 15. Unsubscribing leaves the proxy attached to the subscription. **Medium** (Resolved)

**File:** `Logs/LogSubscriptionCollection.cs` lines 64–95, `Logs/LogSubscriptionProxy.cs` lines 10–13, 52–54

`LogSubscriptionProxy`'s constructor does `Agent.ThresholdChanged += Agent_ThresholdChanged`. `Remove()` and `Clear()` detach the *collection's* handler from the proxy, but they never call `proxy.Complete()`. So the agent keeps a reference to the orphaned proxy. Each subscribe/unsubscribe cycle leaks one proxy, and its `ConcurrentDictionary`, onto the subscriber's event. `LogManager.Complete()` does call `Subscriptions.Complete()` first, but explicit `Unsubscribe` does not.

**Suggested fix:** Call `proxy.Complete()` in `Remove()`. In `Clear()`, call it for each proxy. Wrap the calls in `try/catch`, because they run the user's `remove` accessor.

---

## 16. `BackgroundQueue` can lose the first item to a concurrent `Complete()`. **Medium** (Resolved)

**File:** `shared/Domore.Sharing/Threading/BackgroundQueue.cs` lines 43–58, 66–96

`Add` enqueues the item **first** and only then starts the worker thread. Consider this interleaving:
1. Thread A, `Add`: `Collection.Add(item)` succeeds, and `Thread` is still `null`.
2. Thread B, `Complete`: `CompleteAdding()`, sees `Thread == null`, and returns immediately. `Logging.Complete` then completes the services (`FileLog` flushes, but it hasn't received the item) and **disposes the collection**.
3. Thread A starts the worker. `Take()` throws `ObjectDisposedException`, and the worker exits.

The entry is lost. This affects short-lived apps and tests that log from one thread and complete from another. There is a similar edge case: `ex is InvalidOperationException && Collection.IsAddingCompleted`. If the collection is disposed between the throw and the check, `IsAddingCompleted` throws `ObjectDisposedException` out of `Add`, and so out of the log call.

**Suggested fix:**
- Start the worker thread (under `ThreadLocker`) **before** adding the first item. Alternatively, have `Complete` take `ThreadLocker` and drain the remaining items synchronously if no thread was ever started.
- In `Add`, catch every exception (or test `ex is InvalidOperationException` without touching `Collection` again) and return. `Add` sits on the log path, so it must never throw.

---

## 17. Conf hot reload targets a dead manager after `Logging.Complete()`. **Medium** (Resolved)

**Files:** `Logs/Logging.cs` lines 86–87, `Logs.Conf/Logs/LogConfFile.cs` line 15, `Logs.Conf/Logs/Log.cs`

`Logging.Config` returns `new { Log = Instance.Manager }`, which is a **snapshot** of the current manager. `LogConfFile` passes it once to `ConfFile` as `Target`. After `Logging.Complete()`:
- the next log call creates a **new, unconfigured** `LogManager`, so all logging stops,
- the file watcher keeps applying changes to the **old, disposed** manager,
- `Log.Conf.Configure(path)` returns `false` because `File != null`, so the application can't re-apply the configuration.

`Conf.Contain(...).ConfigureLogging()` callers have the same snapshot problem, but they configure only once, so it's less severe.

**Suggested fix:**
- Make `Logging.Config` return a stable object whose `Log` property resolves the **current** manager on every access, for example `internal sealed class LoggingConfig { public LogManager Log => Instance.Manager; }`, cached in a static field.
- Have `Log.Conf` remember the path and re-apply it when a new manager is created. For example, `Logging` could expose an internal `ManagerCreated` hook, used through `InternalsVisibleTo("Domore.Logs.Conf")`, that calls `File.Configure()`. Alternatively, document that `Logging.Complete()` is terminal when `Log.Conf` is used, and dispose/reset `Log.Conf.File` inside `Complete()`.

---

## 18. `Log.Conf.Configure` cannot be retried after a failure, and `Configured` is reported too early. **Medium** (Resolved)

**File:** `Logs.Conf/Logs/Log.cs` lines 10–21

```csharp
File = new LogConfFile(path);  // published
File.Configure(watch: true);   // may throw
```

- If `Configure` throws (missing directory → `FileSystemWatcher.Path` throws `ArgumentException`, bad conf syntax, `null` path → `File.ReadAllText(null)`), `File` is still set. `Configured` returns `true`, and every later `Configure` call returns `false`. The application can never fix this at runtime, and the `FileSystemWatcher`/`ConfFile` object is leaked.
- `Configured` becomes `true` *before* the configuration is applied. Another thread that checks `Log.Conf.Configured` and starts logging can run with the configuration only partly applied.

**Suggested fix:** Build the object in a local, configure it, and publish it only on success. Dispose it on failure:
```csharp
var file = new LogConfFile(path);
try { file.Configure(watch: true); }
catch { file.Dispose(); throw; }   // or Notify + return false
File = file;
return true;
```
Validate `path` (`ArgumentNullException`). This is not a log method, so throwing here is acceptable. Pick one behavior, throw or return `false`, and document it.

---

## 19. Hot-reload errors are silently swallowed. **Medium** (Resolved)

**File:** `Logs.Conf/Logs/LogConfFile.cs`

`ConfFile` raises `ConfigureError` and `WatchError` for errors that occur during reload. `LogConfFile` subscribes to neither, so a typo in the config file or a watcher buffer overflow has no visible effect. Logging simply stops reflecting the file, and no diagnostic is produced.

**Suggested fix:** In the `LogConfFile` constructor, subscribe `Agent.ConfigureError += (s, e) => Logging.Notify(e.GetException());` and do the same for `WatchError`. On `WatchError`, consider re-arming the watcher by calling `Configure(watch: false)` and then `Configure(watch: true)`. Unsubscribe in `Dispose`.

---

## 20. Rotation file names use local time but are parsed as UTC. **Medium** (Resolved)

**File:** `Logs/Service/FileLog.cs` lines 37–40, 81, 110

`FileDateName()` uses `DateTime.Now` (local). `FileDate()` constructs `new DateTime(..., DateTimeKind.Utc)`, and `Rotate()` compares it with `DateTime.UtcNow`. So each file's computed age is off by the UTC offset, which is up to ±14 hours. This gets worse around DST changes, where file names can also sort non-monotonically.

**Suggested fix:** Use `DateTime.UtcNow` in `FileDateName()`. Alternatively, keep local names but parse them with `DateTimeKind.Local` and compare with `DateTime.Now`. UTC is preferred.

---

## 21. Rotation cleanup can throw, and then old logs are never pruned. **Medium** (Resolved)

**File:** `Logs/Service/FileLog.cs` lines 42–82, 110–130

- `FileDate` uses `int.TryParse` for each part, but then calls `new DateTime(year, month, day, ...)`, which throws `ArgumentOutOfRangeException` for impossible values (month 13, day 32, hour 25, and so on). Any file in the directory that matches `name_*.ext` with a 19-character pseudo-date, such as a user's own file or a copy, makes **every** rotation throw before any deletion happens. Old logs then accumulate without limit. `int.TryParse` also accepts signs and whitespace (`"+1"`, `" 1"`).
- A failing `item.File.Delete()` (a file locked by a viewer or antivirus scanner) throws and aborts the rest of the cleanup.
- If `MoveTo` fails and `nextPath` doesn't exist, the exception is rethrown. That's fine, because the caller catches it, but the age/size cleanup is skipped for that cycle.
- Minor: `items.Sum(...)` inside the `while` loop is O(n²), and the active file is not counted toward `TotalSizeLimit`.

**Suggested fix:**
- Parse with `DateTime.TryParseExact(date, "yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt)`, and return `null` on failure.
- Wrap each `Delete()` in `try/catch` (log with `Notify`, then continue).
- Compute the total size once, and subtract as files are deleted.
- Consider running the pruning even when `MoveTo` fails.

---

## 22. `{Thread.*}` path tokens are resolved on random thread-pool threads. **Medium** (Resolved)

**Files:** `Logs/Service/FileLog.cs` lines 18–35, 109, `shared/Domore.Sharing/IO/PathFormatter.cs` lines 46–50

`FileInfo`/`DirectoryInfo` are evaluated lazily inside `TimerCallback`, which runs on an arbitrary thread-pool thread. So `{Thread.Name}` and `{Thread.ManagedThreadId}` reflect the pool thread, not the application. After rotation, `Rotate()` sets `_FileInfo = null` but **not** `_FileName`, `_FileNameWithoutExtension`, or `_FileExtension`. The next file can then get a different name (a different pool thread ID), while the rotation search pattern and `FileDate` prefix still use the old cached name. Rotation and cleanup stop matching the files that are actually being written.

**Suggested fix:** Resolve the formatted path once, when `Name` or `Directory` is set (or on the first `ILogService.Log` call, on the caller's thread), and store the result. Alternatively, remove the thread-dependent tokens from what `FileLog` supports, and document it. In `Rotate()`, reset all derived caches together, or better, derive them from a single immutable "resolved path" object.

---

## 23. `{dat}`/`{tim}` formatting is culture-sensitive. **Medium** (Resolved)

**File:** `Logs/LogEntry.cs` lines 24–27

`DateTime.ToString("yyyy-MM-dd")` and `ToString("HH:mm:ss.fff")` use the **current culture** of the background thread:
- `:` is the culture's time separator, so some cultures produce `12.34.56.789`.
- `yyyy` uses the culture's **calendar**. For example, `th-TH` (Buddhist calendar) gives `2569`, and `ar-SA` gives a Hijri year.

Log timestamps then differ between machines and can't be parsed reliably. On some exotic cultures and calendars, the call can throw (date out of the calendar's range), which ties into #10.

**Suggested fix:** Pass `CultureInfo.InvariantCulture` to all four `ToString` calls.

---

## 24. Service properties mutated by hot reload race with the logging thread. **Medium** (Resolved)

**Files:** `Logs/Service/ConsoleLog.cs` lines 22–45, `Logs/Service/FileLog.cs` lines 210–216

Conf exposes `log[x].service.*`. For example, `log[f].service.flush interval = ...` is used in the tests, so `ConfFile`'s reload thread writes to service objects while the background thread reads them:
- `ConsoleLog.Foreground`/`Background` are plain `Dictionary` objects. A reload that sets `log[console].service.foreground[warn] = ...` mutates the dictionary while the background thread calls `TryGetValue`. On .NET Framework, concurrent `Dictionary` mutation and reads can corrupt the buckets and cause **infinite loops**, or throw.
- `FileLog`'s numeric and `TimeSpan` properties are non-volatile. On 32-bit, `long`/`TimeSpan` reads can be **torn** (`FileSizeLimit`, `TotalSizeLimit`, `FileAgeLimit`, `FlushInterval`).

**Suggested fix:**
- `ConsoleLog`: replace the dictionaries with immutable snapshots, or a `ConcurrentDictionary`, or a fixed `ConsoleColor[]` indexed by severity, swapped atomically. Alternatively, wrap access in a lock that is held briefly, never while writing to the console.
- `FileLog`: back the 64-bit settings with `Interlocked.Read`/`Exchange`, or read them under `Locker` in `TimerCallback`. Alternatively, copy all settings into an immutable settings object at the start of each flush.

---

## 25. Pending logs are lost on process exit or crash without `Complete()`. **Medium** (Resolved)

**Files:** `Logs/Logging.cs`, `shared/.../BackgroundQueue.cs` (`IsBackground = true`), `Logs/Service/FileLog.cs` (timer-based flushing)

Entries sit in the `BackgroundQueue` and then in the `FileLog` queue for up to `FlushInterval` (2.5 seconds by default). If the application exits without calling `Logging.Complete()`, all of them are lost. That includes the most important case: `log.Critical(ex)` in an unhandled-exception handler just before the process dies.

**Suggested fix:** In the `Logging` static initializer, subscribe to `AppDomain.CurrentDomain.ProcessExit` (and optionally `UnhandledException`) and call a bounded-time `Complete()` (see #9), guarded by `try/catch`. Alternatively, add a `Logging.Flush(TimeSpan)` API and document it. Consider flushing `FileLog` immediately for `Critical`/`Error` entries.

---

## 26. One bad item replaces the whole message. **Medium** (Resolved)

**File:** `Logs/LogFormatter.cs` lines 19–38, 50–58

The single `try/catch` around `data.SelectMany(Format).ToArray()` means:
- a throwing `ToString()` on one argument,
- a custom formatter (`Logging.Format`) that throws or **returns `null`** (`SelectMany` over `null` throws),
- or an `IEnumerable` modified during enumeration

all cause the **entire** message, including the other arguments, to be replaced with an exception dump.

**Suggested fix:** Format each top-level item, and each enumerable element, inside its own `try/catch`. On failure, emit something like `"<format error: {ex.GetType().Name}: {ex.Message}>"` for that item only, and `Notify` the exception. Treat a `null` result from a custom formatter as "fall back to the default formatting".

---

## 27. Unbounded `IEnumerable` expansion. **Low** (Resolved)

**File:** `Logs/LogFormatter.cs` lines 29–36

Every non-string `IEnumerable` is enumerated eagerly on the **caller's** thread:
- an infinite or generator sequence hangs `log.Info(...)` or runs it out of memory,
- an `IQueryable` executes a database query,
- a large collection allocates heavily on hot paths.

This happens even when nothing is enabled (#28).

**Suggested fix:** Cap expansion with a configurable maximum (for example 100 items), followed by an `"… (truncated)"` line. Consider not expanding `IQueryable`, or expanding only `ICollection`/arrays by default.

---

## 28. Every log call is fully formatted and queued even when nothing listens. **Low** (Resolved)

**File:** `Logs/LogManager.cs` lines 53–70, `Logs/Logger.cs` line 17

`LogManager.Log(severity, type, data)` always runs `Formatter.Format(data)`: `ToString`, enumerable expansion, splitting, and allocations. If any service exists, it also enqueues a closure, whether or not any threshold would accept the entry. `Debug(...)` calls in hot loops therefore cost the full price in production configurations that log at `Info`.

**Suggested fix:** At the top of `Log(..., object[])`, call `if (!Log(severity, type)) return;`. This depends on #13 being fixed first, because otherwise the gate drops messages that a service would accept.

---

## 29. Undefined severities throw in the formatter. **Low** (Resolved)

**File:** `Logs/LogEntry.cs` line 23

`Sev[EntrySeverity]` throws `KeyNotFoundException` for a value like `(LogSeverity)7`, which passes every `<=` threshold check. On the background thread, this aborts delivery to all services (#10).

**Suggested fix:** Use `Sev.TryGetValue(EntrySeverity, out var s) ? s : ((int)EntrySeverity).ToString()`. Optionally, clamp or validate the severity in `Logger.Data`.

---

## 30. `ConsoleLog` robustness. **Low** (Resolved)

**File:** `Logs/Service/ConsoleLog.cs` lines 34–46

- Getting and setting `Console.ForegroundColor`/`BackgroundColor` can throw (`IOException` for an invalid handle, or `PlatformNotSupportedException` on browser/WASM, iOS, and Android). Even `Console.WriteLine` is skipped then.
- The color change, write, and restore sequence isn't atomic with respect to other code in the process that writes to the console, so colors bleed into application output and the reverse.
- `Console.WriteLine` blocks when stdout is a pipe that isn't being read. Combined with #5, that stalls the whole application.

**Suggested fix:** Catch color failures separately and fall back to a plain `WriteLine`. Skip colors when `Console.IsOutputRedirected`. Take a lock on a shared console object around set, write, and restore. The real blocking fix is #5 (don't hold shared locks while writing).

---

## 31. `FileLog` with no `Name`/`Directory` fails on every flush. **Low** (Resolved)

**File:** `Logs/Service/FileLog.cs` lines 27–35, 133–145

If `log[x].type = file` is configured without `service.directory`, `Environment.ExpandEnvironmentVariables(null)` throws `ArgumentNullException`. If `service.name` is missing, the path resolves to the directory itself, and `FileInfo.Create()` throws `UnauthorizedAccessException`, which is not retried. Every 2.5 seconds the batch is dropped and `Notify` writes a stack trace to Console, Debug, and Trace.

**Suggested fix:** Supply sensible defaults, for example `Directory = "."` or `{LocalApplicationData}\<AppDomain.FriendlyName>`, and `Name = "{AppDomain.FriendlyName}.log"`. Alternatively, detect the incomplete configuration once, `Notify` once, and treat the service as disabled until it is reconfigured.

---

## 32. `LogServiceProxy.Complete()` instantiates services that were never used. **Low** (Resolved)

**File:** `Logs/LogServiceProxy.cs` lines 84–86

`Complete()` goes through the `Service` getter, which **creates** the service (reflection plus the user constructor) during shutdown if it was never used, just to call `Complete()` on it. This wastes work and is another source of shutdown exceptions (#8).

**Suggested fix:** `var s = Volatile.Read(ref _Service); s?.Complete();` inside `try/catch` + `Notify`.

---

## 33. Configuration is order-dependent. **Low** (Resolved)

**File:** `Logs/LogServiceProxy.cs` lines 49–62

If a conf file lists `log[f].service.name = ...` **before** `log[f].type = file`, the `Service` getter instantiates `Factory.Create("f")`, which fails, so a `None` service is used. The `service.*` values are applied to that service and then discarded when `type` resets `_Service`. The resulting `FileLog` has no name or directory (#31). The same thing happens on hot reload if the order in the file changes.

**Suggested fix:** Document that `type` must come first. Alternatively, buffer the `service.*` settings on the proxy and apply them to whichever service is created. Or, more simply, don't create the service lazily from `Service` when `_Type` hasn't been set explicitly; `Notify` instead.

---

## 34. The mutable `string[]` behind `LogList` is shared across threads. **Low** (Resolved)

**Files:** `Logs/LogEntry.cs` lines 46, 67, `Logs/LogEvent.cs` line 45

`ILogEntry.LogList` returns the raw `string[]`. Event handlers and subscribers run on the caller thread, and they can cast it back to `string[]` and mutate it while the background thread formats the same entry for services. That is a data race, and it can change what gets written to the log.

**Suggested fix:** Expose `Array.AsReadOnly(EntryList)` (cached), or an iterator, through `ILogEntry.LogList`.

---

## 35. Miscellaneous. **Low**

1. **Empty finalizers** (`LogManager`, `LogServiceCollection`, `LogConfFile`, and `BackgroundQueue`): `Dispose(false)` does nothing, so the finalizers only add GC cost (finalization queue, objects promoted to Gen 1). Remove them, and drop the `Dispose(bool)` pattern where nothing unmanaged is owned.
2. **Unbounded `BackgroundQueue`:** a slow sink lets memory grow without limit. Consider a bounded capacity with a drop-oldest or drop-newest policy, and a counter of dropped entries reported through `Notify`. `Add` must never block or throw on the log path.
3. **`Logging.Notify` writes to `Console.Out`:** for CLI tools whose stdout is machine-parsed (JSON, pipes), internal logging errors corrupt the output. Prefer `Console.Error`, or make the sinks configurable.
4. **Type-name keys:** services key configuration and thresholds by `Type.Name`. Types with the same short name in different namespaces share settings, and generic types appear as ``List`1``. Consider `FullName` with a short-name fallback, and document the behavior.
5. **`log.Info(null)`** binds `null` to `object[] data`. `LogManager.Log` then silently logs nothing, while `LogFormatter.Format(null)` was clearly written to produce `""`. Pick one behavior. Passing it through as a single empty line is probably the expected one.
6. **`LogManager.Complete()`** sets `LogEvent = null`, but `Subscribe`, `Event +=`, and `Format` still succeed on a completed manager (the window in #1). After the swap in #1, consider ignoring or notifying registrations made on a completed manager.
7. **`LogServiceFactory`** catches the cast failure with `try { (ILogService)obj }`. Use `obj as ILogService` instead. Also note that the conf file can instantiate **any** type that has a public parameterless constructor (`Type.GetType` + `Activator.CreateInstance`). Treat log configuration files as trusted input, and document that.
8. **`LogConf`** has only a static method and a private constructor. Make it a `static class`.
