using System;
using System.Diagnostics;

namespace Domore.Logs {
    public sealed class Logging {
        private static readonly object ManagerLocker = new object();
        private static readonly object CompleteLocker = new object();
        private readonly static Logging Instance = new Logging();

        private LogManager Manager {
            get {
                if (_Manager == null) {
                    lock (ManagerLocker) {
                        if (_Manager == null) {
                            _Manager = new LogManager();
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

        public static event LogEventHandler LogEvent {
            add => Instance.Manager.LogEvent += value;
            remove => Instance.Manager.LogEvent -= value;
        }

        [Obsolete($"LogEventSeverity is deprecated. Please use {nameof(LogEventThreshold)} instead.")]
        public static LogSeverity LogEventSeverity {
            get => LogEventThreshold;
            set => LogEventThreshold = value;
        }

        public static LogSeverity LogEventThreshold {
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
