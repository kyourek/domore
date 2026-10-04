using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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

    private readonly object FormatLocker = new();
    private readonly string[] EntryList;
    private readonly ReadOnlyCollection<string> EntryView;

    private DateTime LocalDate {
        get {
            lock (FormatLocker) {
                return _LocalDate ??= EntryDate.ToLocalTime();
            }
        }
    }
    private DateTime? _LocalDate;

    private string GetFormat(string format) {
        var severity = Sev.TryGetValue(EntrySeverity, out var abbreviation)
            ? abbreviation
            : EntrySeverity.ToString().ToLowerInvariant();
        var s = format
            .Replace("{log}", LogName)
            .Replace("{sev}", severity)
            .Replace("{dat}", EntryDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            .Replace("{tim}", EntryDate.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture))
            .Replace("{loc.dat}", LocalDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            .Replace("{loc.tim}", LocalDate.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture));
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

    public string LogName {
        get {
            lock (FormatLocker) {
                return field ??= LogType.Name;
            }
        }
    }

    public Type LogType { get; }
    public DateTime EntryDate { get; }
    public LogSeverity EntrySeverity { get; }
    public long RetainedTextBytes { get; }

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
        long retainedTextBytes = 0;
        foreach (var line in EntryList) {
            if (line is not null) {
                retainedTextBytes += 2L * line.Length;
            }
        }
        RetainedTextBytes = retainedTextBytes;
        EntryDate = entryDate;
        EntrySeverity = entrySeverity;
    }

    public string LogData(string format) {
        lock (FormatLocker) {
            return GetFormat(format ?? "");
        }
    }

    Type ILogEntry.LogType => LogType;
    DateTime ILogEntry.LogDate => EntryDate;
    LogSeverity ILogEntry.LogSeverity => EntrySeverity;
    IEnumerable<string> ILogEntry.LogList => EntryView;
}
