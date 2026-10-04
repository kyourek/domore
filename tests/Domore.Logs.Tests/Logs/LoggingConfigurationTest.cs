using Domore.Conf;
using Domore.Conf.Logs;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Logs;

[TestFixture]
[NonParallelizable]
public sealed class LoggingConfigurationTest {
    private static readonly FieldInfo ConfigurationFile = typeof(Log.Conf).GetField("File",
        BindingFlags.Static | BindingFlags.NonPublic);
    private string DirectoryPath;

    private static void ClearConfigurationFile() {
        (ConfigurationFile.GetValue(null) as IDisposable)?.Dispose();
        ConfigurationFile.SetValue(null, null);
    }

    [SetUp]
    public void SetUp() {
        ClearConfigurationFile();
        Assert.That(Log.Conf.Configured, Is.False);
        Logging.Complete();
        FirstService.Reset();
        DirectoryPath = Path.Combine(Path.GetTempPath(), "domore.logs.configure", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(DirectoryPath);
    }

    [TearDown]
    public void TearDown() {
        ClearConfigurationFile();
        Assert.That(Log.Conf.Configured, Is.False);
        Logging.Complete();
        if (Directory.Exists(DirectoryPath)) {
            Directory.Delete(DirectoryPath, recursive: true);
        }
    }

    public sealed class FirstService : ILogService {
        public static Action<string> LabelAssigned;
        public static Func<LogSeverity?> ThresholdAtComplete;
        public static int CompleteCount;
        public static string CompletedLabel;
        public static LogSeverity? CompletedThreshold;

        public string Label {
            get;
            set {
                LabelAssigned?.Invoke(value);
                field = value;
            }
        }
        public string Type { get; set; }
        public void Log(string name, string data, LogSeverity severity) { }
        public void Complete() {
            CompletedLabel = Label;
            CompletedThreshold = ThresholdAtComplete?.Invoke();
            Interlocked.Increment(ref CompleteCount);
        }

        public static void Reset() {
            LabelAssigned = null;
            ThresholdAtComplete = null;
            CompleteCount = 0;
            CompletedLabel = null;
            CompletedThreshold = null;
        }
    }

    public sealed class SecondService : ILogService {
        public string Label { get; set; }
        public string Type { get; set; }
        public void Log(string name, string data, LogSeverity severity) { }
        public void Complete() { }
    }

    private sealed class SourceOrderContainer : IConfContainer {
        private readonly IConfContainer Agent;
        public object Source => Agent.Source;
        public IEnumerable<object> Sources => Agent.Sources;
        public IConfLookup Lookup => Agent.Lookup;

        public SourceOrderContainer(IConfContainer agent) => Agent = agent;
        public T Configure<T>(T target, string key = null) => Agent.Configure(target, key);
        public IEnumerable<T> Configure<T>(Func<T> factory, string key = null,
                                           IEqualityComparer<string> comparer = null) =>
            Agent.Configure(factory, key, comparer);
        public IEnumerable<KeyValuePair<string, T>> Configure<T>(Func<string, T> factory, string key = null,
                                                                  IEqualityComparer<string> comparer = null) =>
            Agent.Configure(factory, key, comparer);
    }

    private static LogManager Manager() =>
        (LogManager)Logging.Config.GetType().GetProperty("Log").GetValue(Logging.Config, null);

    private static int ActiveLeases(LogManager manager) {
        var loggingType = typeof(Logging);
        var locker = loggingType.GetField("ManagerLocker", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        var instance = loggingType.GetField("Instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        var counts = (Dictionary<LogManager, int>)loggingType
            .GetField("UseManagerCount", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(instance);
        lock (locker) {
            return counts.TryGetValue(manager, out var count) ? count : 0;
        }
    }

    [Test]
    public void BuiltInConfigureLoggingRunsExactServiceTypePairsFirst() {
        const string service = "ordered";
        var source = "log[" + service + "].service.label = retained" + Environment.NewLine +
            "log[" + service + "].service.type = service-type" + Environment.NewLine +
            "log[" + service + "].type = " + typeof(FirstService).AssemblyQualifiedName;

        LogConf.ConfigureLogging(source);

        var configured = (FirstService)Manager()[service].Service;
        Assert.Multiple(() => {
            Assert.That(configured.Label, Is.EqualTo("retained"));
            Assert.That(configured.Type, Is.EqualTo("service-type"),
                "Only the exact log[service].type assignment is prioritized.");
        });
    }

    [Test]
    public void BuiltInConfigureLoggingHoldsOneManagerLeaseUntilAllPairsApply() {
        const string service = "leased-config";
        var source = "log[" + service + "].type = " + typeof(FirstService).AssemblyQualifiedName + Environment.NewLine +
            "log[" + service + "].service.label = configured" + Environment.NewLine +
            "log[" + service + "].config.default.severity = debug";
        var oldManager = Manager();
        var oldProxy = oldManager[service];
        FirstService.ThresholdAtComplete = () => oldProxy.Config.Default.Threshold;
        using var setterEntered = new ManualResetEventSlim();
        using var releaseSetter = new ManualResetEventSlim();
        FirstService.LabelAssigned = value => {
            if (value == "configured") {
                setterEntered.Set();
                if (!releaseSetter.Wait(TimeSpan.FromSeconds(10))) {
                    throw new TimeoutException("The test did not release the configured label setter.");
                }
            }
        };

        Task application = null;
        Task completion = null;
        try {
            application = Task.Run(() =>
                LogConfContainer.ConfigureLogging(Domore.Conf.Conf.Contain(source)));
            Assert.That(setterEntered.Wait(TimeSpan.FromSeconds(5)), Is.True);
            completion = Task.Run(Logging.Complete);
            Assert.That(SpinWait.SpinUntil(() => !ReferenceEquals(oldManager, Manager()), TimeSpan.FromSeconds(5)), Is.True,
                "Completion should detach the manager while the configuration lease is held.");

            Assert.Multiple(() => {
                Assert.That(ActiveLeases(oldManager), Is.EqualTo(1));
                Assert.That(completion.IsCompleted, Is.False,
                    "Completion must wait until all expanded pairs finish applying.");
                Assert.That(FirstService.CompleteCount, Is.Zero,
                    "The service must not complete while its property setter is active.");
            });

            releaseSetter.Set();
            Assert.That(application.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(completion.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Assert.Multiple(() => {
                Assert.That(FirstService.CompletedLabel, Is.EqualTo("configured"));
                Assert.That(FirstService.CompletedThreshold, Is.EqualTo(LogSeverity.Debug),
                    "The final setting must apply to the same manager before it retires.");
                Assert.That(FirstService.CompleteCount, Is.EqualTo(1));
            });
        }
        finally {
            releaseSetter.Set();
            application?.Wait(TimeSpan.FromSeconds(5));
            completion?.Wait(TimeSpan.FromSeconds(5));
            FirstService.Reset();
        }
    }

    [Test]
    public void GenericConfPopulationRetainsSourceOrder() {
        const string service = "generic";
        var source = "log[" + service + "].service.label = source-order" + Environment.NewLine +
            "log[" + service + "].type = " + typeof(FirstService).AssemblyQualifiedName;

        Domore.Conf.Conf.Contain(source).Configure(Logging.Config, key: "");

        Assert.That(((FirstService)Manager()[service].Service).Label, Is.Null);
    }

    [Test]
    public void ThirdPartyContainerKeepsItsSourceOrder() {
        const string service = "third-party";
        var source = "log[" + service + "].service.label = source-order" + Environment.NewLine +
            "log[" + service + "].type = " + typeof(FirstService).AssemblyQualifiedName;

        new SourceOrderContainer(Domore.Conf.Conf.Contain(source)).ConfigureLogging();

        Assert.That(((FirstService)Manager()[service].Service).Label, Is.Null);
    }

    [Test]
    public void LogConfConfigurePartitionsExpandedIncludesStably() {
        const string service = "included";
        var includedPath = Path.Combine(DirectoryPath, "included.conf");
        var rootPath = Path.Combine(DirectoryPath, "logging.conf");
        File.WriteAllText(includedPath,
            "log[" + service + "].service.label = include-first" + Environment.NewLine +
            "log[" + service + "].service.label = retained-last" + Environment.NewLine +
            "log[" + service + "].type = " + typeof(FirstService).AssemblyQualifiedName);
        File.WriteAllText(rootPath,
            "@conf.include = included.conf" + Environment.NewLine +
            "log[" + service + "].type = " + typeof(SecondService).AssemblyQualifiedName);

        Assert.That(Log.Conf.Configure(rootPath), Is.True);

        var proxy = Manager()[service];
        Assert.Multiple(() => {
            Assert.That(proxy.Type, Is.EqualTo(typeof(SecondService).AssemblyQualifiedName),
                "Duplicate type assignments keep their order after partitioning.");
            Assert.That(((SecondService)proxy.Service).Label, Is.EqualTo("retained-last"),
                "Included and root service settings keep their relative order after all type assignments.");
            Assert.That(Log.Conf.Configured, Is.True, "The watcher remains installed after initial application.");
        });

        var configuration = ConfigurationFile.GetValue(null);
        var file = (ConfFile)configuration.GetType().GetField("Agent", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(configuration);
        file.Configure(watch: false);
        File.WriteAllText(includedPath,
            "log[" + service + "].service.label = reloaded-label" + Environment.NewLine +
            "log[" + service + "].type = " + typeof(FirstService).AssemblyQualifiedName);

        file.Configure();

        Assert.That(((SecondService)Manager()[service].Service).Label, Is.EqualTo("reloaded-label"),
            "A reload applies all type pairs before included service settings.");
    }
}
