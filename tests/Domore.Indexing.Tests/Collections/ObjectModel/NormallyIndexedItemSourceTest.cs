using NUnit.Framework;
using System;
using System.Threading;

namespace Domore.Collections.ObjectModel;

[TestFixture, Parallelizable]
public sealed class NormallyIndexedItemSourceTest {
    private Implementation Subject {
        get => field ??= new Implementation();
        set => field = value;
    }

    [SetUp]
    public void SetUp() {
        Subject = null;
    }

    [TearDown]
    public void TearDown() {
    }

    [Test]
    public void Item_GetsSameItemWhenTheKeyOnlyDiffersInCaseAndWhitespace() {
        var item1 = Subject["hello world"];
        var item2 = Subject["  helloWorlD\t"];
        Assert.That(item1, Is.SameAs(item2));
    }

    [Test]
    public void Contains_ReturnsTrueWhenTheKeyOnlyDiffersInCaseAndWhitespace() {
        _ = Subject["hello world"];
        Assert.That(Subject.Contains(" helloWorlD\t"), Is.True);
    }

    [Test]
    public void Dispose_DisposesItems() {
        var item1 = Subject["1"];
        var item2 = Subject["2"];
        Subject.Dispose(true);
        using (Assert.EnterMultipleScope()) {
            Assert.That(item1.DisposeCount, Is.EqualTo(1));
            Assert.That(item2.DisposeCount, Is.EqualTo(1));
        }
    }

    [Test]
    public void Dispose_DoesNotDisposesItemsWhenDisposingIsFalse() {
        var item1 = Subject["1"];
        var item2 = Subject["2"];
        Subject.Dispose(false);
        using (Assert.EnterMultipleScope()) {
            Assert.That(item1.DisposeCount, Is.EqualTo(0));
            Assert.That(item2.DisposeCount, Is.EqualTo(0));
        }
    }

    private class Implementation : NormallyIndexedItemSource<Implementation.Item> {
        protected override Item CreateItem(string index) =>
            new(index);

        public sealed class Item : IIndexedItem<string>, IDisposable {
            public int DisposeCount => _DisposeCount;
            private int _DisposeCount;

            public string Index { get; }

            public Item(string index) {
                Index = index;
            }

            void IDisposable.Dispose() {
                Interlocked.Increment(ref _DisposeCount);
            }
        }

        new public void Dispose(bool disposing) {
            base.Dispose(disposing);
        }
    }
}
