# Formatting repair evidence (C14–C15)

`LogFormatter` now isolates failures by argument. A throwing custom formatter or `ToString` contributes its exception text while later arguments are still formatted. Enumerator `Current` failures consume one item from the budget and do not stop later items; failures from `GetEnumerator`, `MoveNext`, or `Dispose` are contained to that enumerable. A custom formatter that returns `null` falls back to normal object formatting. A null params array remains one empty line, and null strings in a formatter result are normalized to empty lines.

Enumerable expansion defaults to 100 items. `Logging.EnumerableItemLimit` configures a positive limit; the current value is captured at the start of each enumerable expansion. A truncation line is added only when another item exists beyond the limit. The limit cannot interrupt a blocked user `MoveNext` or formatter callback.

`LogEntry` uses invariant culture for UTC and local timestamp tokens, which keeps the calendar Gregorian and digits stable. Known severity abbreviations are preserved; `None` and undefined numeric values use the invariant lowercase enum representation instead of throwing.

## Red/green evidence

- Before the production changes, the focused net8.0 regression run compiled and executed. Eight of nine tests failed in the intended behavior paths: one argument or a broken enumerator discarded remaining messages; a null formatter result became an `ArgumentNullException` dump; expansion had no truncation marker or configurable limit; timestamps used the active `ar-SA` Hijri calendar; and `None`/undefined severities threw `KeyNotFoundException`. The null params-array control passed.
- The public `ILog.Info((object[])null)` event regression was separately captured red on this isolated branch because its base `LogManager` returns before formatting null data. The callback-safety repair removes that early return; primary integration will rerun this regression after both branches are merged.
- After the changes, the 10 formatter/entry implementation-scope regressions passed on net8.0. The separate public event regression is excluded pending callback-branch integration.
- Existing formatter, entry, and severity tests plus those 10 regressions passed: 21/21 on net8.0, 21/21 on net462, and 21/21 on net10.0.
- `Domore.Logs.csproj` built all nine targets (net40, net45, net462, net48, netstandard2.0, netcoreapp3.1, net6.0, net8.0, and net10.0) with zero warnings and zero errors.

The disabled-call enablement gate is implemented with the ingress repair (C11–C12); these formatting checks exercise formatting behavior directly and do not claim that ingress fix.
