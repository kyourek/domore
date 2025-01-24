using NUnit.Framework;
using System.Collections.Generic;

namespace Domore.Conf.Converters {
    using Extensions;
    using NUnit.Framework.Legacy;

    [TestFixture]
    public sealed class ConfTextTest {
        private class Parent {
            [ConfText]
            public Child Child { get; set; }
        }

        private class Child {
            public string Name { get; set; }
            public int Age { get; set; }
            [ConfListItems]
            public List<string> FavoriteIceCreamFlavors { get; set; }
        }

        [Test]
        public void ConvertsFromConfText() {
            var parent = new Parent().ConfFrom(key: "", text: @"
                child = {
                    Name = Paul
                    Age = 7
                    favorite ice cream flavors = chocolate, strawberry, NOT vanilla
                }
            ");
            Assert.That(parent.Child.Name, Is.EqualTo("Paul"));
            Assert.That(parent.Child.Age, Is.EqualTo(7));
            CollectionAssert.AreEqual(new[] { "chocolate", "strawberry", "NOT vanilla" }, parent.Child.FavoriteIceCreamFlavors);
        }
    }
}
