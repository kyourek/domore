using Domore.Logs.Service;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CONF = Domore.Conf.Conf;

namespace Domore.Logs.Services;

[TestFixture]
[NonParallelizable]
internal sealed class ConsoleLogTest {
    [Test]
    public void ColorMapsSupportConcurrentUpdates() {
        var log = new ConsoleLog();

        Assert.Multiple(() => {
            Assert.That(log.Foreground, Is.InstanceOf<ConcurrentDictionary<LogSeverity, ConsoleColor>>());
            Assert.That(log.Background, Is.InstanceOf<ConcurrentDictionary<LogSeverity, ConsoleColor>>());
        });
    }

    [Test]
    public void ColorMapsRemainConfigurableThroughConf() {
        var log = new ConsoleLog();
        CONF.Contain("Foreground[Info] = Green").Configure(log, key: "");

        Assert.That(log.Foreground[LogSeverity.Info], Is.EqualTo(ConsoleColor.Green));
    }

    [Test]
    public void ColorFailuresDoNotSuppressConsoleOutput() {
        var output = new StringWriter();
        var originalOutput = Console.Out;
        Func<ConsoleColor> getColor = () => throw new IOException("Console colors are unavailable.");
        Action<ConsoleColor> setColor = _ => throw new IOException("Console colors are unavailable.");
        var log = new ConsoleLog(getColor, setColor, getColor, setColor);

        try {
            Console.SetOut(output);
            ((ILogService)log).Log("test", "visible without colors", LogSeverity.Info);
        }
        finally {
            Console.SetOut(originalOutput);
        }

        Assert.That(output.ToString(), Is.EqualTo("visible without colors" + Environment.NewLine));
    }

    [Test]
    public void ConcurrentColorUpdatesDoNotInterruptLogging() {
        const int count = 2000;
        var output = new StringWriter();
        var originalOutput = Console.Out;
        var log = new ConsoleLog(
            () => ConsoleColor.Gray,
            _ => { },
            () => ConsoleColor.Black,
            _ => { });
        var service = (ILogService)log;
        using var start = new ManualResetEventSlim(false);
        var update = Task.Run(() => {
            start.Wait();
            for (var i = 0; i < count; i++) {
                var severity = (LogSeverity)(i % 128);
                log.Foreground[severity] = (ConsoleColor)(i % 16);
                log.Background[severity] = (ConsoleColor)((i + 1) % 16);
            }
        });
        var write = Task.Run(() => {
            start.Wait();
            for (var i = 0; i < count; i++) {
                service.Log("test", i.ToString(), (LogSeverity)(i % 128));
            }
        });

        try {
            Console.SetOut(output);
            start.Set();
            Task.WaitAll(update, write);
        }
        finally {
            Console.SetOut(originalOutput);
        }

        var lines = output.ToString().Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);
        Assert.That(lines, Has.Length.EqualTo(count));
    }

    [Test]
    public void SetterAndRestorationColorFailuresDoNotSuppressConsoleOutput() {
        var output = new StringWriter();
        var originalOutput = Console.Out;
        var foregroundCalls = 0;
        var backgroundCalls = 0;
        Action<ConsoleColor> setForeground = _ => {
            if (Interlocked.Increment(ref foregroundCalls) == 1) {
                throw new IOException("The foreground color could not be set.");
            }
        };
        Action<ConsoleColor> setBackground = _ => {
            if (Interlocked.Increment(ref backgroundCalls) == 2) {
                throw new IOException("The background color could not be restored.");
            }
        };
        var log = new ConsoleLog(
            () => ConsoleColor.Gray,
            setForeground,
            () => ConsoleColor.Black,
            setBackground);

        try {
            Console.SetOut(output);
            ((ILogService)log).Log("test", "visible after color failures", LogSeverity.Info);
        }
        finally {
            Console.SetOut(originalOutput);
        }

        Assert.Multiple(() => {
            Assert.That(output.ToString(), Is.EqualTo("visible after color failures" + Environment.NewLine));
            Assert.That(foregroundCalls, Is.EqualTo(2));
            Assert.That(backgroundCalls, Is.EqualTo(2));
        });
    }
}
