using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace Domore.Logs {
    /// <summary>
    /// Provides implementations of <see cref="ILog"/>.
    /// </summary>
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

        /// <summary>
        /// Raised when a log event occurs.
        /// </summary>
        public static event LogEventHandler Event {
            add => Instance.Manager.LogEvent += value;
            remove => Instance.Manager.LogEvent -= value;
        }

        /// <summary>
        /// Gets or sets the threshold for log events.
        /// </summary>
        public static LogSeverity EventThreshold {
            get => Instance.Manager.LogEventThreshold;
            set => Instance.Manager.LogEventThreshold = value;
        }

        /// <summary>
        /// Adds a subscription to log events.
        /// </summary>
        /// <param name="subscription">The subscription to be added.</param>
        /// <returns>True if the subscription was added. Otherwise, false.</returns>
        public static bool Subscribe(ILogSubscription subscription) {
            return Instance.Manager.Subscribe(subscription);
        }

        /// <summary>
        /// Removes a subscription to log events.
        /// </summary>
        /// <param name="subscription">The subscription to be removed.</param>
        /// <returns>True if the subscription was removed. Otherwise, false.</returns>
        public static bool Unsubscribe(ILogSubscription subscription) {
            return Instance.Manager.Unsubscribe(subscription);
        }

        /// <summary>
        /// Gets an object that may be used to configure log behavior.
        /// </summary>
        public static object Config =>
            new { Log = Instance.Manager };

        /// <summary>
        /// Gets an instance of <see cref="ILog"/> for the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type whose log is returned.</param>
        /// <returns>The instance of <see cref="ILog"/> for the <paramref name="type"/>.</returns>
        public static ILog For(Type type) {
            return new Logger(type, Instance);
        }

        /// <summary>
        /// Provides a callback used to format instances of <paramref name="type"/> in log messages.
        /// </summary>
        /// <param name="type">The type of instances to be formatted.</param>
        /// <param name="toString">The callback called to format instances of <paramref name="type"/>.</param>
        public static void Format(Type type, Func<object, string[]> toString) {
            Instance.Manager.Formatter.Format(type, toString);
        }

        /// <summary>
        /// Completes all logging.
        /// </summary>
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
