using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Threading.Tasks {
    [TestFixture]
    public sealed class TaskCacheTest {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task<T> Get<T>(T obj) {
            return obj;
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        [Test]
        public async Task Ready_ReturnsResult() {
            var expected = new object();
            var subject = new TaskCache<object>(async _ => await Get(expected));
            var actual = await subject.Ready(CancellationToken.None);
            Assert.That(actual, Is.SameAs(expected));
        }
    }
}
