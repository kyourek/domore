using System;
using System.Diagnostics;
using System.Threading;

namespace Domore.Logs;

internal static class LogQueueDiagnostics {
    private const string OverflowMessage =
        "One or more log messages were rejected because a logging queue is over capacity.";

    private static int Reporting;
    private static long NextReportTimestamp;

    private static void Report(object message) {
        var reporting = Interlocked.CompareExchange(ref Reporting, 1, 0) != 0;
        if (reporting) {
            return;
        }
        try {
            var now = Stopwatch.GetTimestamp();
            var nextReport = Interlocked.Read(ref NextReportTimestamp);
            var reportingTooSoon = now < nextReport;
            var reportIntervalSet = new Func<bool>(() =>
                Interlocked.CompareExchange(ref NextReportTimestamp,
                                            now + Stopwatch.Frequency,
                                            nextReport) == nextReport);
            if (reportingTooSoon || !reportIntervalSet()) {
                Interlocked.Exchange(ref Reporting, 0);
                return;
            }
            var reportQueued = ThreadPool.QueueUserWorkItem(_ => {
                try {
                    Logging.Notify(message);
                }
                catch {
                    /*
                     * Queue overload reporting is best-effort and runs off the caller.
                     */
                }
                finally {
                    Interlocked.Exchange(ref Reporting, 0);
                }
            });
            if (reportQueued == false) {
                Interlocked.Exchange(ref Reporting, 0);
            }
        }
        catch {
            Interlocked.Exchange(ref Reporting, 0);
        }
    }

    public static void ReportOverflow() =>
        Report(OverflowMessage);

    public static void ReportFailure(Exception exception) =>
        Report(exception ?? (object)"A logging queue could not accept a message.");
}
