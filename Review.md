# Domore.Logs outstanding review issues

This document contains the three remaining issues in the current working tree, based on commit `ef0a07c`. The original review was performed on 2026-10-02 at `830f962c92523328345a6424a4fb4f3fc0477875`; the follow-up review compared the `logs` branch with `origin/dev`.

Scope: `source/Domore.Logs`, its imported `Domore.Sharing` sources, logging tests and sample, and the adjacent `Domore.Logs.Conf` integration. Original issue IDs are preserved. New issues 24–26 correspond to follow-up branch-review findings 2–4. All findings below are **Open**.

Severity: **High** means a hang, application failure, lost messages, or destructive behavior under the stated trigger; **Medium** means a reliability or correctness problem.

## Validation

- The current logging suite has 172 cases per target. On Windows, `net462`, `net8.0`, and `net10.0` each passed 171 cases and skipped the Unix-root case: 513 passes and three skips total. Passing tests do not cover the outstanding failures below.
- Both logging projects built without warnings or errors for all nine declared targets: `net40`, `net45`, `net462`, `net48`, `netstandard2.0`, `netcoreapp3.1`, `net6.0`, `net8.0`, and `net10.0`.
- Issues 25–26 were reproduced in an isolated harness on .NET 8 and .NET 10 during the branch review. Both were reconfirmed on .NET 10 against the current sources while updating this document.
- Issue 21 is established by source inspection; no destructive out-of-memory stress test was performed.
- Runtime validation was on Windows. Unix runtime behavior and runtime tests on the other declared frameworks were not independently exercised.

## Findings

### 21. Medium — Both logging queues can grow without a bound

**Location:** [BackgroundQueue.cs](shared/Domore.Sharing/Threading/BackgroundQueue.cs), lines 11 and 70–73; [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs), lines 14 and 379; [LogManager.cs](source/Domore.Logs/Logs/LogManager.cs), the entry creation and service dispatch in `Log()`.

The service queue uses an unbounded `BlockingCollection<Action>`, and the file writer uses an unbounded `ConcurrentQueue<string>`. A slow or blocked service holds up the single service worker while producers continue retaining formatted entries. Slow file I/O can independently accumulate strings in the file queue. `LogCountLimit` limits batch size rather than backlog. Entries below every configured service threshold are also queued because filtering occurs in service delivery. Sustained producer/consumer imbalance has no memory limit or overload policy. This is a source-confirmed capacity issue; no destructive memory stress test was run.

**Fix plan:** Define configurable queue capacity, overload behavior, and observable drop/backlog counters. Apply filtering before enqueue where it preserves the intended configuration semantics, and account for large message sizes as well as item counts. Test overload using a gated slow service and verify the documented policy.

### 25. Medium — Reentrant service replacement applies settings to the discarded instance

**Origin:** Follow-up branch-review finding 3; related to the service-replacement lifecycle.

**Location:** [LogServiceProxy.cs](source/Domore.Logs/Logs/LogServiceProxy.cs), lines 22–27, 48–61, and 83–104.

When a `Log()` callback reconfigures its own proxy, the `Type` setter records a pending replacement, but the `Service` getter still returns the old instance. A public `Conf.Contain(...).Configure(Logging.Config, key: "")` call that sets both `log[reentrant].type` and `log[reentrant].service.option` writes `Option` to the old service. After the callback, `ApplyPendingType()` completes that instance and the replacement is later created with default settings. A property absent on the old type can instead be silently ignored by configuration.

**Reproduction:** Reconfigure the type and set `Option = configured` inside the old service's log callback. After delivery returns, the replacement's `Option` is null. The existing replacement regression does not exercise callback-driven service configuration.

**Fix plan:** Defer the complete reconfiguration consistently, including service-specific property assignments, or provide a configuration mechanism that targets the replacement while deferring its delivery.

**Required regression coverage:** Configure the replacement through the public configuration API from the old callback. Verify its type and service-specific settings, delivery of the next entry, and exactly-once completion of the old instance. Include a property present only on the replacement type.

### 26. Medium — One configuration-file reload can span two logging managers

**Origin:** Follow-up branch-review finding 4; related to configuration reload and manager handoff.

**Location:** [LogConfFile.cs](source/Domore.Logs.Conf/Logs/LogConfFile.cs), lines 7–13 and 26–31; [Logging.cs](source/Domore.Logs/Logs/Logging.cs), the `Config` property and completion handoff; [ConfPopulator.cs](source/Domore.Conf/Conf/ConfPopulator.cs), lines 73–90 and 103–119.

`ConfPopulator` traverses `Target.Log` separately for each configuration pair. The getter resolves `Logging.Config` afresh on each traversal, and the whole application does not lease a manager. If logging completes midway through a reload, earlier settings reach the retired manager while later settings reach the new one. A service type can land on the old manager while its threshold lands on the new proxy, leaving a partially configured service.

**Reproduction:** Apply a file containing a service type, a service `Option` property, and a default severity. Pause the `Option` setter, complete logging, then resume the file application. The current proxy has its fallback type `service` instead of the configured assembly-qualified type, while receiving the final threshold. Completion reports `Type not found [service]`. The committed reload tests cover sequential restart and reload, including a real watcher event, but do not interleave shutdown with a reload.

**Fix plan:** Resolve one target per whole file application and lease that manager against retirement. If shutdown wins before application begins, apply the whole file to the next manager.

**Required regression coverage:** Gate a configuration setter, complete logging during the reload, then resume it. Verify all settings reach one manager, the active service enables the configured severity and receives entries, and retired services complete exactly once. Cover both explicit reload and watcher-driven application.
