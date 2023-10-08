using NUnit.Framework;

namespace Domore.Logs {
    [TestFixture]
    public sealed class LogSeverityTest {
        [Test]
        public void InfoIsGreaterThanDebug() {
            Assert.IsTrue(LogSeverity.Info > LogSeverity.Debug);
        }

        [Test]
        public void WarnIsGreaterThanInfo() {
            Assert.IsTrue(LogSeverity.Warn > LogSeverity.Info);
        }

        [Test]
        public void ErrorIsGreaterThanWarn() {
            Assert.IsTrue(LogSeverity.Error > LogSeverity.Warn);
        }

        [Test]
        public void CriticalIsGreaterThanError() {
            Assert.IsTrue(LogSeverity.Critical > LogSeverity.Error);
        }
    }
}
