using Domore.Conf;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CONF = Domore.Conf.Conf;

namespace Domore.Logs;

partial class LogConfFileTest {
    public sealed class ReloadService : ILogService {
        public static Action<string> OptionAssigned { get; set; }
        public static Action<ReloadService> Completed { get; set; }
        public ConcurrentQueue<string> Entries { get; } = new();
        public int CompleteCount;
        public string CompletedOption;
        public LogSeverity? CompletedThreshold;

        public string Option {
            get => _Option;
            set {
                OptionAssigned?.Invoke(value);
                _Option = value;
            }
        }
        private string _Option;

        public void Log(string name, string data, LogSeverity severity) => Entries.Enqueue(data);
        public void Complete() {
            CompletedOption = Option;
            Completed?.Invoke(this);
            Interlocked.Increment(ref CompleteCount);
        }
    }

    private static LogManager ReloadManager() {
        var config = Logging.Config;
        return (LogManager)config.GetType().GetProperty("Log").GetValue(config, null);
    }

    private static string ReloadConfiguration(string option, string severity) => $@"
        log[reload].type = {typeof(ReloadService).AssemblyQualifiedName}
        log[reload].service.option = {option}
        log[reload].config.default.severity = {severity}
    ";

    [TestCase(false)]
    [TestCase(true)]
    public void ConfigurationApplicationFinishesOnOneManagerBeforeShutdown(bool watcherReload) {
        File.WriteAllText(FilePath, ReloadConfiguration("initial", "warn"));
        Assert.That(Log.Conf.Configure(FilePath), Is.True);
        var file = ActiveFile();
        if (!watcherReload) file.Configure(watch: false);
        var oldManager = ReloadManager();
        var oldProxy = oldManager["reload"];
        var oldService = (ReloadService)oldProxy.Service;
        ReloadService.Completed = service => service.CompletedThreshold = oldProxy.Config.Default.Threshold;
        using var setterEntered = new ManualResetEventSlim();
        using var releaseSetter = new ManualResetEventSlim();
        using var reloaded = new ManualResetEventSlim();
        var gate = 1;
        ReloadService.OptionAssigned = value => {
            if (value == "configured" && Interlocked.Exchange(ref gate, 0) == 1) {
                setterEntered.Set();
                if (!releaseSetter.Wait(TimeSpan.FromSeconds(10))) {
                    throw new TimeoutException("The test did not release the configuration setter.");
                }
            }
        };
        var errors = new ConcurrentQueue<Exception>();
        EventHandler configured = (_, __) => reloaded.Set();
        ErrorEventHandler failed = (_, e) => errors.Enqueue(e.GetException());
        file.Configured += configured;
        file.ConfigureError += failed;
        Task reload = null;
        Task shutdown = null;
        try {
            File.WriteAllText(FilePath, ReloadConfiguration("configured", "debug"));
            if (!watcherReload) reload = Task.Run(() => file.Configure());
            Assert.That(setterEntered.Wait(TimeSpan.FromSeconds(10)), Is.True);
            shutdown = Task.Run(Logging.Complete);
            Assert.That(SpinWait.SpinUntil(() => !ReferenceEquals(oldManager, ReloadManager()), TimeSpan.FromSeconds(5)), Is.True,
                "Shutdown should detach the old manager while waiting for the file application.");
            var completedBeforeRelease = oldService.CompleteCount;
            releaseSetter.Set();
            if (watcherReload) Assert.That(reloaded.Wait(TimeSpan.FromSeconds(10)), Is.True);
            else Assert.That(reload.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(shutdown.Wait(TimeSpan.FromSeconds(5)), Is.True);

            // A later application must still configure the next session, including a real watcher reload above.
            file.Configure(watch: false);
            var currentProxy = ReloadManager()["reload"];
            var currentService = (ReloadService)currentProxy.Service;
            var logger = Logging.For(typeof(LogConfFileTest));
            var debugEnabled = logger.Debug();
            logger.Debug("after interleaved shutdown");
            Logging.Complete();
            Assert.Multiple(() => {
                Assert.That(errors, Is.Empty);
                Assert.That(completedBeforeRelease, Is.Zero, "Shutdown must not complete a service while its configuration is being applied.");
                Assert.That(oldService.CompletedOption, Is.EqualTo("configured"));
                Assert.That(oldService.CompletedThreshold, Is.EqualTo(LogSeverity.Debug), "The final configuration pair must reach the same manager.");
                Assert.That(oldService.CompleteCount, Is.EqualTo(1));
                Assert.That(currentProxy.Type, Is.EqualTo(typeof(ReloadService).AssemblyQualifiedName));
                Assert.That(currentService.Option, Is.EqualTo("configured"));
                Assert.That(debugEnabled, Is.True);
                Assert.That(currentService.Entries.ToArray(), Is.EqualTo(["after interleaved shutdown"]));
                Assert.That(currentService.CompleteCount, Is.EqualTo(1));
            });
        }
        finally {
            releaseSetter.Set();
            reload?.Wait(TimeSpan.FromSeconds(5));
            shutdown?.Wait(TimeSpan.FromSeconds(5));
            file.Configured -= configured;
            file.ConfigureError -= failed;
            file.Dispose();
            ReloadService.OptionAssigned = null;
            ReloadService.Completed = null;
        }
    }

    private sealed class GatedReloadProvider : IConfContentProvider {
        public IConfContentProvider Original;
        public ManualResetEventSlim Entered;
        public ManualResetEventSlim Release;
        private int Gate = 1;

        public ConfContent GetConfContent(object source) {
            if (Interlocked.Exchange(ref Gate, 0) == 1) {
                Entered.Set();
                if (!Release.Wait(TimeSpan.FromSeconds(10))) {
                    throw new TimeoutException("The test did not release configuration parsing.");
                }
            }
            return Original.GetConfContent(source);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReloadStartingAfterShutdownAppliesAllSettingsToNextManager(bool watcherReload) {
        File.WriteAllText(FilePath, ReloadConfiguration("initial", "warn"));
        Assert.That(Log.Conf.Configure(FilePath), Is.True);
        var file = ActiveFile();
        if (!watcherReload) file.Configure(watch: false);
        var oldService = (ReloadService)ReloadManager()["reload"].Service;
        var originalProvider = CONF.ContentProvider;
        using var parsingEntered = new ManualResetEventSlim();
        using var releaseParsing = new ManualResetEventSlim();
        using var reloaded = new ManualResetEventSlim();
        var errors = new ConcurrentQueue<Exception>();
        EventHandler configured = (_, __) => reloaded.Set();
        ErrorEventHandler failed = (_, e) => errors.Enqueue(e.GetException());
        file.Configured += configured;
        file.ConfigureError += failed;
        CONF.ContentProvider = new GatedReloadProvider { Original = originalProvider, Entered = parsingEntered, Release = releaseParsing };
        Task reload = null;
        try {
            File.WriteAllText(FilePath, ReloadConfiguration("configured", "debug"));
            if (!watcherReload) reload = Task.Run(() => file.Configure());
            Assert.That(parsingEntered.Wait(TimeSpan.FromSeconds(10)), Is.True);
            Logging.Complete();
            releaseParsing.Set();
            if (watcherReload) Assert.That(reloaded.Wait(TimeSpan.FromSeconds(10)), Is.True);
            else Assert.That(reload.Wait(TimeSpan.FromSeconds(5)), Is.True);
            var proxy = ReloadManager()["reload"];
            var currentService = (ReloadService)proxy.Service;
            var appliedOption = currentService.Option;
            var logger = Logging.For(typeof(LogConfFileTest));
            var debugEnabled = logger.Debug();
            file.Configure(watch: false);
            logger.Debug("shutdown won before application");
            Logging.Complete();
            Assert.Multiple(() => {
                Assert.That(errors, Is.Empty);
                Assert.That(oldService.CompletedOption, Is.EqualTo("initial"));
                Assert.That(oldService.CompleteCount, Is.EqualTo(1));
                Assert.That(proxy.Type, Is.EqualTo(typeof(ReloadService).AssemblyQualifiedName));
                Assert.That(appliedOption, Is.EqualTo("configured"));
                Assert.That(debugEnabled, Is.True);
                Assert.That(currentService.Entries.ToArray(), Is.EqualTo(["shutdown won before application"]));
                Assert.That(currentService.CompleteCount, Is.EqualTo(1));
            });
        }
        finally {
            releaseParsing.Set();
            reload?.Wait(TimeSpan.FromSeconds(5));
            CONF.ContentProvider = originalProvider;
            file.Configured -= configured;
            file.ConfigureError -= failed;
            file.Dispose();
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ConfigurationSetterCanRequestShutdownAndReleaseItsLease(bool throwFromSetter) {
        File.WriteAllText(FilePath, ReloadConfiguration("initial", "warn"));
        Assert.That(Log.Conf.Configure(FilePath), Is.True);
        var file = ActiveFile();
        file.Configure(watch: false);
        var oldProxy = ReloadManager()["reload"];
        var oldService = (ReloadService)oldProxy.Service;
        ReloadService.Completed = service => service.CompletedThreshold = oldProxy.Config.Default.Threshold;
        ReloadService.OptionAssigned = value => {
            if (value == "configured") {
                Logging.Complete();
                if (throwFromSetter) {
                    throw new InvalidOperationException("Expected configuration setter failure.");
                }
            }
        };
        try {
            File.WriteAllText(FilePath, ReloadConfiguration("configured", "debug"));
            var reload = Task.Run(() => {
                try {
                    file.Configure();
                    return (Exception)null;
                }
                catch (Exception ex) {
                    return ex;
                }
            });
            Assert.That(reload.Wait(TimeSpan.FromSeconds(5)), Is.True, "Shutdown from a setter must not wait for its own lease.");
            Assert.That(SpinWait.SpinUntil(() => oldService.CompleteCount == 1, TimeSpan.FromSeconds(5)), Is.True,
                "The configuration lease must be released even when a setter throws.");
            ReloadService.OptionAssigned = null;
            file.Configure();
            var currentService = (ReloadService)ReloadManager()["reload"].Service;
            var logger = Logging.For(typeof(LogConfFileTest));
            var debugEnabled = logger.Debug();
            logger.Debug("after setter requested shutdown");
            Logging.Complete();
            Assert.Multiple(() => {
                Assert.That(reload.Result?.ToString(), throwFromSetter
                    ? Does.Contain("Expected configuration setter failure.")
                    : Is.Null);
                Assert.That(oldService.CompletedOption, Is.EqualTo(throwFromSetter ? "initial" : "configured"));
                Assert.That(oldService.CompletedThreshold, Is.EqualTo(throwFromSetter ? LogSeverity.Warn : LogSeverity.Debug));
                Assert.That(oldService.CompleteCount, Is.EqualTo(1));
                Assert.That(currentService.Option, Is.EqualTo("configured"));
                Assert.That(debugEnabled, Is.True);
                Assert.That(currentService.Entries.ToArray(), Is.EqualTo(["after setter requested shutdown"]));
                Assert.That(currentService.CompleteCount, Is.EqualTo(1));
            });
        }
        finally {
            ReloadService.OptionAssigned = null;
            ReloadService.Completed = null;
            file.Dispose();
        }
    }
}
