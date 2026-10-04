using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Logs;

partial class LoggingTest {
    private static string ExitProbeRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        "..", "..", "..", "..", ".."));

    private static string ExitProbeDll {
        get {
            var configuration = new DirectoryInfo(AppContext.BaseDirectory).Name;
            return Path.Combine(ExitProbeRoot, "out", "tests", "bin", "Domore.Logs.ProcessExitProbe",
                configuration, "Domore.Logs.ProcessExitProbe.dll");
        }
    }

    private static Process StartExitProbe(string scenario, string marker) {
        var start = new ProcessStartInfo {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            Arguments = "\"" + ExitProbeDll + "\"",
            WorkingDirectory = ExitProbeRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.EnvironmentVariables["DOMORE_LOGS_EXIT_PROBE_PATH"] = marker;
        start.EnvironmentVariables["DOMORE_LOGS_EXIT_PROBE_READY_PATH"] = marker + ".ready";
        start.EnvironmentVariables["DOMORE_LOGS_EXIT_PROBE_SCENARIO"] = scenario;
        return Process.Start(start);
    }

    private static void WaitForExitProbeReady(Process process, string readyDirectory,
        Task<string> stdout, Task<string> stderr) {
        var found = SpinWait.SpinUntil(() => Directory.Exists(readyDirectory) || process.HasExited,
            TimeSpan.FromSeconds(30));
        Assert.That(found, Is.True, "The exit-probe child did not publish its readiness directory.");
        if (Directory.Exists(readyDirectory) == false && process.HasExited) {
            Task.WaitAll(stdout, stderr);
            Assert.Fail("The exit-probe child exited before publishing its readiness marker. Exit code " +
                process.ExitCode + ". Output: " + stdout.Result + Environment.NewLine + stderr.Result);
        }
    }

    [Test]
    public void NormalProcessExitDrainsLoggingWithinBoundedShutdown() {
#if !NET10_0
        Assert.Ignore("The real process-exit probe runs on net10.0.");
#endif
        var directory = Path.Combine(Path.GetTempPath(), "domore-process-exit", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var marker = Path.Combine(directory, "exit.log");
        try {
            using var process = StartExitProbe("flush", marker);
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            if (process.WaitForExit((int)TimeSpan.FromSeconds(60).TotalMilliseconds) == false) {
                process.Kill();
                process.WaitForExit();
                Assert.Fail("The normal process-exit probe exceeded its 60 second bound.");
            }
            Task.WaitAll(stdout, stderr);
            Assert.That(process.ExitCode, Is.Zero, "Child output: " + stdout.Result + Environment.NewLine + stderr.Result);
            var lines = File.ReadAllLines(marker);
            Assert.That(lines, Does.Contain("message:accepted process-exit entry"));
            Assert.That(lines, Does.Contain("complete"));
        }
        finally {
            if (Directory.Exists(directory)) {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Test]
    public void ProcessExitUsesOneFiveSecondBudgetForAllPendingManagers() {
#if !NET10_0
        Assert.Ignore("The real process-exit budget probe runs on net10.0.");
#endif
        var directory = Path.Combine(Path.GetTempPath(), "domore-process-exit", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var marker = Path.Combine(directory, "budget.log");
        var readyDirectory = marker + ".ready";
        try {
            using var process = StartExitProbe("budget", marker);
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            WaitForExitProbeReady(process, readyDirectory, stdout, stderr);
            var readyTicks = Stopwatch.GetTimestamp();
            if (process.WaitForExit((int)TimeSpan.FromSeconds(20).TotalMilliseconds) == false) {
                process.Kill();
                process.WaitForExit();
                Assert.Fail("The child process did not honor the bounded exit callback.");
            }
            Task.WaitAll(stdout, stderr);
            var elapsedTicks = Stopwatch.GetTimestamp() - readyTicks;
            var elapsed = TimeSpan.FromSeconds((double)elapsedTicks / Stopwatch.Frequency);
            var lines = File.ReadAllLines(marker);
            Assert.That(process.ExitCode, Is.Zero, "Child output: " + stdout.Result + Environment.NewLine + stderr.Result);
            Assert.Multiple(() => {
                Assert.That(lines.Count(line => line == "completion-entered"), Is.EqualTo(2),
                    "Two managers should have pending, blocked service completions at process exit.");
                Assert.That(elapsed, Is.GreaterThanOrEqualTo(TimeSpan.FromSeconds(4.5)));
                Assert.That(elapsed, Is.LessThan(TimeSpan.FromSeconds(7.5)),
                    "All pending managers share the process-exit five-second deadline.");
            });
        }
        finally {
            if (Directory.Exists(directory)) {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
