using Domore.Logs.Service;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using CONF = Domore.Conf.Conf;

namespace Domore.Logs.Services;

[TestFixture]
[NonParallelizable]
internal sealed class ConsoleLogTest {
    [Test]
    public void ConfCanConfigureConsoleColorEntries() {
        var log = new ConsoleLog();

        CONF.Contain("foreground[warn] = red").Configure(log, key: "");

        Assert.That(log.Foreground[LogSeverity.Warn], Is.EqualTo(ConsoleColor.Red));
    }

    [Test]
    public void ConsoleLogSupportsConcurrentColorUpdates() {
        var log = new ConsoleLog();
        var service = (ILogService)log;
        var output = new StringWriter();
        var original = Console.Out;
        var failures = new ConcurrentQueue<Exception>();
        var mutate = new Thread(() => {
            try {
                var foreground = log.Foreground;
                var background = log.Background;
                for (var i = 0; i < 5000; i++) {
                    foreground[LogSeverity.Warn] = (i & 1) == 0 ? ConsoleColor.Red : ConsoleColor.Yellow;
                    background[LogSeverity.Warn] = (i & 1) == 0 ? ConsoleColor.Black : ConsoleColor.Blue;
                }
            }
            catch (Exception ex) {
                failures.Enqueue(ex);
            }
        }) {
            IsBackground = true
        };
        var write = new Thread(() => {
            try {
                for (var i = 0; i < 5000; i++) {
                    service.Log(nameof(ConsoleLogSupportsConcurrentColorUpdates), $"{i}", LogSeverity.Warn);
                }
            }
            catch (Exception ex) {
                failures.Enqueue(ex);
            }
        }) {
            IsBackground = true
        };

        try {
            Console.SetOut(output);
            mutate.Start();
            write.Start();

            Assert.That(mutate.Join(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(write.Join(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(failures, Is.Empty);
        }
        finally {
            if (mutate.IsAlive) {
                mutate.Join(TimeSpan.FromSeconds(1));
            }
            if (write.IsAlive) {
                write.Join(TimeSpan.FromSeconds(1));
            }
            Console.SetOut(original);
            output.Dispose();
        }
    }
}
