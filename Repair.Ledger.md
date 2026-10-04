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
| C14–C15 | formatting | Pending | Pending |
| C11–C12, C19, C22 | callback safety | Pending | Pending |
| C17, C23 | service synchronization | Pending | Pending |
| C13, C20, shutdown C21, process exit C18 | shutdown | Pending | Pending |
| C16, reload C18 | configuration | Pending | Pending |
| capacity C21 | queue limits | Pending | Pending |

## Final integration

Pending: all nine library targets; logging and Conf suites on Windows net462,
net8.0, and net10.0; comparison controls; clean tracked worktree; branch audit.

The no-throw contract covers ordinary managed failures inside library-provided
`ILog` calls. It does not cover argument evaluation before invocation or runtime
termination. Arbitrary callbacks cannot be safely preempted. Forced termination
cannot guarantee flushing.
