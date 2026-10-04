using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Domore.Logs;
[TestFixture]
[NonParallelizable]
public sealed class LogEntryTest {
    private static LogEntry NewEntry() {
        var date = new DateTime(2026, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc);
        return new(typeof(LogEntryTest), date, LogSeverity.Info, new[] { "message" });
    }

    [Test]
    public void UndefinedSeverityUsesNumericFallback() {
        var date = new DateTime(2026, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc);
        var entry = new LogEntry(typeof(LogEntryTest), date, (LogSeverity)7, new[] { "message" });
        Assert.That(entry.LogData("{sev}"), Is.EqualTo("7 message"));
    }

    [Test]
    public void LogListDoesNotExposeMutableEntryArray() {
        var entry = NewEntry();
        var logList = ((ILogEntry)entry).LogList;
        if (logList is string[] array) {
            array[0] = "changed";
        }

        Assert.That(entry.LogData("prefix"), Is.EqualTo("prefix message"));
        Assert.That(logList, Is.Not.SameAs(entry.EntryList));
        Assert.That(logList, Is.Not.InstanceOf<string[]>());
        var list = (IList<string>)logList;
        Assert.That(list.IsReadOnly, Is.True);
        Assert.Throws<NotSupportedException>(() => list[0] = "changed");
    }

    [Test]
    public void DateAndTimeUseInvariantCulture() {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalDefaultThreadCulture = CultureInfo.DefaultThreadCurrentCulture;
        try {
            var culture = (CultureInfo)new CultureInfo("th-TH").Clone();
            culture.DateTimeFormat.TimeSeparator = ".";
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.CurrentCulture = culture;
            var actual = NewEntry().LogData("{dat} {tim}");
            Assert.That(actual, Is.EqualTo("2026-01-02 03:04:05.678 message"));
        }
        finally {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.DefaultThreadCurrentCulture = originalDefaultThreadCulture;
        }
    }

    [Test]
    public void LocalDateAndTimeUseInvariantCulture() {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalDefaultThreadCulture = CultureInfo.DefaultThreadCurrentCulture;
        var culture = (CultureInfo)new CultureInfo("th-TH").Clone();
        culture.DateTimeFormat.TimeSeparator = ".";
        try {
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.CurrentCulture = culture;
            var entry = NewEntry();
            var local = entry.EntryDate.ToLocalTime();
            var expectedDate = local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var expectedTime = local.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
            var expected = $"{expectedDate} {expectedTime} message";
            var actual = entry.LogData("{loc.dat} {loc.tim}");
            Assert.That(actual, Is.EqualTo(expected));
        }
        finally {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.DefaultThreadCurrentCulture = originalDefaultThreadCulture;
        }
    }
}
