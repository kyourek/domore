using System;

namespace Domore.Logs {
    internal sealed class LogServiceProxy {
        private static readonly LogServiceFactory Factory = new LogServiceFactory();

        private readonly object Locker = new object();

        private sealed class None : ILogService {
            void ILogService.Log(string name, string data, LogSeverity severity) {
            }

            void ILogService.Complete() {
            }
        }

        public ILogService Service {
            get {
                if (_Service == null) {
                    lock (Locker) {
                        if (_Service == null) {
                            _Service = Factory.Create(Type) ?? new None();
                        }
                    }
                }
                return _Service;
            }
        } 
        private ILogService _Service;

        public LogServiceConfig Config {
            get {
                if (_Config == null) {
                    lock (Locker) {
                        if (_Config == null) {
                            _Config = new LogServiceConfig();
                        }
                    }
                }
                return _Config;
            }
        }
        private LogServiceConfig _Config;

        public string Type {
            get => _Type ?? (_Type = Name);
            set {
                if (_Type != value) {
                    lock (Locker) {
                        _Type = value;
                        _Service = null;
                    }
                }
            }
        }
        private string _Type;

        public string Name { get; }

        public LogServiceProxy(string name) {
            Name = name;
        }

        public void Log(LogEntry entry) {
            if (null == entry) throw new ArgumentNullException(nameof(entry));
            var sev = entry.LogSeverity;
            var name = entry.LogName;
            var limit = Config[name].Severity ?? Config.Default.Severity;
            if (limit.HasValue && limit.Value != LogSeverity.None && limit.Value <= sev) {
                var frmt = Config[name].Format ?? Config.Default.Format;
                var data = entry.LogData(frmt);
                Service.Log(name, data, sev);
            }
        }

        public void Complete() {
            Service.Complete();
        }
    }
}
