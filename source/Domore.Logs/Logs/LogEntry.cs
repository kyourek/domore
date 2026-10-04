using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Domore.Logs;

internal sealed class LogEntry : ILogEntry {
    private static readonly Dictionary<LogSeverity, string> Sev = new() {
        { LogSeverity.Critical, "crt" },
        { LogSeverity.Debug, "dbg" },
        { LogSeverity.Error, "err" },
        { LogSeverity.Info, "inf" },
        { LogSeverity.Warn, "wrn" }
    };

    private readonly Dictionary<string, string> Format = [];
    private readonly string[] EntryList;
    private readonly ReadOnlyCollection<string> EntryView;

    private DateTime LocalDate => _LocalDate ??= EntryDate.ToLocalTime();
    private DateTime? _LocalDate;

    private string GetFormat(string format) {
        var s = format
            .Replace("{log}", LogName)
            .Replace("{sev}", Sev[EntrySeverity])
            .Replace("{dat}", EntryDate.ToString("yyyy-MM-dd"))
            .Replace("{tim}", EntryDate.ToString("HH:mm:ss.fff"))
            .Replace("{loc.dat}", LocalDate.ToString("yyyy-MM-dd"))
            .Replace("{loc.tim}", LocalDate.ToString("HH:mm:ss.fff"));
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
    public LogSeverity EntrySeverity { get; }

    public LogEntry(Type logType,
                    DateTime entryDate,
                    LogSeverity entrySeverity,
                    string[] entryList,
                    bool takeOwnership = false) {
        if (entryList is null) {
            throw new ArgumentNullException(nameof(entryList));
        }
        LogType = logType ?? throw new ArgumentNullException(nameof(logType));
        /*
         * Ownership may only be transferred when no caller retains the array.
         */
        EntryList = takeOwnership ? entryList : (string[])entryList.Clone();
        EntryView = Array.AsReadOnly(EntryList);
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
    IEnumerable<string> ILogEntry.LogList => EntryView;
}
