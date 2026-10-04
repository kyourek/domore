using NUnit.Framework;
using System;
using System.Globalization;

namespace Domore.Logs;

[TestFixture]
public sealed class LogEntryRepairTest {
    [Test]
    public void DateFieldsUseInvariantGregorianFormatting() {
        var previous = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
            var entry = new LogEntry(typeof(LogEntryRepairTest),
                new DateTime(2020, 2, 3, 4, 5, 6, 789, DateTimeKind.Utc),
                LogSeverity.Info,
                ["message"]);

            Assert.That(entry.LogData("{dat}|{tim}"), Is.EqualTo("2020-02-03|04:05:06.789 message"));
        }
        finally {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [TestCase((LogSeverity)99, "99")]
    [TestCase(LogSeverity.None, "none")]
    public void UndefinedAndNoneSeverityHaveSafeFormatting(LogSeverity severity, string abbreviation) {
        var entry = new LogEntry(typeof(LogEntryRepairTest), DateTime.UtcNow, severity, ["message"]);
        Assert.That(entry.LogData("{sev}"), Is.EqualTo(abbreviation + " message"));
    }
}
