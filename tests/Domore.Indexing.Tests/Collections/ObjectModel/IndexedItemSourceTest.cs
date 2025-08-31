using NUnit.Framework;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Domore.Collections.ObjectModel {
    [TestFixture, Parallelizable]
    internal sealed class IndexedItemSourceTest {
        private sealed class Implementation1 : IndexedItemSource<int, Implementation1.Item> {
            protected sealed override Item CreateItem(int index) {
                return new(index);
            }

            public Implementation1(object syncRoot) : base(syncRoot: syncRoot) {
            }

            public sealed class Item(int index) : IIndexedItem<int> {
                public int Index { get; } = index;
            }
        }

        [Test]
        public void SynchronizedSourceIsSynchronized() {
            var items = new ConcurrentBag<Implementation1.Item>();
            var subject = new Implementation1(syncRoot: new());
            var workNum = 0;
            var workCount = 1000;
            for (var i = 0; i < workCount; i++) {
                ThreadPool.QueueUserWorkItem(state: null, callBack: _ => {
                    for (var i = 0; i < 1000; i++) {
                        items.Add(subject[i]);
                        Thread.Sleep(0);
                    }
                    Interlocked.Add(ref workNum, 1);
                });
            }
            SpinWait.SpinUntil(() => workNum == 1000, 5000);
            var actual = items.Distinct().Count();
            var expected = 1000;
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
