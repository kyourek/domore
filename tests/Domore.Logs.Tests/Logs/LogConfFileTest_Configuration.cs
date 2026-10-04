using Domore.Conf;
using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Domore.Logs;

partial class LogConfFileTest {
    private static Delegate Handlers(ConfFile file, string eventName) =>
        typeof(ConfFile).GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(file) as Delegate;

    [Test]
    public void WatcherErrorsReportToStandardErrorAndHandlersAreRemovedOnDispose() {
        File.WriteAllText(FilePath, "log.logeventthreshold = info");
        var logConfFileType = typeof(Log.Conf).Assembly.GetType("Domore.Logs.LogConfFile", throwOnError: true);
        var configuration = (IDisposable)Activator.CreateInstance(logConfFileType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null, args: new object[] { FilePath }, culture: null);
        logConfFileType.GetMethod("Configure").Invoke(configuration, new object[] { false });
        var file = (ConfFile)logConfFileType.GetField("Agent", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(configuration);
        file.Delay = 0;
        using var configureErrorRaised = new ManualResetEventSlim();
        ErrorEventHandler observer = (_, __) => configureErrorRaised.Set();
        file.ConfigureError += observer;

        var previous = Console.Error;
        using var diagnostics = new StringWriter();
        try {
            Console.SetError(diagnostics);
            typeof(ConfFile).GetMethod("Watcher_Error", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(file, new object[] { null, new ErrorEventArgs(new InvalidOperationException("watcher failure")) });

            File.WriteAllText(FilePath, "log.logeventthreshold = invalid-severity");
            typeof(ConfFile).GetMethod("Watcher_Event", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(file, new object[] { null,
                    new FileSystemEventArgs(WatcherChangeTypes.Changed, DirectoryPath, Path.GetFileName(FilePath)) });
            Assert.That(configureErrorRaised.Wait(TimeSpan.FromSeconds(5)), Is.True,
                "The deterministic watcher event should finish its delayed configuration callback.");

            Assert.That(() => file.Configure(), Throws.Exception,
                "Explicit configuration failures remain visible to the caller.");
        }
        finally {
            Console.SetError(previous);
            file.ConfigureError -= observer;
            configuration.Dispose();
        }

        var configHandlers = Handlers(file, nameof(ConfFile.ConfigureError));
        var watchHandlers = Handlers(file, nameof(ConfFile.WatchError));

        Assert.Multiple(() => {
            Assert.That(diagnostics.ToString(), Does.Contain("watcher failure"));
            Assert.That(diagnostics.ToString(), Does.Contain("invalid-severity"));
            Assert.That(configHandlers?.GetInvocationList() ?? Array.Empty<Delegate>(), Is.Empty,
                "The logger's configuration error handler is detached during disposal.");
            Assert.That(watchHandlers?.GetInvocationList() ?? Array.Empty<Delegate>(), Is.Empty,
                "The logger's watcher error handler is detached during disposal.");
        });
    }
}
