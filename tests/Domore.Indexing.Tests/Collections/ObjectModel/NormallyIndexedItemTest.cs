using NUnit.Framework;

namespace Domore.Collections.ObjectModel;

[TestFixture, Parallelizable]
internal sealed class NormallyIndexedItemTest {
    private sealed class Implementation1 : NormallyIndexedItem<Implementation1> {
    }

    [TestCase("  Hello, World!  ", "Hello,World!")]
    [TestCase(null, "")]
    [TestCase("\t", "")]
    public void Source_Item_Index_IsExpected(string s1, string s2) {
        var subject = new Implementation1.Source();
        var item = subject[s1];
        var actual = ((IIndexedItem<string>)item).Index;
        var expected = s2;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase("  Hello, World!  ", "hello,  world! ")]
    [TestCase("   ", "")]
    [TestCase("   ", null)]
    [TestCase(null, "")]
    public void Source_ItemsAreSame(string s1, string s2) {
        var subject = new Implementation1.Source();
        var item1 = subject[s1];
        var item2 = subject[s2];
        Assert.That(item1, Is.SameAs(item2));
    }
}
