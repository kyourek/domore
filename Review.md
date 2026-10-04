# Domore.Logs outstanding review issues

This document contains the remaining issue in the current working tree, based on commit `5b72a70`. The original review was performed on 2026-10-02 at `830f962c92523328345a6424a4fb4f3fc0477875`; the follow-up review compared the `logs` branch with `origin/dev`.

Scope: `source/Domore.Logs`, its imported `Domore.Sharing` sources, logging tests and sample, and the adjacent `Domore.Logs.Conf` integration. Original issue IDs are preserved. The finding below is **Open**.

Severity: **High** means a hang, application failure, lost messages, or destructive behavior under the stated trigger; **Medium** means a reliability or correctness problem.

## Validation

- The current logging suite has 185 cases per target. On Windows, `net462`, `net8.0`, and `net10.0` each passed 184 cases and skipped the Unix-root case: 552 passes and three skips total. Passing tests do not cover the outstanding capacity issue below.
- Both logging projects built without warnings or errors for all nine declared targets: `net40`, `net45`, `net462`, `net48`, `netstandard2.0`, `netcoreapp3.1`, `net6.0`, `net8.0`, and `net10.0`.
- Issue 21 is established by source inspection; no destructive out-of-memory stress test was performed.
- Runtime validation was on Windows. Unix runtime behavior and runtime tests on the other declared frameworks were not independently exercised.

## Findings

### 21. Medium — Both logging queues can grow without a bound

**Location:** [BackgroundQueue.cs](shared/Domore.Sharing/Threading/BackgroundQueue.cs), lines 11 and 70–73; [FileLog.cs](source/Domore.Logs/Logs/Service/FileLog.cs), lines 14 and 379; [LogManager.cs](source/Domore.Logs/Logs/LogManager.cs), the entry creation and service dispatch in `Log()`.

The service queue uses an unbounded `BlockingCollection<Action>`, and the file writer uses an unbounded `ConcurrentQueue<string>`. A slow or blocked service holds up the single service worker while producers continue retaining formatted entries. Slow file I/O can independently accumulate strings in the file queue. `LogCountLimit` limits batch size rather than backlog. Entries below every configured service threshold are also queued because filtering occurs in service delivery. Sustained producer/consumer imbalance has no memory limit or overload policy. This is a source-confirmed capacity issue; no destructive memory stress test was run.

**Fix plan:** Define configurable queue capacity, overload behavior, and observable drop/backlog counters. Apply filtering before enqueue where it preserves the intended configuration semantics, and account for large message sizes as well as item counts. Test overload using a gated slow service and verify the documented policy.
