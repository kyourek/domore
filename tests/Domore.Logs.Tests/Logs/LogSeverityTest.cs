using NUnit.Framework;

namespace Domore.Logs; 
[TestFixture]
public sealed class LogSeverityTest {
    [Test]
    public void InfoIsGreaterThanDebug() {
        Assert.That(LogSeverity.Info > LogSeverity.Debug);
    }

    [Test]
    public void WarnIsGreaterThanInfo() {
        Assert.That(LogSeverity.Warn > LogSeverity.Info);
    }

    [Test]
    public void ErrorIsGreaterThanWarn() {
        Assert.That(LogSeverity.Error > LogSeverity.Warn);
    }

    [Test]
    public void CriticalIsGreaterThanError() {
        Assert.That(LogSeverity.Critical > LogSeverity.Error);
    }
}
