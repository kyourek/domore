# Callback-safety repairs

This branch addresses comparison findings C11–C12, C19, and C22.

## Changes

- Wrapped every public `ILog` emission and enabled check in an ordinary-exception boundary. Emissions contain manager acquisition, threshold evaluation, formatting, dispatch, and lease-release failures; enabled checks return `false` after a failure. A null log type is treated as disabled.
- Added a flowing callback scope for formatter, event, subscriber, service, factory, and diagnostic callbacks. Nested public logging and enabled checks are suppressed in the callback and in work started through ordinary execution-context propagation. The .NET Framework targets use logical `CallContext`; newer targets use `AsyncLocal`. Manager ownership flows with the scope so callback-triggered completion cannot target a replacement manager.
- Checked enablement before invoking the formatter. Event handlers, subscriptions, and services are isolated so one failing destination does not block later destinations.
- Routed diagnostics to stderr and protected diagnostic formatting and output with a separate flowing recursion guard. A failing `ToString`, writer, or nested diagnostic is contained.
- Attached the default threshold-change handler before publishing the default configuration and made the reference volatile for safe publication.

## Red/green evidence

- The baseline null-type matrix exposed six throwing emission methods. After the ingress boundary, all emission methods return normally and all checks return `false`.
- The parameterized `TypeDelegator.Name` fault reached from a configured service threshold caused all 12 emission/check entry points to fail when the outer ingress catches were temporarily removed. With the catches restored, all 12 pass and checks return `false`.
- Removing the formatter callback scope made the formatter delegate, `ToString`, and throwing-formatter feedback tests fail: nested checks returned `true`, and the event count rose to three instead of one. The guarded implementation passes all three tests, including a `Task.Run` nested call and guard restoration after the formatter throws.
- Removing the early enabled gate caused a disabled message's formatter callback to run once instead of zero times. Restoring the gate makes the focused test pass.
- Removing the diagnostic-depth check made recursive diagnostic reporting write twice; an asynchronous recursive writer also blocked trying to re-enter the stderr writer. The guarded writer test now completes with one diagnostic write.
- Temporarily removing callback scopes from subscriber, factory, and service paths made nested checks return `true` and generated extra events/service calls. The green tests verify isolation, later-destination delivery, and guard restoration.
- The publication-window race has no deterministic injection seam. The regression verifies handler-driven threshold updates after first publication and checks that `_Default` has the CLR volatile field modifier; the handler-before-publication invariant is also evident in the construction order.

## Validation

- Focused callback, no-throw, gate, feedback, publication, and threshold-path tests: net8.0 passed (29 in the first focused run; 16 in the finalized gate/formatter/threshold run) and net462 passed (18 in the finalized callback-flow run).
- Full `Domore.Logs.Tests`: net8.0 passed 219, skipped 1; net462 passed 219, skipped 1.
- `Domore.Logs` and its Conf dependencies built for all nine library targets: net40, net45, net462, net48, netstandard2.0, netcoreapp3.1, net6.0, net8.0, and net10.0, with zero warnings or errors.

The full suites print expected injected callback exceptions to stderr. They also observed the pre-existing file-service timer reporting a transient locked-file `IOException`; both suites completed successfully. Tests sharing file-service state were run sequentially.
