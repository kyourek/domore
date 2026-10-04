using Domore.Conf.Logs;
using Domore.Logs;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

internal static class Program {
    private static string Marker => Environment.GetEnvironmentVariable("DOMORE_LOGS_EXIT_PROBE_PATH");

    private static void Configure<T>() where T : ILogService {
        LogConf.ConfigureLogging($@"
            log[probe].type = {typeof(T).AssemblyQualifiedName}
            log[probe].config.default.severity = info
        ");
    }

    private static int Main() {
        var scenario = Environment.GetEnvironmentVariable("DOMORE_LOGS_EXIT_PROBE_SCENARIO");
        if (string.Equals(scenario, "budget", StringComparison.Ordinal)) {
            return RunBudgetProbe();
        }
        Configure<FlushLogService>();
        Logging.For(typeof(Program)).Info("accepted process-exit entry");
        return 0;
    }

    private static int RunBudgetProbe() {
        BlockingLogService.Reset();
        Configure<BlockingLogService>();
        Logging.For(typeof(Program)).Info("first pending manager");
        if (Logging.Complete(TimeSpan.Zero)) {
            return 1;
        }
        if (BlockingLogService.FirstCompleteEntered.Wait(TimeSpan.FromSeconds(10)) == false) {
            return 2;
        }

        Configure<BlockingLogService>();
        Logging.For(typeof(Program)).Info("second pending manager");
        if (Logging.Complete(TimeSpan.Zero)) {
            return 3;
        }
        if (BlockingLogService.SecondCompleteEntered.Wait(TimeSpan.FromSeconds(10)) == false) {
            return 4;
        }

        Directory.CreateDirectory(Environment.GetEnvironmentVariable("DOMORE_LOGS_EXIT_PROBE_READY_PATH"));
        return 0;
    }
}

public sealed class FlushLogService : ILogService {
    public void Log(string name, string data, LogSeverity severity) {
        File.AppendAllText(Environment.GetEnvironmentVariable("DOMORE_LOGS_EXIT_PROBE_PATH"),
            "message:" + data + Environment.NewLine);
    }

    public void Complete() {
        File.AppendAllText(Environment.GetEnvironmentVariable("DOMORE_LOGS_EXIT_PROBE_PATH"),
            "complete" + Environment.NewLine);
    }
}

public sealed class BlockingLogService : ILogService {
    private static readonly ManualResetEventSlim Blockers = new();
    private static readonly object Writer = new();
    private static int CompleteCount;
    public static ManualResetEventSlim FirstCompleteEntered { get; } = new();
    public static ManualResetEventSlim SecondCompleteEntered { get; } = new();

    public static void Reset() {
        Interlocked.Exchange(ref CompleteCount, 0);
        FirstCompleteEntered.Reset();
        SecondCompleteEntered.Reset();
        Blockers.Reset();
    }

    public void Log(string name, string data, LogSeverity severity) { }

    public void Complete() {
        var count = Interlocked.Increment(ref CompleteCount);
        lock (Writer) {
            File.AppendAllText(Environment.GetEnvironmentVariable("DOMORE_LOGS_EXIT_PROBE_PATH"),
                "completion-entered" + Environment.NewLine);
        }
        if (count == 1) {
            FirstCompleteEntered.Set();
        }
        else {
            SecondCompleteEntered.Set();
        }
        Blockers.Wait();
    }
}
