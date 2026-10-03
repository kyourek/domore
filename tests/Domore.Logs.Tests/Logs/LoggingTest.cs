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

        public CompletingLogService() {
        }

        public static void Reset() {
            CallbackReturned.Reset();
            ServiceCompleted.Reset();
        }

        public void Log(string name, string data, LogSeverity severity) {
            Logging.Complete();
            CallbackReturned.Set();
        }

        public void Complete() {
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

    private static int DeferredCompletionCount() {
        var flags = BindingFlags.Static | BindingFlags.NonPublic;
        var instance = typeof(Logging).GetField("Instance", flags).GetValue(null);
        var locker = typeof(Logging).GetField("ManagerLocker", flags).GetValue(null);
        var requests = (HashSet<LogManager>)typeof(Logging)
            .GetField("DeferredCompletions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);
        lock (locker) {
            return requests.Count;
        }
    }

    private static void WaitForDeferredCompletions() {
        Assert.That(SpinWait.SpinUntil(() => DeferredCompletionCount() == 0, TimeSpan.FromSeconds(5)), Is.True,
            "Queued completion requests should finish before checking the next session.");
    }

    [Test]
    public void DeferredCompletionCannotRetireNextLoggingSession() {
        CompletingLogService.Reset();
        HealthyCompleteLogService.Reset();
        var subscription = new DeferredCompletionSubscription();
        var completeLocker = typeof(Logging).GetField("CompleteLocker", BindingFlags.Static | BindingFlags.NonPublic)
            .GetValue(null);

        // Hold shutdown so callback requests cannot run until the next session exists.
        lock (completeLocker) {
            Config = $@"
                log[complete].type = {typeof(CompletingLogService).AssemblyQualifiedName}
                log[complete].config.default.severity = info
            ";
            Log.Info("first completion request");
            Log.Info("second completion request");
            Assert.That(CompletingLogService.CallbackReturned.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Logging.Complete();
            Assert.That(CompletingLogService.ServiceCompleted.IsSet, Is.True);
            Assert.That(DeferredCompletionCount(), Is.EqualTo(1), "Repeated requests should share one queued completion.");

            Assert.That(Logging.Subscribe(subscription), Is.True);
            Config = $@"
                log[healthy].type = {typeof(HealthyCompleteLogService).AssemblyQualifiedName}
                log[healthy].config.default.severity = info
            ";
        }

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
