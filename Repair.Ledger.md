# logs3 repair ledger

`logs3` starts at `dev` (`830f962`) and merges `logs` (`339131c`) in
`Merge logs into logs3`. `dev` is kept unchanged. The original branch comparison
is preserved in [Review.Comparison.md](Review.Comparison.md).

Implementation is delegated to GPT-6-Luna with Max reasoning in separate
worktrees. The primary agent reviews and merges each repair. Worker evidence
files record focused red/green runs and any cases where deterministic red
reproduction is impractical.

## Baseline

The 23 comparison probes were restored from the original comparison export.
The service ordering probe now invokes built-in `ConfigureLogging`, matching
the intended guarantee; direct generic configuration retains source order.

On Windows / .NET SDK 10.0.112, the `logs3` baseline comparison run on net8.0
had **12 passed and 11 failed**. The failures reproduced null-type ingress,
event recursion, disabled-call formatting, argument isolation, null formatter
fallback, null params, enumeration bounds, invariant timestamps, undefined
severity, service ordering, and the first-worker startup race. The twelve
preservation controls passed. Evidence: `logs3-baseline-comparisons.trx` in the
logging test project's ignored `TestResults` directory.

## Repairs

| Findings | Task | Reviewed commit(s) | Validation |
|---|---|---|---|
| C14–C15 | formatting | `1c912e2`, merged `4c70613` | [Formatting evidence](Repair.Formatting.md); 21 focused checks on each required runtime |
| C11–C12, C19, C22 | callback safety | `05f05cf`, merged `059b48e` | [Callback evidence](Repair.CallbackSafety.md); full baseline suite green on net462/net8.0 |
| C17, C23 | service synchronization | `818c2db`, merged `8572d75` | [Service evidence](Repair.ServiceSynchronization.md); 53 service checks on each required runtime |
| C13, C20, shutdown C21, process exit C18 | shutdown | `d6f42ed`, `0090222`, merged `74ba99c` | [Shutdown evidence](Repair.Shutdown.md); lifetime gates, process-exit subprocesses, nine-target build |
| C16, reload C18 | configuration | `67e87fa`, merged `90a9b4c` | [Configuration evidence](Repair.Configuration.md); 17 logging checks and 373 Conf checks on net8.0 |
| capacity C21 | queue limits | `8626e0f`, merged `a204d82` | [Queue evidence](Repair.QueueLimits.md); 19 capacity and 56 related checks on each required runtime |

## Final integration

After the first three reviewed merges, the comparison and formatter repair
fixtures on net8.0 had **29 passed and 2 failed**. The remaining intended red
cases were C13 (first-worker startup) and C16 (service ordering). The public
null-params event regression passed with the combined ingress/formatting fixes.
Evidence: `logs3-first-wave-comparisons.trx` in ignored `TestResults`.

After merging shutdown and configuration, the complete comparison fixture
passed **25/25** on net8.0, including the original 23 probes and two queue
interruption checks. Evidence: `logs3-retirement-config-comparisons.trx`.

The complete integrated suites passed on Windows, serially in Release
configuration, using .NET SDK 10.0.112:

| Suite | Runtime | Passed | Skipped | Failed |
|---|---|---:|---:|---:|
| Conf | net462 | 359 | 14 | 0 |
| Conf | net8.0 | 373 | 0 | 0 |
| Conf | net10.0 | 373 | 0 | 0 |
| Logs | net462 | 300 | 3 | 0 |
| Logs | net8.0 | 300 | 3 | 0 |
| Logs | net10.0 | 302 | 1 | 0 |

Evidence: `logs3-final-conf-net462.trx`, `logs3-final-conf-net8.trx`, and
`logs3-final-conf-net10.trx` in the Conf test project's ignored `TestResults`.
The existing ConfContentProviderTest subprocess probes are explicitly skipped
below net8.0. Logging evidence is `logs3-final-logs-net462.trx`,
`logs3-final-logs-net8.trx`, and `logs3-final-logs-net10.trx` in its ignored
`TestResults`. Logs skips the Unix-only absolute-path case on Windows and runs
the two real process-exit probes only on net10.0. Both subprocess probes passed.
The full logging runs include all comparison controls and capacity repairs.

The six suites total **2,007 passed, 21 expected skips, and zero failures**.

`dotnet build source/Domore.Logs.Conf/Domore.Logs.Conf.csproj --configuration Release`
built Logs, Logs.Conf, and Conf on all nine library targets with **zero warnings
and zero errors**: net40, net45, net462, net48, netstandard2.0, netcoreapp3.1,
net6.0, net8.0, and net10.0. Each test run used `dotnet test` on its listed
project/runtime with `--configuration Release` and a named TRX logger.

Branch audit confirmed that all six `agent/logs3/*` heads are ancestors of
`logs3`, with implementation integrated at `a204d82`. `dev` remains exactly
`830f962c92523328345a6424a4fb4f3fc0477875`. Worker worktrees are clean; the final
documentation commit leaves the main tracked worktree clean. The package
[README](source/Domore.Logs/README.md) documents the public contracts,
configuration keys, queue status API, and intentional completion validation.

The no-throw contract covers ordinary managed failures inside library-provided
`ILog` calls. It does not cover argument evaluation before invocation or runtime
termination. Arbitrary callbacks cannot be safely preempted. Forced termination
cannot guarantee flushing.
