using System;
using System.Diagnostics;

namespace Domore.Logs {
    public sealed class Logging {
        private static readonly object ManagerLocker = new object();
        private static readonly object CompleteLocker = new object();
        private readonly static Logging Instance = new Logging();

        private LogServiceManager Manager {
            get {
                if (_Manager == null) {
                    lock (ManagerLocker) {
                        if (_Manager == null) {
                            _Manager = new LogServiceManager();
                        }
                    }
                }
                return _Manager;
            }
            set => _Manager = value;
        }
        private LogServiceManager _Manager;

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

        public static LogSeverity LogEventSeverity {
            get => Instance.Manager.LogEventSeverity;
            set => Instance.Manager.LogEventSeverity = value;
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
