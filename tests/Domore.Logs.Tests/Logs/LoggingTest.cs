using Domore.Conf.Logs;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CONF = Domore.Conf.Conf;

namespace Domore.Logs;

[TestFixture]
public sealed partial class LoggingTest {
    private ILog Log {
        get => _Log ??= Logging.For(typeof(LoggingTest));
        set => _Log = value;
    }
    private ILog _Log;

    private bool SkipLoggingCompleteOnTearDown;

    private string Config {
        get => _Config;
        set => CONF.Contain(_Config = value).Configure(Logging.Config, key: "");
    }
    private string _Config;

    [SetUp]
    public void SetUp() {
        Log = null;
        SkipLoggingCompleteOnTearDown = false;
    }

    [TearDown]
    public void TearDown() {
        if (SkipLoggingCompleteOnTearDown != true) {
            Logging.Complete();
        }
    }

    [Test]
    public void LogEventIsRaised2() {
        var message = "";
        Logging.EventThreshold = LogSeverity.Info;
        Logging.Event += (_, e) => {
            message = e.LogList.Single();
        };
        var log = Logging.For(typeof(LoggingTest));
        log.Info("Here's the log");
        Assert.That(message, Is.EqualTo("Here's the log"));
    }

    [Test]
    public void LogEventIsNotRaisedIfThresholdIsNotMet() {
        var message = "";
        Logging.EventThreshold = LogSeverity.Warn;
        Logging.Event += (_, e) => {
            message = e.LogList.Single();
        };
        var log = Logging.For(typeof(LoggingTest));
        log.Info("Here's the log");
        Assert.That(message, Is.EqualTo(""));
    }

    [Test]
    public void LogEventIsRaisedWithManyMessages2() {
        var message = new List<string>();
        Logging.EventThreshold = LogSeverity.Info;
        Logging.Event += (_, e) => {
            message.AddRange(e.LogList);
        };
        var log = Logging.For(typeof(LoggingTest));
        log.Info("log1", "log2", "log3");
        Assert.That(message, Is.EqualTo(["log1", "log2", "log3"]));
    }

    [Test]
    public void ThrowingLogEventHandlerDoesNotInterruptOtherDelivery() {
        HealthyCompleteLogService.Reset();
        Config = $@"
                log[healthy].type = {typeof(HealthyCompleteLogService).AssemblyQualifiedName}
                log[healthy].config.default.severity = info
            ";
        Logging.EventThreshold = LogSeverity.Info;
        var laterEventHandlerCalled = false;
        Logging.Event += (_, __) => throw new InvalidOperationException("Expected test event handler failure.");
        Logging.Event += (_, __) => laterEventHandlerCalled = true;
        var subscription = new CompletionWindowSubscription();
        Logging.Subscribe(subscription);

        var loggingCallFailed = false;
        try {
            Log.Info("survives event handler");
        }
        catch (Exception) {
            loggingCallFailed = true;
        }
        Logging.Complete();

        Assert.Multiple(() => {
            Assert.That(loggingCallFailed, Is.False);
            Assert.That(laterEventHandlerCalled, Is.True);
            Assert.That(subscription.Entries.ToArray(), Is.EqualTo(["survives event handler"]));
            Assert.That(HealthyCompleteLogService.Entries.ToArray(), Is.EqualTo(["survives event handler"]));
        });
    }

    private sealed class TrackingThresholdSubscription : ILogSubscription {
        private EventHandler ThresholdChangedHandlers;

        public int HandlerCount => ThresholdChangedHandlers?.GetInvocationList().Length ?? 0;
        public int RemoveCount { get; private set; }

        event EventHandler ILogSubscription.ThresholdChanged {
            add => ThresholdChangedHandlers += value;
            remove {
                ThresholdChangedHandlers -= value;
                RemoveCount++;
            }
        }

        public LogSeverity Threshold(Type type) => LogSeverity.Info;

        public void Receive(ILogEntry entry) {
        }
    }

    private sealed class GatedThresholdSubscription : ILogSubscription {
        private EventHandler ThresholdChangedHandlers;
        private int CurrentThreshold;
        private int ThresholdCalls;

        public readonly ManualResetEventSlim ThresholdStarted = new(false);
        public readonly ManualResetEventSlim ContinueThreshold = new(false);

        public GatedThresholdSubscription(LogSeverity threshold) {
            CurrentThreshold = (int)threshold;
        }

        event EventHandler ILogSubscription.ThresholdChanged {
            add => ThresholdChangedHandlers += value;
            remove => ThresholdChangedHandlers -= value;
        }

        public LogSeverity Threshold(Type type) {
            var threshold = (LogSeverity)Volatile.Read(ref CurrentThreshold);
            if (Interlocked.Increment(ref ThresholdCalls) == 1) {
                ThresholdStarted.Set();
                if (ContinueThreshold.Wait(TimeSpan.FromSeconds(5)) == false) {
                    throw new TimeoutException("The test did not release the gated threshold query.");
                }
            }
            return threshold;
        }

        public void ChangeThreshold(LogSeverity threshold) {
            Volatile.Write(ref CurrentThreshold, (int)threshold);
            ThresholdChangedHandlers?.Invoke(this, EventArgs.Empty);
        }

        public void Receive(ILogEntry entry) {
        }
    }

    [Test]
    public void ThresholdChangeCannotBeOverwrittenByInFlightCacheFill() {
        var subscription = new GatedThresholdSubscription(LogSeverity.Warn);
        var proxy = new LogSubscriptionProxy(subscription);
        var type = typeof(LoggingTest);
        var pendingThreshold = Task.Run(() => proxy.Threshold(type));

        try {
            Assert.That(subscription.ThresholdStarted.Wait(TimeSpan.FromSeconds(5)), Is.True,
                "The initial threshold query should reach the gated callback.");
            subscription.ChangeThreshold(LogSeverity.Debug);
        }
        finally {
            subscription.ContinueThreshold.Set();
        }

        try {
            Assert.That(pendingThreshold.Wait(TimeSpan.FromSeconds(5)), Is.True,
                "The initial threshold query should finish after it is released.");
            Assert.That(pendingThreshold.Result, Is.EqualTo(LogSeverity.Warn));
            Assert.That(proxy.Threshold(type), Is.EqualTo(LogSeverity.Debug));
        }
        finally {
            proxy.Complete();
            subscription.ThresholdStarted.Dispose();
            subscription.ContinueThreshold.Dispose();
        }
    }

    [Test]
    public void UnsubscribeDetachesThresholdHandlerFromAgent() {
        var collection = new LogSubscriptionCollection();
        var subscription = new TrackingThresholdSubscription();
        collection.Add(subscription);
        var afterAdd = subscription.HandlerCount;

        collection.Remove(subscription);
        var afterRemove = subscription.HandlerCount;
        collection.Add(subscription);
        var afterReAdd = subscription.HandlerCount;
        collection.Remove(subscription);

        Assert.Multiple(() => {
            Assert.That(afterAdd, Is.EqualTo(1));
            Assert.That(afterRemove, Is.EqualTo(0));
            Assert.That(afterReAdd, Is.EqualTo(1));
            Assert.That(subscription.HandlerCount, Is.EqualTo(0));
            Assert.That(subscription.RemoveCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void ClearDetachesThresholdHandlersAndCompletionIsIdempotent() {
        var collection = new LogSubscriptionCollection();
        var subscription = new TrackingThresholdSubscription();
        collection.Add(subscription);
        collection.Complete();
        var afterComplete = subscription.HandlerCount;
        collection.Clear();
        collection.Clear();
        var afterRepeatedClear = subscription.HandlerCount;
        var removeCountAfterFirstLifecycle = subscription.RemoveCount;

        collection.Add(subscription);
        collection.Clear();

        Assert.Multiple(() => {
            Assert.That(afterComplete, Is.EqualTo(0));
            Assert.That(afterRepeatedClear, Is.EqualTo(0));
            Assert.That(removeCountAfterFirstLifecycle, Is.EqualTo(1));
            Assert.That(subscription.HandlerCount, Is.EqualTo(0));
            Assert.That(subscription.RemoveCount, Is.EqualTo(2));
        });
    }

    private sealed class SubscribingDuringThresholdSubscription : ILogSubscription {
        private readonly ILogSubscription Subscription;
        private bool Added;

        public SubscribingDuringThresholdSubscription(ILogSubscription subscription) {
            Subscription = subscription;
        }

        event EventHandler ILogSubscription.ThresholdChanged {
            add { }
            remove { }
        }

        public LogSeverity Threshold(Type type) {
            if (Added == false) {
                Added = true;
                Logging.Subscribe(Subscription);
            }
            return LogSeverity.Info;
        }

        public void Receive(ILogEntry entry) {
        }
    }

    private sealed class SubscribingDuringReceiveSubscription : ILogSubscription {
        private readonly ILogSubscription Subscription;
        private bool Added;

        public SubscribingDuringReceiveSubscription(ILogSubscription subscription) {
            Subscription = subscription;
        }

        event EventHandler ILogSubscription.ThresholdChanged {
            add { }
            remove { }
        }

        public LogSeverity Threshold(Type type) => LogSeverity.Info;

        public void Receive(ILogEntry entry) {
            if (Added == false) {
                Added = true;
                Logging.Subscribe(Subscription);
            }
        }
    }

    [Test]
    public void SubscriptionThresholdCallbackCanAddSubscription() {
        var addedSubscription = new CompletionWindowSubscription();
        Logging.Subscribe(new SubscribingDuringThresholdSubscription(addedSubscription));
        var enabled = false;
        var loggingCallFailed = false;
        try {
            enabled = Log.Info();
        }
        catch (Exception) {
            loggingCallFailed = true;
        }

        Log.Info("after threshold callback");
        Logging.Complete();

        Assert.Multiple(() => {
            Assert.That(loggingCallFailed, Is.False);
            Assert.That(enabled, Is.True);
            Assert.That(addedSubscription.Entries.ToArray(), Is.EqualTo(["after threshold callback"]));
        });
    }

    [Test]
    public void SubscriptionReceiveCallbackCanAddSubscription() {
        var addedSubscription = new CompletionWindowSubscription();
        Logging.Subscribe(new SubscribingDuringReceiveSubscription(addedSubscription));
        var loggingCallFailed = false;
        try {
            Log.Info("adds subscription");
        }
        catch (Exception) {
            loggingCallFailed = true;
        }

        Log.Info("after receive callback");
        Logging.Complete();

        Assert.Multiple(() => {
            Assert.That(loggingCallFailed, Is.False);
            Assert.That(addedSubscription.Entries.ToArray(), Is.EqualTo(["after receive callback"]));
        });
    }

    private static void ConfigureCollectionService(LogServiceCollection collection, string name, Type type) {
        var service = collection[name];
        service.Type = type.AssemblyQualifiedName;
        service.Config.Default.Threshold = LogSeverity.Info;
    }

    private static class ServiceReplacementState {
        public static readonly ConcurrentQueue<string> Events = new();
        public static readonly ManualResetEventSlim OldLogStarted = new();
        public static readonly ManualResetEventSlim ReleaseOldLog = new();
        public static int OldCompleteCount;
        public static int NewCompleteCount;

        public static void Reset() {
            while (Events.TryDequeue(out _)) {
            }
            OldLogStarted.Reset();
            ReleaseOldLog.Reset();
            OldCompleteCount = 0;
            NewCompleteCount = 0;
        }
    }

    private sealed class OldReplacementLogService : ILogService {
        public void Log(string name, string data, LogSeverity severity) {
            ServiceReplacementState.Events.Enqueue("old-log-start");
            ServiceReplacementState.OldLogStarted.Set();
            if (ServiceReplacementState.ReleaseOldLog.Wait(TimeSpan.FromSeconds(5)) == false) {
                throw new TimeoutException("The test did not release the old service log callback.");
            }
            ServiceReplacementState.Events.Enqueue("old-log-end");
        }

        public void Complete() {
            ServiceReplacementState.Events.Enqueue("old-complete");
            Interlocked.Increment(ref ServiceReplacementState.OldCompleteCount);
        }
    }

    private sealed class NewReplacementLogService : ILogService {
        public void Log(string name, string data, LogSeverity severity) {
            ServiceReplacementState.Events.Enqueue($"new-log:{data}");
        }

        public void Complete() {
            ServiceReplacementState.Events.Enqueue("new-complete");
            Interlocked.Increment(ref ServiceReplacementState.NewCompleteCount);
        }
    }

    [Test]
    public void ServiceTypeReplacementCompletesOldServiceAfterInflightDelivery() {
        ServiceReplacementState.Reset();
        var proxy = new LogServiceProxy("replacement");
        proxy.Type = typeof(OldReplacementLogService).AssemblyQualifiedName;
        proxy.Config.Default.Threshold = LogSeverity.Info;
        Task delivery = null;
        Task replacement = null;

        try {
            delivery = Task.Run(() => proxy.Log(new LogEntry(typeof(LoggingTest), DateTime.UtcNow,
                LogSeverity.Info, ["before replacement"])));
            Assert.That(ServiceReplacementState.OldLogStarted.Wait(TimeSpan.FromSeconds(5)), Is.True,
                "The old service should enter its log callback.");

            using var replacementStarted = new ManualResetEventSlim();
            replacement = Task.Run(() => {
                replacementStarted.Set();
                proxy.Type = typeof(NewReplacementLogService).AssemblyQualifiedName;
            });
            Assert.That(replacementStarted.Wait(TimeSpan.FromSeconds(5)), Is.True,
                "The type replacement should be requested while delivery is active.");
        }
        finally {
            ServiceReplacementState.ReleaseOldLog.Set();
        }

        var completionAttempted = false;
        try {
            Assert.That(delivery.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(replacement.Wait(TimeSpan.FromSeconds(5)), Is.True);
            proxy.Log(new LogEntry(typeof(LoggingTest), DateTime.UtcNow, LogSeverity.Info, ["after replacement"]));
            proxy.Complete();
            completionAttempted = true;
        }
        finally {
            ServiceReplacementState.ReleaseOldLog.Set();
            if (completionAttempted == false) {
                proxy.Complete();
            }
        }

        var events = ServiceReplacementState.Events.ToArray();
        var oldLogEnded = Array.IndexOf(events, "old-log-end");
        var oldCompleted = Array.IndexOf(events, "old-complete");
        Assert.Multiple(() => {
            Assert.That(ServiceReplacementState.OldCompleteCount, Is.EqualTo(1));
            Assert.That(ServiceReplacementState.NewCompleteCount, Is.EqualTo(1));
            Assert.That(oldLogEnded, Is.GreaterThanOrEqualTo(0));
            Assert.That(oldCompleted, Is.GreaterThan(oldLogEnded));
            Assert.That(events, Does.Contain("new-log:after replacement"));
        });
    }

    private sealed class AddingServiceDuringLog : ILogService {
        private static int Added;

        public static LogServiceCollection Collection { get; set; }

        public static void Reset(LogServiceCollection collection) {
            Collection = collection;
            Added = 0;
        }

        public void Log(string name, string data, LogSeverity severity) {
            if (Interlocked.Exchange(ref Added, 1) == 0) {
                ConfigureCollectionService(Collection, "z_added", typeof(HealthyCompleteLogService));
            }
        }

        public void Complete() {
        }
    }

    [Test]
    public void ServiceDeliverySnapshotSurvivesReentrantConfiguration() {
        HealthyCompleteLogService.Reset();
        var collection = new LogServiceCollection();
        AddingServiceDuringLog.Reset(collection);
        ConfigureCollectionService(collection, "a_mutating", typeof(AddingServiceDuringLog));
        ConfigureCollectionService(collection, "y_healthy", typeof(HealthyCompleteLogService));
        try {
            collection.Send(new LogEntry(typeof(LoggingTest), DateTime.UtcNow, LogSeverity.Info, ["service snapshot"]));
            collection.Complete();
        }
        finally {
            collection.Dispose();
            AddingServiceDuringLog.Reset(null);
        }

        Assert.That(HealthyCompleteLogService.Entries.ToArray(), Is.EqualTo(["service snapshot"]));
    }

    private sealed class AddingServiceDuringComplete : ILogService {
        private static int Added;

        public static LogServiceCollection Collection { get; set; }

        public static void Reset(LogServiceCollection collection) {
            Collection = collection;
            Added = 0;
        }

        public void Log(string name, string data, LogSeverity severity) {
        }

        public void Complete() {
            if (Interlocked.Exchange(ref Added, 1) == 0) {
                _ = Collection["z_added"];
            }
        }
    }

    [Test]
    public void ServiceCompletionSnapshotSurvivesReentrantConfiguration() {
        HealthyCompleteLogService.Reset();
        var collection = new LogServiceCollection();
        AddingServiceDuringComplete.Reset(collection);
        ConfigureCollectionService(collection, "a_mutating", typeof(AddingServiceDuringComplete));
        ConfigureCollectionService(collection, "y_healthy", typeof(HealthyCompleteLogService));
        _ = collection["y_healthy"].Service;
        var completionFailed = false;
        try {
            collection.Complete();
        }
        catch (Exception) {
            completionFailed = true;
        }
        finally {
            collection.Dispose();
            AddingServiceDuringComplete.Reset(null);
        }

        Assert.Multiple(() => {
            Assert.That(completionFailed, Is.False);
            Assert.That(HealthyCompleteLogService.CompleteCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void DefaultLogEventThresholdIsNone() {
        Assert.That(Logging.EventThreshold, Is.EqualTo(LogSeverity.None));
    }

    [TestCase(LogSeverity.Debug)]
    [TestCase(LogSeverity.Info)]
    [TestCase(LogSeverity.Warn)]
    [TestCase(LogSeverity.Error)]
    [TestCase(LogSeverity.Critical)]
    public void EnabledIsFalseIfNothingIsConfigured(LogSeverity severity) {
        Assert.That(Log.Enabled(severity), Is.False);
    }

    [TestCase(LogSeverity.Debug)]
    [TestCase(LogSeverity.Info)]
    [TestCase(LogSeverity.Warn)]
    [TestCase(LogSeverity.Error)]
    [TestCase(LogSeverity.Critical)]
    public void SeverityEnabledIsFalseIfNothingIsConfigured(LogSeverity severity) {
        Assert.That(Log.GetType().GetMethod($"{severity}", Type.EmptyTypes).Invoke(Log, null), Is.False);
    }

    [TestCase(LogSeverity.Debug)]
    [TestCase(LogSeverity.Info)]
    [TestCase(LogSeverity.Warn)]
    [TestCase(LogSeverity.Error)]
    [TestCase(LogSeverity.Critical)]
    public void EnabledIsTrueIfEventIsSubscribedTo2(LogSeverity severity) {
        Logging.Event += (_, __) => { };
        Logging.EventThreshold = severity;
        Assert.That(Log.Enabled(severity), Is.True);
    }

    [TestCase(LogSeverity.Debug)]
    [TestCase(LogSeverity.Info)]
    [TestCase(LogSeverity.Warn)]
    [TestCase(LogSeverity.Error)]
    [TestCase(LogSeverity.Critical)]
    public void SeverityEnabledIsTrueIfEventIsSubscribedTo2(LogSeverity severity) {
        Logging.Event += (_, __) => { };
        Logging.EventThreshold = severity;
        Assert.That(Log.GetType().GetMethod($"{severity}", Type.EmptyTypes).Invoke(Log, null), Is.True);
    }

    [TestCase(LogSeverity.Debug)]
    [TestCase(LogSeverity.Info)]
    [TestCase(LogSeverity.Warn)]
    [TestCase(LogSeverity.Error)]
    [TestCase(LogSeverity.Critical)]
    public void SeverityEnabledIsFalseIfEventIsSubscribedToButThresholdNotMet2(LogSeverity severity) {
        Logging.Event += (_, __) => { };
        Logging.EventThreshold = severity == LogSeverity.Critical ? LogSeverity.None : (severity + 1);
        Assert.That(Log.GetType().GetMethod($"{severity}", Type.EmptyTypes).Invoke(Log, null), Is.False);
    }

    private sealed class TestLogService : ILogService {
        private readonly List<Item> List;

        public ReadOnlyCollection<Item> Items { get; }

        public static TestLogService Instance { get; private set; }

        public TestLogService() {
            List = new List<Item>();
            Items = new ReadOnlyCollection<Item>(List);
            Instance = this;
        }

        void ILogService.Complete() {
        }

        void ILogService.Log(string name, string data, LogSeverity severity) {
            List.Add(new Item(name, data, severity));
        }

        public sealed class Item {
            public string Name { get; }
            public string Data { get; }
            public LogSeverity Severity { get; }

            public Item(string name, string data, LogSeverity severity) {
                Name = name;
                Data = data;
                Severity = severity;
            }
        }
    }

    private void ConfigTest(string config = null) {
        Config = $@"
                Log[t].type = {typeof(TestLogService).AssemblyQualifiedName}
                log[t].config.default.severity = info
                log[t].config.default.format = {{log}} [{{sev}}]
                {config}
            ";
    }

    private sealed class CompletingLogService : ILogService {
        public static ManualResetEventSlim CallbackReturned { get; } = new();
        public static ManualResetEventSlim ServiceCompleted { get; } = new();
        public static ManualResetEventSlim CompleteEntered { get; } = new();
        public static ManualResetEventSlim AllowComplete { get; } = new();
        public static bool BlockComplete { get; set; }

        public CompletingLogService() {
        }

        public static void Reset() {
            CallbackReturned.Reset();
            ServiceCompleted.Reset();
            CompleteEntered.Reset();
            AllowComplete.Reset();
            BlockComplete = false;
        }

        public void Log(string name, string data, LogSeverity severity) {
            Logging.Complete();
            CallbackReturned.Set();
        }

        public void Complete() {
            CompleteEntered.Set();
            if (BlockComplete) {
                AllowComplete.Wait(TimeSpan.FromSeconds(10));
            }
            ServiceCompleted.Set();
        }
    }

    [Test]
    public void CompleteCanBeCalledFromLogServiceCallback() {
        CompletingLogService.Reset();
        Config = $@"
                log[complete].type = {typeof(CompletingLogService).AssemblyQualifiedName}
                log[complete].config.default.severity = info
            ";

        Log.Info("complete from callback");

        var callbackReturned = CompletingLogService.CallbackReturned.Wait(TimeSpan.FromSeconds(2));
        if (callbackReturned == false) {
            SkipLoggingCompleteOnTearDown = true;
        }
        Assert.That(callbackReturned, Is.True, "Logging.Complete should return without joining its own worker thread.");
        Assert.That(CompletingLogService.ServiceCompleted.Wait(TimeSpan.FromSeconds(2)), Is.True);
    }

    private sealed class DeferredCompletionSubscription : ILogSubscription {
        public ManualResetEventSlim Detached { get; } = new();
        public ConcurrentQueue<string> Entries { get; } = new();

        event EventHandler ILogSubscription.ThresholdChanged {
            add { }
            remove => Detached.Set();
        }

        public LogSeverity Threshold(Type type) => LogSeverity.Info;

        public void Receive(ILogEntry entry) {
            foreach (var item in entry.LogList) {
                Entries.Enqueue(item);
            }
        }
    }

    private static int RetiringManagerCount() {
        var flags = BindingFlags.Static | BindingFlags.NonPublic;
        var instance = typeof(Logging).GetField("Instance", flags).GetValue(null);
        var locker = typeof(Logging).GetField("ManagerLocker", flags).GetValue(null);
        var requests = (System.Collections.IDictionary)typeof(Logging)
            .GetField("RetiringManagers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);
        lock (locker) {
            return requests.Count;
        }
    }

    private sealed class CompletingOnRemovalSubscription : ILogSubscription {
        private EventHandler Handler;
        public ManualResetEventSlim RemovalEntered { get; } = new();
        public ManualResetEventSlim AllowRemoval { get; } = new();
        public ManualResetEventSlim NestedCompletionReturned { get; } = new();
        public bool NestedCompletionResult;

        event EventHandler ILogSubscription.ThresholdChanged {
            add => Handler += value;
            remove {
                RemovalEntered.Set();
                AllowRemoval.Wait(TimeSpan.FromSeconds(10));
                Handler -= value;
                NestedCompletionResult = Logging.Complete(TimeSpan.Zero);
                NestedCompletionReturned.Set();
            }
        }

        public LogSeverity Threshold(Type type) => LogSeverity.Info;
        public void Receive(ILogEntry entry) { }
    }

    private static LogManager CurrentManager() {
        var config = Logging.Config;
        return (LogManager)config.GetType().GetProperty("Log").GetValue(config, null);
    }

    private static void WaitForDeferredCompletions() {
        Assert.That(SpinWait.SpinUntil(() => RetiringManagerCount() == 0, TimeSpan.FromSeconds(5)), Is.True,
            "Retirement workers should finish before checking the next session.");
    }

    [Test]
    public void RepeatedManagerRetirementsDoNotRetainWaitHandles() {
        Logging.Complete();
        var retirementType = typeof(Logging).GetNestedType("Retirement", BindingFlags.NonPublic);
        var waitHandles = retirementType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(field => typeof(WaitHandle).IsAssignableFrom(field.FieldType))
            .ToArray();

        for (var i = 0; i < 64; i++) {
            Logging.Configure(_ => { });
            Assert.That(Logging.Complete(TimeSpan.FromMilliseconds(-1)), Is.True,
                "Each new manager should fully retire before the next session is created.");
        }

        Assert.Multiple(() => {
            Assert.That(waitHandles, Is.Empty,
                "A retirement record must not own a kernel wait handle that outlives its callers.");
            Assert.That(RetiringManagerCount(), Is.Zero);
        });
    }

    [Test]
    public void DeferredCompletionCannotRetireNextLoggingSession() {
        CompletingLogService.Reset();
        HealthyCompleteLogService.Reset();
        var subscription = new DeferredCompletionSubscription();
        CompletingLogService.BlockComplete = true;
        Config = $@"
            log[complete].type = {typeof(CompletingLogService).AssemblyQualifiedName}
            log[complete].config.default.severity = info
        ";
        Log.Info("completion requested from service callback");
        Assert.That(CompletingLogService.CallbackReturned.Wait(TimeSpan.FromSeconds(5)), Is.True);
        Assert.That(CompletingLogService.CompleteEntered.Wait(TimeSpan.FromSeconds(5)), Is.True,
            "The detached manager should continue retirement on its worker.");
        Assert.That(RetiringManagerCount(), Is.EqualTo(1));

        Assert.That(Logging.Subscribe(subscription), Is.True);
        Config = $@"
            log[healthy].type = {typeof(HealthyCompleteLogService).AssemblyQualifiedName}
            log[healthy].config.default.severity = info
        ";
        CompletingLogService.AllowComplete.Set();
        Assert.That(CompletingLogService.ServiceCompleted.Wait(TimeSpan.FromSeconds(5)), Is.True);

        WaitForDeferredCompletions();
        var detachedByOldRequest = subscription.Detached.IsSet;
        var subscriptionStillRegistered = Logging.Subscribe(subscription) == false;
        Log.Info("next session survives deferred completion");
        Logging.Complete();

        Assert.Multiple(() => {
            Assert.That(detachedByOldRequest, Is.False, "An old completion request must not clear the new session.");
            Assert.That(subscriptionStillRegistered, Is.True);
            Assert.That(subscription.Entries.ToArray(), Is.EqualTo(["next session survives deferred completion"]));
            Assert.That(HealthyCompleteLogService.Entries.ToArray(), Is.EqualTo(["next session survives deferred completion"]));
            Assert.That(HealthyCompleteLogService.CompleteCount, Is.EqualTo(1));
        });
    }

    private sealed class RetiringCompletionLogService : ILogService {
        public static Action Callback { get; set; }

        public void Log(string name, string data, LogSeverity severity) => Callback();
        public void Complete() { }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CompletionFromRetiringCallbackCannotRetireNextSession(bool serviceCallback) {
        HealthyCompleteLogService.Reset();
        using var callbackEntered = new ManualResetEventSlim();
        using var releaseCallback = new ManualResetEventSlim();
        using var callbackReturned = new ManualResetEventSlim();
        void callback() {
            callbackEntered.Set();
            if (releaseCallback.Wait(TimeSpan.FromSeconds(5)) == false) {
                throw new TimeoutException("The test did not release the retiring callback.");
            }
            Logging.Complete();
            callbackReturned.Set();
        }
        object currentManager() {
            var config = Logging.Config;
            return config.GetType().GetProperty("Log").GetValue(config, null);
        }
        if (serviceCallback) {
            RetiringCompletionLogService.Callback = callback;
            Config = $@"
                log[retiring].type = {typeof(RetiringCompletionLogService).AssemblyQualifiedName}
                log[retiring].config.default.severity = info
            ";
        }
        else {
            Logging.EventThreshold = LogSeverity.Info;
            Logging.Event += (_, __) => callback();
        }
        var oldManager = currentManager();
        var subscription = new DeferredCompletionSubscription();
        var logging = Task.Run(() => Log.Info("retiring session"));
        Task completion = null;
        try {
            Assert.That(callbackEntered.Wait(TimeSpan.FromSeconds(5)), Is.True);
            completion = Task.Run(Logging.Complete);
            Assert.That(SpinWait.SpinUntil(() => !ReferenceEquals(oldManager, currentManager()), TimeSpan.FromSeconds(5)), Is.True,
                "Shutdown should detach the old manager before waiting for its callback.");
            Assert.That(Logging.Subscribe(subscription), Is.True);
            Config = $@"
                log[healthy].type = {typeof(HealthyCompleteLogService).AssemblyQualifiedName}
                log[healthy].config.default.severity = info
            ";
        }
        finally {
            releaseCallback.Set();
            logging.Wait(TimeSpan.FromSeconds(5));
            completion?.Wait(TimeSpan.FromSeconds(5));
            RetiringCompletionLogService.Callback = null;
        }
        Assert.That(callbackReturned.IsSet, Is.True);
        Assert.That(completion.IsCompleted, Is.True);
        WaitForDeferredCompletions();
        var detachedByOldRequest = subscription.Detached.IsSet;
        var subscriptionStillRegistered = Logging.Subscribe(subscription) == false;
        Log.Info("next session survives retiring callback");
        Logging.Complete();

        Assert.Multiple(() => {
            Assert.That(detachedByOldRequest, Is.False);
            Assert.That(subscriptionStillRegistered, Is.True);
            Assert.That(subscription.Entries.ToArray(), Is.EqualTo(["next session survives retiring callback"]));
            Assert.That(HealthyCompleteLogService.Entries.ToArray(), Is.EqualTo(["next session survives retiring callback"]));
            Assert.That(HealthyCompleteLogService.CompleteCount, Is.EqualTo(1));
        });
    }

    private sealed class ThrowingCompleteLogService : ILogService {
        public static ConcurrentQueue<string> Entries { get; } = new();
        public static bool FailNextComplete { get; set; }
        public static int CompleteCount { get; private set; }

        public static void Reset() {
            while (Entries.TryDequeue(out _)) {
            }
            FailNextComplete = true;
            CompleteCount = 0;
        }

        public void Log(string name, string data, LogSeverity severity) {
            Entries.Enqueue(data);
        }

        public void Complete() {
            CompleteCount++;
            if (FailNextComplete) {
                FailNextComplete = false;
                throw new InvalidOperationException("Expected test service completion failure.");
            }
        }
    }

    private sealed class HealthyCompleteLogService : ILogService {
        public static ConcurrentQueue<string> Entries { get; } = new();
        public static int CompleteCount { get; private set; }

        public static void Reset() {
            while (Entries.TryDequeue(out _)) {
            }
            CompleteCount = 0;
        }

        public void Log(string name, string data, LogSeverity severity) {
            Entries.Enqueue(data);
        }

        public void Complete() {
            CompleteCount++;
        }
    }

    [Test]
    public void CompletionErrorDoesNotPoisonNextLoggingSession() {
        ThrowingCompleteLogService.Reset();
        HealthyCompleteLogService.Reset();
        Config = $@"
                log[a_throw].type = {typeof(ThrowingCompleteLogService).AssemblyQualifiedName}
                log[a_throw].config.default.severity = info
                log[z_healthy].type = {typeof(HealthyCompleteLogService).AssemblyQualifiedName}
                log[z_healthy].config.default.severity = info
            ";
        Log.Info("before completion");

        var firstCompletionFailed = false;
        try {
            Logging.Complete();
        }
        catch (Exception) {
            firstCompletionFailed = true;
        }

        var healthyCompletionCount = HealthyCompleteLogService.CompleteCount;
        Config = $@"
                log[a_throw].type = {typeof(ThrowingCompleteLogService).AssemblyQualifiedName}
                log[a_throw].config.default.severity = info
                log[z_healthy].type = {typeof(HealthyCompleteLogService).AssemblyQualifiedName}
                log[z_healthy].config.default.severity = info
            ";
        Log.Info("after completion");

        var retryFailed = false;
        try {
            Logging.Complete();
        }
        catch (Exception) {
            retryFailed = true;
            SkipLoggingCompleteOnTearDown = true;
        }

        Assert.Multiple(() => {
            Assert.That(firstCompletionFailed, Is.True);
            Assert.That(healthyCompletionCount, Is.EqualTo(1));
            Assert.That(retryFailed, Is.False);
            Assert.That(ThrowingCompleteLogService.Entries.ToArray(), Is.EqualTo(["before completion", "after completion"]));
            Assert.That(HealthyCompleteLogService.Entries.ToArray(), Is.EqualTo(["before completion", "after completion"]));
        });
    }

    private sealed class GatedCompleteLogService : ILogService {
        public static ManualResetEventSlim CompleteEntered { get; } = new();
        public static ManualResetEventSlim AllowComplete { get; } = new();

        public static void Reset() {
            CompleteEntered.Reset();
            AllowComplete.Reset();
        }

        public void Log(string name, string data, LogSeverity severity) {
        }

        public void Complete() {
            CompleteEntered.Set();
            AllowComplete.Wait(TimeSpan.FromSeconds(5));
        }
    }

    private sealed class UnusedConfiguredLogService : ILogService {
        public static int Created;
        public static int Completed;

        public UnusedConfiguredLogService() => Interlocked.Increment(ref Created);
        public void Log(string name, string data, LogSeverity severity) { }
        public void Complete() => Interlocked.Increment(ref Completed);
    }

    private sealed class LeaseRetirementLogService : ILogService {
        public static ConcurrentQueue<string> Entries { get; } = new();
        public void Log(string name, string data, LogSeverity severity) => Entries.Enqueue(data);
        public void Complete() { }
    }

    [Test]
    public void CompletingUnusedConfiguredServiceDoesNotCreateIt() {
        UnusedConfiguredLogService.Created = 0;
        UnusedConfiguredLogService.Completed = 0;
        Config = $@"
            log[unused].type = {typeof(UnusedConfiguredLogService).AssemblyQualifiedName}
            log[unused].config.default.severity = info
        ";

        Logging.Complete();

        Assert.Multiple(() => {
            Assert.That(UnusedConfiguredLogService.Created, Is.Zero);
            Assert.That(UnusedConfiguredLogService.Completed, Is.Zero);
        });
    }

    [Test]
    public void RetirementRetriesInterruptedLeaseWaitBeforeDrainingAcceptedWork() {
        GatedCompleteLogService.Reset();
        while (LeaseRetirementLogService.Entries.TryDequeue(out _)) { }
        using var callbackEntered = new ManualResetEventSlim();
        using var releaseCallback = new ManualResetEventSlim();
        Logging.EventThreshold = LogSeverity.Info;
        Logging.Event += (_, entry) => {
            if (entry.LogList.Contains("leased entry")) {
                callbackEntered.Set();
                releaseCallback.Wait(TimeSpan.FromSeconds(5));
            }
        };
        Config = $@"
            log[accepted].type = {typeof(LeaseRetirementLogService).AssemblyQualifiedName}
            log[accepted].config.default.severity = info
            log[gated].type = {typeof(GatedCompleteLogService).AssemblyQualifiedName}
            log[gated].config.default.severity = info
        ";
        var logging = Task.Run(() => Log.Info("leased entry"));
        Assert.That(callbackEntered.Wait(TimeSpan.FromSeconds(5)), Is.True);

        try {
            Assert.That(Logging.Complete(TimeSpan.Zero), Is.False,
                "The active manager lease prevents retirement from completing immediately.");
            var instance = typeof(Logging).GetField("Instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            var retiring = (System.Collections.IDictionary)typeof(Logging)
                .GetField("RetiringManagers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);
            var record = retiring.Values.Cast<object>().Single();
            var worker = (Thread)record.GetType().GetField("Worker").GetValue(record);
            Assert.That(SpinWait.SpinUntil(() => (worker.ThreadState & ThreadState.WaitSleepJoin) != 0,
                TimeSpan.FromSeconds(5)), Is.True, "Retirement should wait on the active lease.");
            worker.Interrupt();
            releaseCallback.Set();

            Assert.That(logging.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(GatedCompleteLogService.CompleteEntered.Wait(TimeSpan.FromSeconds(5)), Is.True,
                "After the lease ends, retirement must drain accepted work and complete services.");
            Assert.That(LeaseRetirementLogService.Entries.ToArray(), Is.EqualTo(["leased entry"]));
        }
        finally {
            releaseCallback.Set();
            GatedCompleteLogService.AllowComplete.Set();
            logging.Wait(TimeSpan.FromSeconds(5));
        }

        Assert.That(Logging.Complete(TimeSpan.FromMilliseconds(-1)), Is.True,
            "A later infinite completion must include and finish the interrupted retirement.");
    }

    [Test]
    public void SubscriptionRemovalCompletionKeepsNewLoggingSessionAlive() {
        var subscription = new CompletingOnRemovalSubscription();
        Assert.That(Logging.Subscribe(subscription), Is.True);
        var oldManager = CurrentManager();
        var completion = Task.Run(() => Logging.Complete(TimeSpan.FromSeconds(5)));
        try {
            Assert.That(subscription.RemovalEntered.Wait(TimeSpan.FromSeconds(5)), Is.True);
            var newManager = CurrentManager();
            Assert.That(newManager, Is.Not.SameAs(oldManager));
            Assert.That(Logging.Subscribe(subscription), Is.True);
            subscription.AllowRemoval.Set();

            Assert.That(subscription.NestedCompletionReturned.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(completion.Wait(TimeSpan.FromSeconds(5)), Is.True);
            var current = CurrentManager();
            Assert.Multiple(() => {
                Assert.That(subscription.NestedCompletionResult, Is.False,
                    "A callback owned by the retired manager must not wait for itself or retire the new one.");
                Assert.That(current, Is.SameAs(newManager));
                Assert.That(Logging.Subscribe(subscription), Is.False,
                    "The old retirement callback must leave the new manager unchanged.");
            });
        }
        finally {
            subscription.AllowRemoval.Set();
            completion.Wait(TimeSpan.FromSeconds(5));
            Logging.Complete();
        }
    }

    [Test]
    public void FlowedCallbackFromRetiringManagerCannotCompleteNewSession() {
        using var childStarted = new ManualResetEventSlim();
        using var releaseChild = new ManualResetEventSlim();
        using var childReturned = new ManualResetEventSlim();
        Task child = null;
        Logging.EventThreshold = LogSeverity.Info;
        Logging.Event += (_, entry) => {
            if (entry.LogList.Contains("start old callback")) {
                child = Task.Run(() => {
                    childStarted.Set();
                    releaseChild.Wait(TimeSpan.FromSeconds(10));
                    Logging.Complete();
                    childReturned.Set();
                });
            }
        };

        Log.Info("start old callback");
        Assert.That(childStarted.Wait(TimeSpan.FromSeconds(5)), Is.True);
        try {
            Logging.Complete();
            Logging.EventThreshold = LogSeverity.Info;
            var newManager = CurrentManager();
            releaseChild.Set();
            Assert.That(childReturned.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(child?.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(CurrentManager(), Is.SameAs(newManager));
        }
        finally {
            releaseChild.Set();
            child?.Wait(TimeSpan.FromSeconds(5));
        }
    }

    [Test]
    public void TimedCompletionReturnsAtDeadlineAndLaterCompletionFlushesRetirements() {
        GatedCompleteLogService.Reset();
        HealthyCompleteLogService.Reset();
        Config = $@"
            log[gated].type = {typeof(GatedCompleteLogService).AssemblyQualifiedName}
            log[gated].config.default.severity = info
        ";
        Log.Info("accepted before timed completion");

        Assert.That(Logging.Complete(TimeSpan.FromMilliseconds(40)), Is.False);
        Assert.That(GatedCompleteLogService.CompleteEntered.Wait(TimeSpan.FromSeconds(5)), Is.True,
            "Background cleanup should reach service completion after draining the queue.");
        Assert.That(Logging.Complete(TimeSpan.Zero), Is.False,
            "A second completion sees the earlier pending retirement and shares its zero timeout.");

        Config = $@"
            log[healthy].type = {typeof(HealthyCompleteLogService).AssemblyQualifiedName}
            log[healthy].config.default.severity = info
        ";
        Log.Info("new session while old completion is gated");
        GatedCompleteLogService.AllowComplete.Set();

        Assert.That(Logging.Complete(TimeSpan.FromMilliseconds(-1)), Is.True,
            "The -1 millisecond timeout requests an unbounded flush of old and current managers.");
        Assert.That(HealthyCompleteLogService.Entries.ToArray(), Is.EqualTo(["new session while old completion is gated"]));
        Assert.That(HealthyCompleteLogService.CompleteCount, Is.EqualTo(1));
    }

    [Test]
    public void TimedCompletionValidatesTimeoutBeforeRetiringAnything() {
        Assert.That(Logging.Complete(TimeSpan.Zero), Is.True);
        Assert.That(Logging.Complete(TimeSpan.FromMilliseconds(-1)), Is.True);
        Assert.Throws<ArgumentOutOfRangeException>(() => Logging.Complete(TimeSpan.FromTicks(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Logging.Complete(TimeSpan.FromMilliseconds(-2)));
    }

    [Test]
    public void ProcessExitHandlerDrainsAcceptedMessages() {
        HealthyCompleteLogService.Reset();
        Config = $@"
            log[healthy].type = {typeof(HealthyCompleteLogService).AssemblyQualifiedName}
            log[healthy].config.default.severity = info
        ";
        Log.Info("flush at process exit");
        var processExit = typeof(Logging).GetMethod("ProcessExit", BindingFlags.Static | BindingFlags.NonPublic);

        processExit.Invoke(null, [null, EventArgs.Empty]);

        Assert.Multiple(() => {
            Assert.That(HealthyCompleteLogService.Entries.ToArray(), Is.EqualTo(["flush at process exit"]));
            Assert.That(HealthyCompleteLogService.CompleteCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void CompletionOnTaskWithFlowedManagerLeaseDoesNotWaitForThatLease() {
        var config = Logging.Config;
        var oldManager = (LogManager)config.GetType().GetProperty("Log").GetValue(config, null);
        using var childStarted = new ManualResetEventSlim();
        using var childReturned = new ManualResetEventSlim();
        Task completion = null;
        var returnedBeforeLeaseRelease = false;

        Logging.Configure(_ => {
            completion = Task.Run(() => {
                childStarted.Set();
                Logging.Complete();
                childReturned.Set();
            });
            Assert.That(childStarted.Wait(TimeSpan.FromSeconds(5)), Is.True);
            returnedBeforeLeaseRelease = childReturned.Wait(TimeSpan.FromSeconds(2));
        });
        completion?.Wait(TimeSpan.FromSeconds(5));
        var newConfig = Logging.Config;
        var newManager = (LogManager)newConfig.GetType().GetProperty("Log").GetValue(newConfig, null);
        WaitForDeferredCompletions();

        Assert.Multiple(() => {
            Assert.That(returnedBeforeLeaseRelease, Is.True,
                "A child task carrying the manager context must defer instead of waiting on its parent lease.");
            Assert.That(newManager, Is.Not.SameAs(oldManager));
        });
    }

    private sealed class CompletionWindowSubscription : ILogSubscription {
        public ConcurrentQueue<string> Entries { get; } = new();

        event EventHandler ILogSubscription.ThresholdChanged {
            add { }
            remove { }
        }

        public LogSeverity Threshold(Type type) => LogSeverity.Info;

        public void Receive(ILogEntry entry) {
            foreach (var item in entry.LogList) {
                Entries.Enqueue(item);
            }
        }
    }

    [Test]
    public void WorkAcceptedDuringCompletionBelongsToNextLoggingSession() {
        GatedCompleteLogService.Reset();
        Config = $@"
                log[gated].type = {typeof(GatedCompleteLogService).AssemblyQualifiedName}
                log[gated].config.default.severity = info
            ";
        Log.Info("before completion");

        var subscription = new CompletionWindowSubscription();
        var completion = Task.Run(Logging.Complete);
        var completionEntered = false;
        var subscribedDuringCompletion = false;
        var duplicateWasRejected = false;
        var completionFinished = false;
        try {
            completionEntered = GatedCompleteLogService.CompleteEntered.Wait(TimeSpan.FromSeconds(2));
            if (completionEntered) {
                subscribedDuringCompletion = Logging.Subscribe(subscription);
                Log.Info("during completion");
            }
            GatedCompleteLogService.AllowComplete.Set();
            completionFinished = completion.Wait(TimeSpan.FromSeconds(5));
            if (completionFinished) {
                duplicateWasRejected = Logging.Subscribe(subscription) == false;
                Log.Info("after completion");
                Logging.Complete();
            }
        }
        finally {
            GatedCompleteLogService.AllowComplete.Set();
            if (completion.IsCompleted == false) {
                completion.Wait(TimeSpan.FromSeconds(5));
            }
        }

        Assert.Multiple(() => {
            Assert.That(completionEntered, Is.True);
            Assert.That(completionFinished, Is.True);
            Assert.That(subscribedDuringCompletion, Is.True);
            Assert.That(duplicateWasRejected, Is.True);
            Assert.That(subscription.Entries.ToArray(), Is.EqualTo(["during completion", "after completion"]));
        });
    }

    private sealed class ThrowingLogCallbackService : ILogService {
        public void Log(string name, string data, LogSeverity severity) {
            throw new InvalidOperationException("Expected test service log failure.");
        }

        public void Complete() {
        }
    }

    [Test]
    public void LogFailureDoesNotPreventDeliveryToLaterServices() {
        HealthyCompleteLogService.Reset();
        Config = $@"
                log[a_throw].type = {typeof(ThrowingLogCallbackService).AssemblyQualifiedName}
                log[a_throw].config.default.severity = info
                log[z_healthy].type = {typeof(HealthyCompleteLogService).AssemblyQualifiedName}
                log[z_healthy].config.default.severity = info
            ";

        Log.Info("survives throwing service");
        Logging.Complete();

        Assert.That(HealthyCompleteLogService.Entries.ToArray(), Is.EqualTo(["survives throwing service"]));
    }

    [Test]
    public void ServiceInitializationFailureDoesNotPreventDeliveryToLaterServices() {
        HealthyCompleteLogService.Reset();
        Config = @"
                log[a_invalid].type = System.Int32, NoSuch, Version=abc
                log[a_invalid].config.default.severity = info
                log[z_healthy].type = " + typeof(HealthyCompleteLogService).AssemblyQualifiedName + @"
                log[z_healthy].config.default.severity = info
            ";

        Log.Info("survives initialization failure");
        var completionReportedInitializationFailure = false;
        try {
            Logging.Complete();
        }
        catch (AggregateException) {
            completionReportedInitializationFailure = true;
        }

        Assert.Multiple(() => {
            Assert.That(completionReportedInitializationFailure, Is.True);
            Assert.That(HealthyCompleteLogService.Entries.ToArray(), Is.EqualTo(["survives initialization failure"]));
        });
    }

    private sealed class ThresholdProbeLogService : ILogService {
        public static ConcurrentQueue<string> Entries { get; } = new();

        public static void Clear() {
            while (Entries.TryDequeue(out _)) {
            }
        }

        public void Log(string name, string data, LogSeverity severity) {
            Entries.Enqueue(data);
        }

        public void Complete() {
        }
    }

    [Test]
    public void EnabledIncludesOtherServiceDefaultWhenTypeOverrideExists() {
        ThresholdProbeLogService.Clear();
        Config = $@"
                log[strict].type = {typeof(ThresholdProbeLogService).AssemblyQualifiedName}
                log[strict].config[LoggingTest].severity = warn
                log[fallback].type = {typeof(ThresholdProbeLogService).AssemblyQualifiedName}
                log[fallback].config.default.severity = debug
            ";

        var enabled = Log.Info();
        Log.Info("message");
        Logging.Complete();

        using (Assert.EnterMultipleScope()) {
            Assert.That(enabled, Is.True);
            Assert.That(ThresholdProbeLogService.Entries.ToArray(), Is.EqualTo(["message"]));
        }
    }

    [Test]
    public void EnabledRespectsExplicitNoneTypeOverride() {
        ThresholdProbeLogService.Clear();
        Config = $@"
                log[probe].type = {typeof(ThresholdProbeLogService).AssemblyQualifiedName}
                log[probe].config.default.severity = debug
                log[probe].config[LoggingTest].severity = none
            ";

        var enabled = Log.Info();
        Log.Info("suppressed");
        Logging.Complete();

        using (Assert.EnterMultipleScope()) {
            Assert.That(enabled, Is.False);
            Assert.That(ThresholdProbeLogService.Entries, Is.Empty);
        }
    }

    [Test]
    public void CustomLogServiceIsInstantiated() {
        ConfigTest();
        Log.Info("Some message");
        Logging.Complete();
        var actual = TestLogService.Instance.Items.Select(i => i.Data);
        var expected = new[] { "LoggingTest [inf] Some message" };
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void CustomLogServiceIsPassedFormattedData() {
        ConfigTest();
        Log.Info("Here", "are some", "messages.");
        Logging.Complete();
        var actual = TestLogService.Instance.Items.Select(i => i.Data).Single();
        var expected = @"LoggingTest [inf]
  Here
  are some
  messages.";
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void CustomLogServiceIsPassedFormattedException() {
        ConfigTest();
        var err = default(Exception);
        try {
            throw new Exception("Oops...");
        }
        catch (Exception ex) {
            Log.Error(err = ex);
        }
        Logging.Complete();
        var actual = TestLogService.Instance.Items.Select(i => i.Data).Single();
        var expected =
            "LoggingTest [err]" + Environment.NewLine +
            string.Join(Environment.NewLine, err
                .ToString()
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => $"  {line}"));
        Assert.That(actual, Is.EqualTo(expected));
    }

    private sealed class XYPoint {
        public int X { get; set; }
        public int Y { get; set; }
    }

    [Test]
    public void CustomLogServiceIsPassedCustomFormat() {
        ConfigTest();
        Logging.Format(typeof(XYPoint), obj => {
            var p = (XYPoint)obj;
            return new[] {
                $"XY {{",
                $"  x: {p.X}",
                $"  y: {p.Y}",
                $"}}"
            };
        });
        Log.Warn(new XYPoint { X = 1, Y = 2 }, new XYPoint { X = 3, Y = 4 });
        Logging.Complete();
        var actual = TestLogService.Instance.Items.Select(i => i.Data).Single();
        var expected = @"LoggingTest [wrn]
  XY {
    x: 1
    y: 2
  }
  XY {
    x: 3
    y: 4
  }";
        Assert.That(actual, Is.EqualTo(expected));
    }

    private sealed class CustomLogQueue : ILogService {
        public static Queue<string> Queue { get; } = new();

        public void Log(string name, string data, LogSeverity severity) {
            Queue.Enqueue(data);
        }

        public void Complete() {
        }
    }

    [Test]
    public void ExampleLogServiceWorksAsShown() {
        LogConf.ConfigureLogging($@"
                log[queue].type = {typeof(CustomLogQueue).AssemblyQualifiedName}
                log[queue].config.default.severity = info
            ");
        Log.Info("Put me in the queue.");
        Logging.Complete();

        var message = CustomLogQueue.Queue.Dequeue();
        Assert.That(message, Is.EqualTo("Put me in the queue."));
    }
}
