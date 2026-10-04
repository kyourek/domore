using Domore.Threading;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Domore.Logs;
public sealed partial class LoggingTest {
    [Test]
    public void ReviewBackgroundQueueFirstAddCompleteStressDrainsAcceptedItems() {
        const int iterations = 32;
        var acceptedItems = 0;
        var executedItems = 0;
        var failures = new List<string>();
        var threadLockerField = typeof(BackgroundQueue).GetField(
            "ThreadLocker",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var collectionField = typeof(BackgroundQueue).GetField(
            "Collection",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(threadLockerField, Is.Not.Null);
        Assert.That(collectionField, Is.Not.Null);

        for (var i = 0; i < iterations; i++) {
            var queue = new BackgroundQueue();
            var threadLocker = threadLockerField.GetValue(queue);
            var collection = (BlockingCollection<Action>)collectionField.GetValue(queue);
            var itemExecuted = 0;
            var addStarted = new ManualResetEventSlim();
            var allowAdd = new ManualResetEventSlim();
            var addFinished = new ManualResetEventSlim();
            var addFailure = default(Exception);
            var addThreadStarted = false;
            var lockerHeld = false;
            var accepted = 0;
            var completed = false;

            var addThread = new Thread(() => {
                addStarted.Set();
                try {
                    if (allowAdd.Wait(TimeSpan.FromSeconds(5))) {
                        queue.Add(() => Interlocked.Increment(ref itemExecuted));
                    }
                }
                catch (Exception ex) {
                    addFailure = ex;
                }
                finally {
                    addFinished.Set();
                }
            }) {
                IsBackground = true
            };

            try {
                Monitor.Enter(threadLocker, ref lockerHeld);
                addThread.Start();
                addThreadStarted = true;
                if (addStarted.Wait(TimeSpan.FromSeconds(1)) == false) {
                    failures.Add($"iteration {i}: Add did not start");
                }
                allowAdd.Set();

                SpinWait.SpinUntil(
                    () => collection.Count > 0 || addFinished.IsSet,
                    TimeSpan.FromMilliseconds(100));
                accepted = collection.Count;
                acceptedItems += accepted;

                completed = queue.Complete(TimeSpan.FromSeconds(1));
                executedItems += Interlocked.CompareExchange(ref itemExecuted, 0, 0);
            }
            catch (Exception ex) {
                failures.Add($"iteration {i}: {ex}");
            }
            finally {
                allowAdd.Set();
                try {
                    queue.Dispose();
                }
                catch (Exception ex) {
                    failures.Add($"iteration {i}: Dispose threw {ex}");
                }
                if (lockerHeld) {
                    Monitor.Exit(threadLocker);
                }
            }

            if (completed == false) {
                failures.Add($"iteration {i}: Complete did not report a drained queue");
            }
            if (addFailure != null) {
                failures.Add($"iteration {i}: Add threw {addFailure}");
            }
            if (addThreadStarted && addThread.Join(TimeSpan.FromSeconds(2)) == false) {
                failures.Add($"iteration {i}: Add did not return");
            }
            var executed = Interlocked.CompareExchange(ref itemExecuted, 0, 0);
            if (accepted != executed) {
                failures.Add($"iteration {i}: accepted {accepted} item(s), executed {executed}");
            }
            if (addThreadStarted && addThread.Join(0)) {
                addStarted.Dispose();
                allowAdd.Dispose();
                addFinished.Dispose();
            }
        }

        Assert.That(
            failures,
            Is.Empty,
            $"Accepted {acceptedItems} item(s), but only {executedItems} had executed when Complete returned. " +
            string.Join(Environment.NewLine, failures));
    }

#if NET48 || NET10_0
    // The exit helper is built only for net48 and net10.0.
    [Test]
    public void ReviewProcessExitFlushesQueuedFileLogEntry() {
        var outputFile = Path.Combine(
            Path.GetDirectoryName(typeof(LoggingTest).Assembly.Location),
            $"process-exit-{Guid.NewGuid():N}.log");
        var message = $"process-exit-{Guid.NewGuid():N}";
        var testOutput = new DirectoryInfo(Path.GetDirectoryName(typeof(LoggingTest).Assembly.Location));
        var helperOutput = Path.GetFullPath(Path.Combine(
            testOutput.FullName,
            "..",
            "..",
            "Domore.Logs.ExitHelper",
            testOutput.Name));

#if NETFRAMEWORK
        var helperPath = Path.Combine(helperOutput, "Domore.Logs.ExitHelper.exe");
        var startInfo = new ProcessStartInfo(
            helperPath,
            $"\"{outputFile}\" \"{message}\"");
#else
        var helperPath = Path.Combine(helperOutput, "Domore.Logs.ExitHelper.dll");
        var startInfo = new ProcessStartInfo(
            "dotnet",
            $"\"{helperPath}\" \"{outputFile}\" \"{message}\"");
#endif
        startInfo.CreateNoWindow = true;
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardError = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.WorkingDirectory = helperOutput;

        try {
            using var process = Process.Start(startInfo);
            Assert.That(process, Is.Not.Null, $"Could not start exit helper at {helperPath}");
            var standardOutput = process.StandardOutput.ReadToEndAsync();
            var standardError = process.StandardError.ReadToEndAsync();
            var exited = process.WaitForExit((int)TimeSpan.FromSeconds(10).TotalMilliseconds);
            if (exited == false) {
                try {
                    process.Kill();
                }
                catch {
                }
                process.WaitForExit((int)TimeSpan.FromSeconds(2).TotalMilliseconds);
            }
            var standardOutputText = standardOutput.Wait(TimeSpan.FromSeconds(2))
                ? standardOutput.Result
                : "standard output was still being read";
            var standardErrorText = standardError.Wait(TimeSpan.FromSeconds(2))
                ? standardError.Result
                : "standard error was still being read";

            Assert.That(
                exited,
                Is.True,
                $"Exit helper timed out. stdout: {standardOutputText}; stderr: {standardErrorText}");
            Assert.That(
                process.ExitCode,
                Is.Zero,
                $"Exit helper failed. stdout: {standardOutputText}; stderr: {standardErrorText}");
            Assert.That(File.Exists(outputFile), Is.True, "the log file was not written");
            Assert.That(File.ReadAllText(outputFile), Does.Contain(message));
        }
        finally {
            if (File.Exists(outputFile)) {
                File.Delete(outputFile);
            }
        }
    }
#endif
}
