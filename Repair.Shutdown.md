# Shutdown repairs: C13, C20, shutdown C21, process-exit C18

Shutdown now admits and drains every accepted queue item, detaches logging managers under a short lock, and retires each manager on a dedicated background worker. A retirement waits for active leases, drains the queue, completes already-created services and subscriptions, then disposes the manager. Completion captures pending retirements at entry and uses one monotonic deadline across all captured records. `Logging.Complete()` remains an unbounded full flush; `Logging.Complete(TimeSpan)` accepts nonnegative or exactly -1 ms timeouts and returns `false` at expiry while retirement continues.

Manager-owner context flows with ordinary execution-context propagation. A completion request from a flowed lease or retiring callback defers its wait, and a callback from an old manager cannot detach a newer session. Retirement diagnostics keep the old manager owner context. Completion records use monitor state rather than kernel wait handles, so repeated sessions do not leave event handles for garbage collection. Service completion visits only instantiated services; a failed attempted service construction is retained and reported by explicit completion without retrying arbitrary factory code.

The shared `BackgroundQueue` keeps Logs independent, writes fallback diagnostics to stderr, and accepts an optional contained diagnostic callback. A concurrent first completion starts a worker for queued accepted work if the adding thread has not started it yet. Queue worker and queue join interruptions are contained and resumed. Normal process exit calls full retirement with one five-second deadline, including retirements already pending when the exit callback begins.

## Red/green evidence

- **C13 queue startup:** the original concurrent-first-completion probe was red on baseline (`accepted = 1`, `executed = 0`). The gated regression now asserts an item is already accepted while worker startup is held, then verifies completion executes it exactly once and repeated completion/disposal are safe.
- **Queue interruption safety:** temporarily removing the `ThreadInterruptedException` recovery made the worker test fail because it abandoned the next accepted action, and made the join test fail with `ThreadInterruptedException` escaping completion. Restoring the recovery made both tests pass. A held logging lease also verifies that interrupting its retirement waiter does not bypass lease waiting or skip accepted work.
- **C20 unused services:** temporarily restoring unconditional service creation made `CompletingUnusedConfiguredServiceDoesNotCreateIt` fail (`Created = 1`, `Completed = 1`). The repaired proxy leaves never-used services unconstructed, while preserving explicit error reporting for a factory attempt that failed.
- **C20 flowed ownership:** temporarily ignoring the flowed manager owner made `CompletionOnTaskWithFlowedManagerLeaseDoesNotWaitForThatLease` fail because the child waited for its parent lease. Removing the old-owner/new-session check made the subscription-removal regression detach the new manager. Both regressions pass with the ownership checks restored.
- **C18 process exit:** temporarily removing the process-exit registration made the real child-process flush test fail because no log marker was written. The standalone probe now verifies the normal exit flush and measures one five-second deadline while two pending managers have blocked service completion callbacks. Both subprocess tests pass on net10.0.
- **Retirement wait resources:** injecting a wait-handle field into retirement records made the repeated-session invariant fail. With monitor-backed record state, 64 create/retire cycles complete and leave no pending retirement or record-owned wait handle.
- The timed completion API did not exist in the base revision, so a behavioral red run was impractical. Its regression covers zero, finite, and infinite waits, invalid negative values, a blocked service callback, timeout followed by a full flush, and inclusion of prior pending retirements.

## Validation

- `dotnet build source/Domore.Logs/Domore.Logs.csproj --no-restore -v minimal`: passed all nine library targets (`net40`, `net45`, `net462`, `net48`, `netstandard2.0`, `netcoreapp3.1`, `net6.0`, `net8.0`, `net10.0`).
- Shutdown and lifetime focused tests: 10/10 passed on net8.0 and 10/10 passed on net462. The net10.0 shutdown/path focused set passed 16/16, including the two real child-process checks.
- Net10.0 Logs suite, excluding only `ServicePropertiesBeforeTypeAreApplied` because this worktree predates the separately reviewed C16 configuration commit: 276 passed, 1 skipped, 0 failed. The complete Logs suite is to be rerun after integration with that reviewed configuration repair.
- `git diff --check` passed after the final documentation and test updates.

The five-second exit deadline bounds waiting by the exit handler; arbitrary service callbacks cannot be forcibly stopped. Timed completion returns at its deadline while its independent background retirement continues safely.
