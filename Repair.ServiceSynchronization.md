# Service synchronization repair

Repairs comparison findings C17 and C23 on `agent/logs3/service-synchronization`.

## Changes

- Console foreground and background maps now use `ConcurrentDictionary`, with atomic lazy initialization and replacement. Existing indexed `Conf` updates still work.
- Console writes share a lock because color state belongs to the process-wide console. Color reads, color changes, and restoration are best-effort; a color failure no longer prevents writing the message.
- File retry, rotation-size, retention-size, and retention-age settings use the same lock as file operations. This protects wide values and keeps settings updates ordered with flush and rotation work.
- Retention captures archive lengths, isolates each deletion failure, and continues to later candidates. A failed deletion stays in the retained set and remains part of the byte total; failed paths are skipped after their first attempt so cleanup cannot loop forever. Reporting a deletion error is also contained.
- The existing actual-parent selection, offset-aware date parsing, direct append, and rotation after final flush remain intact.

## Red/green evidence

The focused regressions first failed for their intended reasons on Windows:

- Console color maps were `Dictionary` instances, and injected getter failure raised `IOException` before output.
- The locked archive raised `IOException` from `FileInfo.Delete`, wrapped by reflection, and stopped retention before later eligible files were removed.
- With the former auto-properties temporarily restored, `WideSettingsWaitForTheFileServiceLock` failed because the setter completed while the service lock was held.
- With the old uncontained color setter behavior temporarily restored, the setter/restoration regression raised its injected foreground `IOException` before output.

After the fixes, the complete file and console service fixtures passed on each requested runtime:

| Target | Result |
|---|---:|
| net462 | 53 passed |
| net8.0 | 53 passed |
| net10.0 | 53 passed |

The focused net8.0 run covering the new regressions also passed all 7 tests. The net8.0 regression set includes concurrent color-map updates during logging, `Conf` indexed color assignment, color getter/setter/restoration failures, locked-file retention with byte-limit accounting, and property access blocking on the file-service lock.

The logging library built successfully for all nine targets with zero warnings and zero errors: net40, net45, net462, net48, netstandard2.0, netcoreapp3.1, net6.0, net8.0, and net10.0.

Commands were run from Windows and serialized to avoid overlap with tests sharing filesystem state. Full logging/Conf suites remain part of integration validation.
