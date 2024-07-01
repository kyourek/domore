using System;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Logs {
    internal sealed class LogEntry : ILogEntry {
        private static readonly Dictionary<LogSeverity, string> Sev = new() {
            { LogSeverity.Critical, "crt" },
            { LogSeverity.Debug, "dbg" },
            { LogSeverity.Error, "err" },
            { LogSeverity.Info, "inf" },
            { LogSeverity.Warn, "wrn" }
        };

        private readonly Dictionary<string, string> Format = [];

        private string GetFormat(string format) {
            var s = format
                .Replace("{log}", LogName)
                .Replace("{sev}", Sev[EntrySeverity])
                .Replace("{dat}", EntryDate.ToString("yyyy-MM-dd"))
                .Replace("{tim}", EntryDate.ToString("HH:mm:ss.fff"));
            var logList = EntryList;
            if (logList.Length == 1) {
                return s == ""
                    ? logList[0]
                    : s + " " + logList[0];
            }
            return s == ""
                ? string.Join(Environment.NewLine, logList)
                : (s + Environment.NewLine + string.Join(Environment.NewLine, logList.Select(line => $"  {line}")));
        }

        public string LogName =>
            _LogName ?? (
            _LogName = LogType.Name);
        private string _LogName;

        public Type LogType { get; }
        public DateTime EntryDate { get; }
        public string[] EntryList { get; }
        public LogSeverity EntrySeverity { get; }

        public LogEntry(Type logType, DateTime entryDate, LogSeverity entrySeverity, string[] entryList) {
            LogType = logType ?? throw new ArgumentNullException(nameof(logType));
            EntryList = entryList ?? throw new ArgumentNullException(nameof(entryList));
            EntryDate = entryDate;
            EntrySeverity = entrySeverity;
        }

        public string LogData(string format) {
            var key = format ?? "";
            if (Format.TryGetValue(key, out var value) == false) {
                Format[key] = value = GetFormat(key);
            }
            return value;
        }

        Type ILogEntry.LogType => LogType;
        DateTime ILogEntry.LogDate => EntryDate;
        LogSeverity ILogEntry.LogSeverity => EntrySeverity;
        IEnumerable<string> ILogEntry.LogList => EntryList;
    }
}
