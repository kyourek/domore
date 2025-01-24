using NUnit.Framework;
using System.Collections.Generic;

namespace Domore.Conf.Converters {
    using Extensions;
    using NUnit.Framework.Legacy;

    [TestFixture]
    public sealed class ConfKeyTest {
        private class Parent {
            [ConfKey]
            public Child Child { get; set; }

            [ConfKey]
            public Child SecondChild { get; set; }
        }

        private class Child {
            public string Name { get; set; }
            public int Age { get; set; }
            [ConfListItems, Conf(nameof(FavoriteIceCreamFlavors), "FavoriteIceCream")]
            public List<string> FavoriteIceCreamFlavors { get; set; }
        }

        [Test]
        public void ConvertsFromConfKey() {
            var parent = new Parent().ConfFrom(@"
                paul.name = Paul
                paul.Age = 7
                paul.favorite ice cream flavors = {
                    chocolate,
                    strawberry,
                    NOT vanilla
                }
                parent.child = paul
            ");
            Assert.That(parent.Child.Name, Is.EqualTo("Paul"));
            Assert.That(parent.Child.Age, Is.EqualTo(7));
            CollectionAssert.AreEqual(new[] { "chocolate", "strawberry", "NOT vanilla" }, parent.Child.FavoriteIceCreamFlavors);
        }

        [Test]
        public void ConvertsFromMultipleConfKeys() {
            var parent = new Parent().ConfFrom(@"
                paul.name = Paul
                paul.Age = 7
                paul.favorite ice cream flavors = {
                    chocolate,
                    strawberry,
                    NOT vanilla
                }
                parent.child        = paul
                parent.second child = mary

                Mary.Name = Mary
                Mary.age = 4
                mary.favorite IceCream = {
                    Vanilla
                }
            ");
            Assert.That(parent.Child.Name, Is.EqualTo("Paul"));
            Assert.That(parent.Child.Age, Is.EqualTo(7));
            CollectionAssert.AreEqual(new[] { "chocolate", "strawberry", "NOT vanilla" }, parent.Child.FavoriteIceCreamFlavors);

            Assert.That(parent.SecondChild.Name, Is.EqualTo("Mary"));
            Assert.That(parent.SecondChild.Age, Is.EqualTo(4));
            CollectionAssert.AreEqual(new[] { "Vanilla" }, parent.SecondChild.FavoriteIceCreamFlavors);
        }

        private class Parent2 {
            [ConfKey(PropertySetByKey = "Name")]
            public Child Child { get; set; }

            [ConfKey(PropertySetByKey = "Name")]
            public Child SecondChild { get; set; }
        }

        [Test]
        public void SetsPropertyWithKey() {
            var parent = new Parent2().ConfFrom(key: "parent", text: @"
                paul.Age = 7
                paul.favorite ice cream flavors = {
                    chocolate,
                    strawberry,
                    NOT vanilla
                }
                parent.child        = paul
                parent.second child = MaryJo

                MaryJo.age = 4
                maryjo.favorite IceCream = {
                    Vanilla
                }
            ");
            Assert.That(parent.Child.Name, Is.EqualTo("paul"));
            Assert.That(parent.Child.Age, Is.EqualTo(7));
            CollectionAssert.AreEqual(new[] { "chocolate", "strawberry", "NOT vanilla" }, parent.Child.FavoriteIceCreamFlavors);

            Assert.That(parent.SecondChild.Name, Is.EqualTo("MaryJo"));
            Assert.That(parent.SecondChild.Age, Is.EqualTo(4));
            CollectionAssert.AreEqual(new[] { "Vanilla" }, parent.SecondChild.FavoriteIceCreamFlavors);
        }
    }
}
