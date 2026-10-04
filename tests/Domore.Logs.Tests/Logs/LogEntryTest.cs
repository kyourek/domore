using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Domore.Logs;

[TestFixture]
public sealed class LogEntryTest {
    [Test]
    public void PublicMessageListCannotBeMutatedThroughListInterfaces() {
        ILogEntry entry = new LogEntry(typeof(LogEntryTest), DateTime.UtcNow, LogSeverity.Info, ["original", "second"]);
        var messages = entry.LogList;

        Assert.Multiple(() => {
            Assert.That(messages, Is.Not.InstanceOf<string[]>());
            Assert.That(entry.LogList, Is.SameAs(messages), "Observers should share one cached view.");
            Assert.Throws<NotSupportedException>(() => ((IList<string>)messages)[0] = "changed");
            Assert.Throws<NotSupportedException>(() => ((IList)messages)[0] = "changed");
            Assert.Throws<NotSupportedException>(() => ((ICollection<string>)messages).Clear());
            Assert.That(messages, Is.EqualTo(["original", "second"]));
        });
    }

    [Test]
    public void CallerOwnedArrayCannotChangeEntryMessagesOrServiceFormatting() {
        var source = new[] { "original", "second" };
        var entry = new LogEntry(typeof(LogEntryTest), DateTime.UtcNow, LogSeverity.Info, source);

        source[0] = "changed";

        Assert.Multiple(() => {
            Assert.That(((ILogEntry)entry).LogList, Is.EqualTo(["original", "second"]));
            Assert.That(entry.LogData(null), Is.EqualTo("original" + Environment.NewLine + "second"));
        });
    }
}
