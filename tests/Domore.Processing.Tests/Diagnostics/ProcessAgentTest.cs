using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Diagnostics;

[TestFixture]
internal sealed class ProcessAgentTest {
    private static bool IsWindows {
#if NETFRAMEWORK
        get => true;
#else
        get => OperatingSystem.IsWindows();
#endif
    }

    [Test]
    public async Task Start_LaunchesConfiguredProcessAndDrainsOutputBeforeCompleting() {
        var workingDirectory = CreateWorkingDirectory();
        var scriptPath = WriteScript(
            workingDirectory,
            windows: [
                "@echo %DOMORE_PROCESS_AGENT_VALUE%",
                "@cd",
                "1>&2 echo error-line"
            ],
            unix: [
                "printf '%s\\n' \"$DOMORE_PROCESS_AGENT_VALUE\"",
                "pwd",
                "printf '%s\\n' 'error-line' >&2"
            ]);
        try {
            var agent = CreateAgent(scriptPath, workingDirectory);
            agent.Environment = new Dictionary<string, string> {
                ["DOMORE_PROCESS_AGENT_VALUE"] = "environment-line"
            };
            agent.Synchronize = false;

            var callbackCount = 0;
            IProcessProxy proxy = null;
            var start = agent.Start(created => {
                callbackCount++;
                proxy = created;
            }, CancellationToken.None);

            Assert.That(proxy, Is.Not.Null);
            Assert.That(proxy.Stream, Is.Not.Null);
            await CompleteWithin(start, TimeSpan.FromSeconds(10));

            var standardOutput = Lines(proxy, ProcessOutputKind.StandardOutput);
            var standardError = Lines(proxy, ProcessOutputKind.StandardError);
            using (Assert.EnterMultipleScope()) {
                Assert.That(callbackCount, Is.EqualTo(1));
                Assert.That(standardOutput, Has.Length.EqualTo(2));
                Assert.That(standardOutput[0], Is.EqualTo("environment-line"));
                Assert.That(
                    PathsEqual(standardOutput[1], workingDirectory),
                    Is.True,
                    $"Expected working directory '{workingDirectory}', got '{standardOutput[1]}'.");
                Assert.That(standardError, Is.EqualTo(new[] {
                    "error-line"
                }));
            }
        }
        finally {
            DeleteDirectory(workingDirectory);
        }
    }

    [TestCase(true, true)]
    [TestCase(false, false)]
    public void Start_MarshalsCollectionChangesAccordingToSynchronizeSetting(bool synchronize,
                                                                             bool expectedOnOwnerThread) {
        var workingDirectory = CreateWorkingDirectory();
        var scriptPath = WriteScript(
            workingDirectory,
            windows: ["@echo context-line"],
            unix: ["printf 'context-line\\n'"]);
        using var synchronizationContext = new PumpingSynchronizationContext();
        try {
            var agent = CreateAgent(scriptPath, workingDirectory);
            if (synchronize == false) {
                agent.Synchronize = false;
            }

            var previousContext = SynchronizationContext.Current;
            IProcessProxy proxy = null;
            Task start;
            try {
                SynchronizationContext.SetSynchronizationContext(synchronizationContext);
                start = agent.Start(created => proxy = created, onErrorCaught: null, CancellationToken.None);
            }
            finally {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }

            Assert.That(proxy, Is.Not.Null);
            Assert.That(proxy.Stream, Is.Not.Null);
            var notificationThread = 0;
            var notificationCount = 0;
            ((INotifyCollectionChanged)proxy.Stream.LineItems).CollectionChanged += (_, _) => {
                Interlocked.Exchange(
                    ref notificationThread,
                    Thread.CurrentThread.ManagedThreadId);
                Interlocked.Increment(ref notificationCount);
            };

            synchronizationContext.PumpUntil(start, TimeSpan.FromSeconds(10));

            var lines = Lines(proxy, ProcessOutputKind.StandardOutput);
            using (Assert.EnterMultipleScope()) {
                Assert.That(lines, Is.EqualTo(new[] {
                    "context-line"
                }));
                Assert.That(
                    Interlocked.CompareExchange(ref notificationThread, 0, 0) == synchronizationContext.OwnerThread,
                    Is.EqualTo(expectedOnOwnerThread));
                Assert.That(
                    Interlocked.CompareExchange(ref notificationCount, 0, 0),
                    Is.EqualTo(1));
            }
        }
        finally {
            DeleteDirectory(workingDirectory);
        }
    }

    [Test]
    public async Task Start_CancellationTerminatesProcessAndCancelsTask() {
        var workingDirectory = CreateWorkingDirectory();
        var scriptPath = WriteScript(
            workingDirectory,
            windows: [
                ":loop",
                "@goto loop"
            ],
            unix: [
                "while :; do :; done"
            ]);
        using var cancellation = new CancellationTokenSource();
        IProcessProxy proxy = null;
        Task start = null;
        try {
            var agent = CreateAgent(scriptPath, workingDirectory);
            agent.Synchronize = false;
            start = agent.Start(created => proxy = created, onErrorCaught: null, cancellation.Token);

            Assert.That(proxy, Is.Not.Null);
            Assert.That(proxy.Stream, Is.Not.Null);
            var processId = ((ProcessProxy)proxy).ProcessID;
            Assert.That(processId, Is.Not.Null);
            cancellation.Cancel();

            var completed = await Task.WhenAny(start, Task.Delay(TimeSpan.FromSeconds(10)));
            Assert.That(completed, Is.SameAs(start));
            Assert.CatchAsync<OperationCanceledException>(async () => await start);
            using (Assert.EnterMultipleScope()) {
                Assert.That(HasExited(processId.Value), Is.True);
            }
        }
        finally {
            if (proxy is ProcessProxy processProxy) {
                await processProxy.Kill();
            }
            if (start is not null) {
                await Task.WhenAny(start, Task.Delay(TimeSpan.FromSeconds(5)));
            }
            DeleteDirectory(workingDirectory);
        }
    }

    [Test]
    public async Task Start_PropagatesProcessLaunchFailure() {
        var missingFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var agent = new ProcessAgent {
            FileName = missingFile,
            Synchronize = false
        };
        IProcessProxy proxy = null;
        var errors = new List<Exception>();
        var start = agent.Start(created => proxy = created, errors.Add, CancellationToken.None);

        Assert.That(proxy, Is.Not.Null);
        Assert.That(proxy.Stream, Is.Null);
        Assert.ThrowsAsync<Win32Exception>(async () => await start);
        Assert.That(errors, Is.Empty);
    }

    private static ProcessAgent CreateAgent(string scriptPath, string workingDirectory) {
        return new ProcessAgent {
            Arguments = IsWindows
                ? $"/d /q /c \"\"{scriptPath}\"\""
                : $"\"{scriptPath}\"",
            FileName = IsWindows
                ? Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe"
                : "/bin/sh",
            WorkingDirectory = workingDirectory
        };
    }

    private static string CreateWorkingDirectory() {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    // A terminated process may release its working directory shortly after exiting.
    private static void DeleteDirectory(string path) {
        for (var attempt = 1; ; attempt++) {
            try {
                Directory.Delete(path, recursive: true);
                return;
            }
            catch (IOException) when (attempt < 50) {
                Thread.Sleep(100);
            }
            catch (UnauthorizedAccessException) when (attempt < 50) {
                Thread.Sleep(100);
            }
        }
    }

    private static async Task CompleteWithin(Task task, TimeSpan timeout) {
        var completed = await Task.WhenAny(task, Task.Delay(timeout));
        Assert.That(completed, Is.SameAs(task));
        await task;
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

    private static string[] Lines(IProcessProxy proxy, ProcessOutputKind kind) {
        return proxy.Stream.LineItems
            .Where(item => item.Kind == kind)
            .Select(item => item.Line)
            .ToArray();
    }

    private static bool PathsEqual(string left, string right) {
        var comparison = IsWindows
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var leftFullPath = Path.GetFullPath(left).TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);
        var rightFullPath = Path.GetFullPath(right).TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);
        return string.Equals(leftFullPath, rightFullPath, comparison);
    }

    private static string WriteScript(string workingDirectory,
                                      string[] windows,
                                      string[] unix) {
        var path = Path.Combine(
            workingDirectory,
            $"{Guid.NewGuid():N}{(IsWindows ? ".cmd" : ".sh")}");
        File.WriteAllLines(path, IsWindows ? windows : unix, Encoding.ASCII);
        return path;
    }
}
