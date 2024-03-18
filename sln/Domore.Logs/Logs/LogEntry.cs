using System;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Logs {
    internal sealed class LogEntry : ILogEntry {
        private static readonly Dictionary<LogSeverity, string> Sev = new Dictionary<LogSeverity, string> {
            { LogSeverity.Critical, "crt" },
            { LogSeverity.Debug, "dbg" },
            { LogSeverity.Error, "err" },
            { LogSeverity.Info, "inf" },
            { LogSeverity.Warn, "wrn" }
        };

        private readonly Dictionary<string, string> Format = new Dictionary<string, string>();

        private string GetFormat(string format) {
            var s = format
                .Replace("{log}", LogName)
                .Replace("{sev}", Sev[LogSeverity])
                .Replace("{dat}", LogDate.ToString("yyyy-MM-dd"))
                .Replace("{tim}", LogDate.ToString("HH:mm:ss.fff"));
            var logList = LogList;
            if (logList.Length == 1) {
                return s == ""
                    ? logList[0]
                    : s + " " + logList[0];
            }
            return s == ""
                ? string.Join(Environment.NewLine, logList)
                : (s + Environment.NewLine + string.Join(Environment.NewLine, logList.Select(line => $"  {line}")));
        }

        public Type LogType { get; }
        public DateTime LogDate { get; }
        public string[] LogList { get; }
        public LogSeverity LogSeverity { get; }

        public string LogName =>
            _LogName ?? (
            _LogName = LogType.Name);
        private string _LogName;

        public LogEntry(Type logType, DateTime logDate, LogSeverity logSeverity, string[] logList) {
            if (null == logType) throw new ArgumentNullException(nameof(logType));
            if (null == logList) throw new ArgumentNullException(nameof(logList));
            LogType = logType;
            LogDate = logDate;
            LogList = logList;
            LogSeverity = logSeverity;
        }

        public string LogData(string format) {
            var key = format ?? "";
            if (Format.TryGetValue(key, out var value) == false) {
                Format[key] = value = GetFormat(key);
            }
            return value;
        }

        IEnumerable<string> ILogEntry.LogList => 
            LogList;
    }
}
