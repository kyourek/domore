using NUnit.Framework;

namespace Domore.Collections.ObjectModel {
    [TestFixture]
    public class NormallyIndexedCollectionTest {
        private Implementation Subject {
            get => _Subject ?? (_Subject = new Implementation());
            set => _Subject = value;
        }
        private Implementation _Subject;

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

        private class Implementation : NormallyIndexedCollection<Implementation.Item> {
            protected override Item CreateItem(string index) =>
                new Item(index);

            public class Item : IIndexedItem<string> {
                public string Index { get; }

                public Item(string index) {
                    Index = index;
                }
            }
        }
    }
}
