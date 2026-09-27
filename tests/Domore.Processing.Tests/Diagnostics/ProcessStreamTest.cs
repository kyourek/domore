using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Domore.Diagnostics;

[TestFixture]
internal sealed class ProcessStreamTest {
    private static bool IsWindows {
#if NETFRAMEWORK
        get => true;
#else
        get => OperatingSystem.IsWindows();
#endif
    }

    [Test]
    public void ReadLines_AddsBlankAndUnterminatedLinesForEachStream() {
        var items = ReadLines(
            windows: [
                "@echo stdout-one",
                "@echo(",
                "@echo stdout-two",
                "@set /p _=stdout-tail<nul",
                "1>&2 <nul set /p _=stderr-one",
                "1>&2 echo("
            ],
            unix: [
                "printf 'stdout-one\\n\\nstdout-two\\nstdout-tail'",
                "printf 'stderr-one\\n' >&2"
            ]);

        using (Assert.EnterMultipleScope()) {
            Assert.That(Lines(items, ProcessOutputKind.StandardOutput), Is.EqualTo(new[] {
                "stdout-one",
                string.Empty,
                "stdout-two",
                "stdout-tail"
            }));
            Assert.That(Lines(items, ProcessOutputKind.StandardError), Is.EqualTo(new[] {
                "stderr-one"
            }));
        }
    }

    [Test]
    public void ReadLines_PostsCollectionChangesToSynchronizationContext() {
        var scriptPath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}{(IsWindows ? ".cmd" : ".sh")}");
        using var synchronizationContext = new PumpingSynchronizationContext();
        try {
            File.WriteAllLines(
                scriptPath,
                IsWindows
                    ? ["@set /p _=", "@echo context-line"]
                    : ["IFS= read -r _", "printf 'context-line\\n'"],
                Encoding.ASCII);
            using var process = StartShell(scriptPath, redirectStandardInput: true);

            ProcessStream stream;
            var previousContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            try {
                stream = new ProcessStream(process, synchronizationContext);
            }
            finally {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }

            using (stream) {
                var lines = ((IProcessStream)stream).LineItems;
                var notificationThread = 0;
                var notificationCount = 0;
                ((INotifyCollectionChanged)lines).CollectionChanged += (_, _) => {
                    Interlocked.Exchange(
                        ref notificationThread,
                        Thread.CurrentThread.ManagedThreadId);
                    Interlocked.Increment(ref notificationCount);
                };

                process.StandardInput.WriteLine("continue");
                process.StandardInput.Close();
                synchronizationContext.PumpUntil(
                    stream.Complete(),
                    TimeSpan.FromSeconds(10));
                process.WaitForExit();

                using (Assert.EnterMultipleScope()) {
                    Assert.That(process.ExitCode, Is.EqualTo(0));
                    Assert.That(Lines(lines, ProcessOutputKind.StandardOutput), Is.EqualTo(new[] {
                        "context-line"
                    }));
                    Assert.That(
                        Interlocked.CompareExchange(ref notificationThread, 0, 0),
                        Is.EqualTo(synchronizationContext.OwnerThread));
                    Assert.That(
                        Interlocked.CompareExchange(ref notificationCount, 0, 0),
                        Is.EqualTo(1));
                    Assert.That(synchronizationContext.PostCount, Is.GreaterThan(0));
                }
            }
        }
        finally {
            File.Delete(scriptPath);
        }
    }

    [Test]
    public void ReadLines_AddsLineThatSpansMultipleBuffers() {
        var expected = new string('x', 5000);
        var items = ReadLines(
            windows: [
                $"@echo {expected}"
            ],
            unix: [
                $"printf '%s' '{expected}'"
            ]);
        var line = Lines(items, ProcessOutputKind.StandardOutput).Single();

        Assert.That(line, Is.EqualTo(expected));
    }

    [Test]
    public void CurrentItem_IsItemReceivingTextAndClearsAfterLineCompletes() {
        var scriptPath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}{(IsWindows ? ".cmd" : ".sh")}");
        try {
            File.WriteAllLines(
                scriptPath,
                IsWindows
                    ? ["@echo first", "@echo(", "@set /p _=tail<nul"]
                    : ["printf 'first\\n\\ntail'"],
                Encoding.ASCII);
            using var process = StartShell(scriptPath);
            var currentItems = new List<ProcessStreamOutput>();
            var stream = new ProcessStream(process, synchronizationContext: null);
            stream.PropertyChanged += (_, e) => {
                if (e.PropertyName == nameof(ProcessStream.CurrentItem) && stream.CurrentItem is { } item) {
                    lock (currentItems) {
                        currentItems.Add(item);
                    }
                }
            };
            using (stream) {
                Assert.That(stream.Complete().Wait(TimeSpan.FromSeconds(10)), Is.True);
                process.WaitForExit();
                var lineItems = ((IProcessStream)stream).LineItems;

                using (Assert.EnterMultipleScope()) {
                    Assert.That(stream.CurrentItem, Is.Null);
                    Assert.That(Lines(lineItems, ProcessOutputKind.StandardOutput), Is.EqualTo(new[] {
                        "first",
                        string.Empty,
                        "tail"
                    }));
                    lock (currentItems) {
                        Assert.That(currentItems, Is.Not.Empty);
                        Assert.That(currentItems, Is.SubsetOf(lineItems));
                    }
                }
            }
        }
        finally {
            File.Delete(scriptPath);
        }
    }

    [Test]
    public void LineItems_ReportsReadOnly() {
        var scriptPath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}{(IsWindows ? ".cmd" : ".sh")}");
        try {
            File.WriteAllLines(
                scriptPath,
                IsWindows ? ["@echo line"] : ["printf 'line\\n'"],
                Encoding.ASCII);
            using var process = StartShell(scriptPath);
            using var stream = new ProcessStream(process, synchronizationContext: null);
            var lineItems = ((IProcessStream)stream).LineItems;

            Assert.That(lineItems.IsReadOnly, Is.True);
            Assert.That(stream.Complete().Wait(TimeSpan.FromSeconds(10)), Is.True);
            process.WaitForExit();
        }
        finally {
            File.Delete(scriptPath);
        }
    }

    private static string[] Lines(IEnumerable<IProcessStreamOutput> items, ProcessOutputKind kind) {
        return items
            .OfType<ProcessStreamOutput>()
            .Where(item => item.Kind == kind)
            .Select(item => item.Line)
            .ToArray();
    }

    private static IProcessStreamOutput[] ReadLines(string[] windows,
                                                  string[] unix) {
        var scriptPath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}{(IsWindows ? ".cmd" : ".sh")}");
        try {
            File.WriteAllLines(scriptPath, IsWindows ? windows : unix, Encoding.ASCII);
            using var process = StartShell(scriptPath);
            using var stream = new ProcessStream(process, synchronizationContext: null);
            var completed = stream.Complete().Wait(TimeSpan.FromSeconds(10));
            if (completed == false && process.HasExited == false) {
                process.Kill();
                process.WaitForExit();
            }
            Assert.That(completed, Is.True);
            process.WaitForExit();
            var items = ((IProcessStream)stream).LineItems.ToArray();
            Assert.That(
                process.ExitCode,
                Is.EqualTo(0),
                string.Join(Environment.NewLine, Lines(items, ProcessOutputKind.StandardError)));
            return items;
        }
        finally {
            File.Delete(scriptPath);
        }
    }

    private static Process StartShell(string scriptPath, bool redirectStandardInput = false) {
        var isWindows = IsWindows;
        var process = new Process {
            StartInfo = new ProcessStartInfo {
                Arguments = isWindows
                    ? $"/d /q /c \"\"{scriptPath}\"\""
                    : $"\"{scriptPath}\"",
                CreateNoWindow = true,
                FileName = isWindows
                    ? Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe"
                    : "/bin/sh",
                RedirectStandardError = true,
                RedirectStandardInput = redirectStandardInput,
                RedirectStandardOutput = true,
                UseShellExecute = false
            }
        };
        Assert.That(process.Start(), Is.True);
        return process;
    }

}
