using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Diagnostics;

[TestFixture]
internal sealed class ProcessProxyTest {
    private static bool IsWindows {
#if NETFRAMEWORK
        get => true;
#else
        get => OperatingSystem.IsWindows();
#endif
    }

    [Test]
    public async Task Start_DrainsStandardOutputAndErrorBeforeReturning() {
        var longLine = new string('x', 5000);
        var scriptPath = WriteScript(
            windows: [
                "@echo stdout-one",
                "@echo(",
                $"@echo {longLine}",
                "@set /p _=stdout-tail<nul",
                "1>&2 <nul set /p _=stderr-tail",
                "1>&2 echo("
            ],
            unix: [
                "printf 'stdout-one\\n\\n'",
                $"printf '%s\\n' '{longLine}'",
                "printf 'stdout-tail'",
                "printf 'stderr-tail' >&2"
            ]);
        try {
            var proxy = CreateProxy(scriptPath);
            await proxy.Start();
            var output = Lines(proxy, ProcessOutputKind.StandardOutput);
            var error = Lines(proxy, ProcessOutputKind.StandardError);

            using (Assert.EnterMultipleScope()) {
                Assert.That(proxy.Started, Is.True);
                Assert.That(proxy.Running, Is.False);
                Assert.That(
                    proxy.ExitCode,
                    Is.EqualTo(0),
                    string.Join(Environment.NewLine, error));
                Assert.That(output, Is.EqualTo(new[] {
                    "stdout-one",
                    string.Empty,
                    longLine,
                    "stdout-tail"
                }));
                Assert.That(error, Is.EqualTo(new[] {
                    "stderr-tail"
                }));
            }
        }
        finally {
            File.Delete(scriptPath);
        }
    }

    [Test]
    public async Task Start_CancellationTerminatesProcessBeforeClearingRunningState() {
        var scriptPath = WriteScript(
            windows: [
                ":loop",
                "@goto loop"
            ],
            unix: [
                "while :; do :; done"
            ]);
        using var cancellation = new CancellationTokenSource();
        var proxy = CreateProxy(scriptPath);
        var start = default(Task);
        var processId = default(int?);
        try {
            start = proxy.Start(cancellationToken: cancellation.Token);
            Assert.That(proxy.Running, Is.True);
            processId = proxy.ProcessID;
            Assert.That(processId, Is.Not.Null);
            cancellation.Cancel();

            var completed = await Task.WhenAny(start, Task.Delay(TimeSpan.FromSeconds(10)));
            Assert.That(completed, Is.SameAs(start));
            Assert.CatchAsync<OperationCanceledException>(async () => await start);

            using (Assert.EnterMultipleScope()) {
                Assert.That(proxy.Running, Is.False);
                Assert.That(
                    SpinWait.SpinUntil(
                        () => HasExited(processId.Value),
                        TimeSpan.FromSeconds(5)),
                    Is.True);
            }
        }
        finally {
            cancellation.Cancel();
            if (processId is int id) {
                KillProcess(id);
            }
            if (start is not null) {
                await Task.WhenAny(start, Task.Delay(TimeSpan.FromSeconds(5)));
            }
            File.Delete(scriptPath);
        }
    }

    [Test]
    public async Task Start_ProvidesEndOfInputToProcess() {
        var scriptPath = WriteScript(
            windows: [
                "@set /p _=",
                "@echo after-input"
            ],
            unix: [
                "IFS= read -r _",
                "printf 'after-input\\n'"
            ]);
        var proxy = CreateProxy(scriptPath);
        var start = proxy.Start();
        try {
            var completed = await Task.WhenAny(start, Task.Delay(TimeSpan.FromSeconds(10)));
            Assert.That(completed, Is.SameAs(start));
            await start;
            Assert.That(Lines(proxy, ProcessOutputKind.StandardOutput), Is.EqualTo(new[] {
                "after-input"
            }));
        }
        finally {
            await proxy.Kill();
            await Task.WhenAny(start, Task.Delay(TimeSpan.FromSeconds(5)));
            File.Delete(scriptPath);
        }
    }

    [Test]
    public async Task Start_CancellationCompletesWhenChildProcessHoldsOutput() {
        var markerPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.marker");
        var childPath = WriteScript(
            windows: [
                ":wait",
                "@ping -n 2 127.0.0.1 >nul 2>&1",
                $"@if exist \"{markerPath}\" goto wait"
            ],
            unix: [
                $"while [ -e '{markerPath}' ]; do sleep 0.1; done"
            ]);
        var scriptPath = WriteScript(
            windows: [
                "@echo started",
                $"@cmd /d /q /c \"\"{childPath}\"\""
            ],
            unix: [
                "printf 'started\\n'",
                $"/bin/sh '{childPath}'"
            ]);
        File.WriteAllText(markerPath, string.Empty);
        using var cancellation = new CancellationTokenSource();
        var proxy = CreateProxy(scriptPath);
        var start = proxy.Start(cancellationToken: cancellation.Token);
        try {
            Assert.That(
                SpinWait.SpinUntil(
                    () => proxy.Stream?.LineItems.Count > 0,
                    TimeSpan.FromSeconds(10)),
                Is.True);
            cancellation.Cancel();

            var completed = await Task.WhenAny(start, Task.Delay(TimeSpan.FromSeconds(10)));
            Assert.That(completed, Is.SameAs(start));
            Assert.CatchAsync<OperationCanceledException>(async () => await start);
            Assert.That(proxy.Running, Is.False);
        }
        finally {
            cancellation.Cancel();
            File.Delete(markerPath);
            await Task.WhenAny(start, Task.Delay(TimeSpan.FromSeconds(30)));
            // Give the child process time to observe the marker removal and release the pipes.
            await Task.Delay(TimeSpan.FromSeconds(3));
            File.Delete(scriptPath);
            File.Delete(childPath);
        }
    }

    [Test]
    public async Task Start_ReportsNonZeroExitCode() {
        var scriptPath = WriteScript(
            windows: ["@exit /b 7"],
            unix: ["exit 7"]);
        try {
            var proxy = CreateProxy(scriptPath);
            await proxy.Start();
            using (Assert.EnterMultipleScope()) {
                Assert.That(proxy.ExitCode, Is.EqualTo(7));
                Assert.That(proxy.ExitTime, Is.Not.Null);
            }
        }
        finally {
            File.Delete(scriptPath);
        }
    }

    [Test]
    public async Task Start_ThrowsWhenAlreadyStarted() {
        var scriptPath = WriteScript(
            windows: ["@echo once"],
            unix: ["printf 'once\\n'"]);
        try {
            var proxy = CreateProxy(scriptPath);
            await proxy.Start();
            Assert.ThrowsAsync<InvalidOperationException>(async () => await proxy.Start());
        }
        finally {
            File.Delete(scriptPath);
        }
    }

    [Test]
    public void Start_PreCanceledTokenDoesNotStartProcess() {
        var proxy = CreateProxy("unused");
        Assert.CatchAsync<OperationCanceledException>(
            async () => await proxy.Start(cancellationToken: new CancellationToken(true)));
        using (Assert.EnterMultipleScope()) {
            Assert.That(proxy.Started, Is.False);
            Assert.That(proxy.ProcessID, Is.Null);
            Assert.That(proxy.Stream, Is.Null);
        }
    }

    [Test]
    public void Environment_ReportsReadOnlyAndRejectsMutation() {
        var source = new Dictionary<string, string> {
            ["DOMORE_PROCESS_PROXY_TEST"] = "initial"
        };
        IProcessProxy proxy = new ProcessProxy("unused", environment: source);
        var environment = proxy.Environment;

        using (Assert.EnterMultipleScope()) {
            Assert.That(environment.IsReadOnly, Is.True);
            Assert.That(environment["DOMORE_PROCESS_PROXY_TEST"], Is.EqualTo("initial"));
            Assert.Throws<NotSupportedException>(() => environment.Add("NEW_VALUE", "value"));
            Assert.Throws<NotSupportedException>(() => environment["DOMORE_PROCESS_PROXY_TEST"] = "changed");
            Assert.Throws<NotSupportedException>(() => environment.Remove("DOMORE_PROCESS_PROXY_TEST"));
            Assert.Throws<NotSupportedException>(() => environment.Clear());
        }
    }

    [Test]
    public void Start_FailedLaunchDoesNotPreventRetry() {
        var missingFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var proxy = new ProcessProxy(missingFile);

        Assert.ThrowsAsync<Win32Exception>(async () => await proxy.Start());
        using (Assert.EnterMultipleScope()) {
            Assert.That(proxy.Started, Is.False);
            Assert.That(proxy.Running, Is.False);
            Assert.That(proxy.StartTime, Is.Null);
        }

        Assert.ThrowsAsync<Win32Exception>(async () => await proxy.Start());
    }

    private static ProcessProxy CreateProxy(string scriptPath) {
        return new ProcessProxy(
            fileName: IsWindows
                ? Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe"
                : "/bin/sh",
            arguments: IsWindows
                ? $"/d /q /c \"\"{scriptPath}\"\""
                : $"\"{scriptPath}\"");
    }

    private static string[] Lines(ProcessProxy proxy, ProcessOutputKind kind) {
        return ((IProcessStream)proxy.Stream).LineItems
            .OfType<ProcessStreamOutput>()
            .Where(item => item.Kind == kind)
            .Select(item => item.Line)
            .ToArray();
    }

    private static string WriteScript(string[] windows, string[] unix) {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}{(IsWindows ? ".cmd" : ".sh")}");
        File.WriteAllLines(path, IsWindows ? windows : unix, Encoding.ASCII);
        return path;
    }

    private static bool HasExited(int processId) {
        try {
            using var process = Process.GetProcessById(processId);
            return process.HasExited;
        }
        catch (ArgumentException) {
            return true;
        }
    }

    private static void KillProcess(int processId) {
        if (HasExited(processId)) {
            return;
        }
        using var process = Process.GetProcessById(processId);
        if (process.HasExited == false) {
            try {
                process.Kill();
            }
            catch (InvalidOperationException) when (process.HasExited) {
            }
            catch (Win32Exception) when (process.HasExited) {
            }
            process.WaitForExit();
        }
    }
}
