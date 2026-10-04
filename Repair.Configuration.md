# Configuration repair: C16 and reload C18

The built-in logging configuration paths now apply every exact
`log[service].type` assignment before other pairs. The ordered population path
uses parsed key parts, partitions the fully expanded pairs in one pass, and
keeps the original order within each partition. Included pairs and duplicate
assignments therefore retain their precedence. Generic `Conf.Configure` calls
and third-party `IConfContainer` implementations continue to apply pairs in
content-provider order, which is now documented on `IConf.Configure` and
`Conf.Configure<T>`.

Both built-in entry points run the expanded population through
`Logging.Configure`, keeping one manager lease across the entire application.
`LogConfFile` reports watcher configuration and watcher failures through the
protected `Logging.Notify` stderr path, and detaches both handlers when disposed.
The watcher remains attached to its configuration file across logging manager
restarts; completion does not use a global event to clear it. Explicit
configuration failures still propagate to their caller.

## Red/green evidence

On Windows with .NET SDK 10.0.112, focused `net8.0` regressions were run in the
configuration worktree.

- Before the ordering change, `ServicePropertiesBeforeTypeAreApplied` failed
  because the configured service label was null instead of `retained`.
- Before the ordering change, `LogConfConfigurePartitionsExpandedIncludesStably`
  failed because an included service label was null instead of `retained-last`.
  The test includes duplicate type assignments and then reloads the included
  file with another service setting before a type assignment.
- With the diagnostic handlers temporarily replaced by no-ops,
  `WatcherErrorsReportToStandardErrorAndHandlersAreRemovedOnDispose` failed
  both stderr assertions: neither `watcher failure` nor `invalid-severity` was
  reported. Restoring the protected `Logging.Notify` calls made it pass.
- With `ConfigureLogging` temporarily restored to direct
  `confContainer.Configure(Logging.Config, ...)`,
  `BuiltInConfigureLoggingHoldsOneManagerLeaseUntilAllPairsApply` found no lease
  on the retiring manager; completion returned and completed the service while
  its setter was gated. The `Logging.Configure` wrapper made the gate test pass,
  including the final threshold value observed during service completion.
- The generic source-order and third-party-container cases assert that a
  service property before its type assignment is discarded, preserving their
  existing source-order behavior. The built-in test also places
  `log[ordered].service.type` before `log[ordered].type` and confirms only the
  exact service type pair moves ahead of settings.

After the repairs:

```text
dotnet test tests/Domore.Logs.Tests/Domore.Logs.Tests.csproj -f net8.0 --no-restore -v minimal --filter "FullyQualifiedName~LoggingConfigurationTest|FullyQualifiedName~LogConfFileTest|FullyQualifiedName~BranchComparisonTest.ServicePropertiesBeforeTypeAreApplied|FullyQualifiedName~BranchComparisonTest.NewConfigurationSurvivesOldManagerShutdown"
Passed: 17, Failed: 0

dotnet test tests/Domore.Conf.Tests/Domore.Conf.Tests.csproj -f net8.0 --no-restore -v minimal
Passed: 373, Failed: 0

git diff --check
Passed
```

The logging filter includes the existing reload and leased-application
shutdown cases, the new watcher diagnostics and disposal case, both source
ordering compatibility controls, included configuration and duplicate
assignments, and the session-safe watcher check. Full framework and target
matrix validation remains part of final integration.
