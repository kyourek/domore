using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace Domore.Logs {
    public sealed class Logging {
        private static readonly object ManagerLocker = new();
        private static readonly object CompleteLocker = new();
        private static readonly Logging Instance = new();

        private LogManager Manager {
            get {
                if (_Manager == null) {
                    lock (ManagerLocker) {
                        if (_Manager == null) {
                            var manager = new LogManager();
                            Thread.MemoryBarrier();
                            _Manager = manager;
                        }
                    }
                }
                return _Manager;
            }
            set => _Manager = value;
        }
        private LogManager _Manager;

        private Logging() {
        }

        internal bool Log(Logger logger, LogSeverity severity) {
            return Manager.Log(severity, logger?.Type);
        }

        internal void Log(Logger logger, LogSeverity severity, params object[] data) {
            Manager.Log(severity, logger?.Type, data);
        }

        internal static void Notify(object obj) {
            try { Debug.WriteLine(obj); } catch { }
            try { Trace.WriteLine(obj); } catch { }
            try { Console.WriteLine(obj); } catch { }
        }

        [Obsolete($"LogEvent is deprecated. Please use {nameof(Event)} instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static event LogEventHandler LogEvent {
            add => Event += value;
            remove => Event -= value;
        }

        public static event LogEventHandler Event {
            add => Instance.Manager.LogEvent += value;
            remove => Instance.Manager.LogEvent -= value;
        }

        [Obsolete($"LogEventSeverity is deprecated. Please use {nameof(EventThreshold)} instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static LogSeverity LogEventSeverity {
            get => EventThreshold;
            set => EventThreshold = value;
        }

        public static LogSeverity EventThreshold {
            get => Instance.Manager.LogEventThreshold;
            set => Instance.Manager.LogEventThreshold = value;
        }

        public static bool Subscribe(ILogSubscription subscription) {
            return Instance.Manager.Subscribe(subscription);
        }

        public static bool Unsubscribe(ILogSubscription subscription) {
            return Instance.Manager.Unsubscribe(subscription);
        }

        public static object Config =>
            new { Log = Instance.Manager };

        public static ILog For(Type type) {
            return new Logger(type, Instance);
        }

        public static void Format(Type type, Func<object, string[]> toString) {
            Instance.Manager.Formatter.Format(type, toString);
        }

        public static void Complete() {
            lock (CompleteLocker) {
                using (var manager = Instance.Manager) {
                    manager.Complete();
                }
                Instance.Manager = null;
            }
        }
    }
}
