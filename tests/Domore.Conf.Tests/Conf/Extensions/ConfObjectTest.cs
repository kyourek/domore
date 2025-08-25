using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Conf.Extensions;

[TestFixture]
internal class ConfObjectTest {
    private class OnePropertyClass {
        public string StringProp { get; set; }
    }

    private class TwoPropertyClass : OnePropertyClass {
        public double DoubleProp { get; set; }
    }

    [Test]
    public void ConfText_GetsTwoProperties() {
        var contents = new TwoPropertyClass { StringProp = "Hello World", DoubleProp = 1.23 }.ConfText("Settings");
        var expected = string.Join(Environment.NewLine, "Settings.DoubleProp = 1.23", "Settings.StringProp = Hello World");
        Assert.That(contents, Is.EqualTo(expected));
    }

    private class ComplexClass : TwoPropertyClass {
        public OnePropertyClass Child { get; } = new OnePropertyClass();
    }

    [Test]
    public void ConfText_GetsComplexText() {
        var subject = new ComplexClass();
        subject.StringProp = "mystr";
        subject.DoubleProp = 4.321;
        subject.Child.StringProp = "My other str";
        var actual = subject.ConfText();
        var expected = string.Join(Environment.NewLine,
            "ComplexClass.Child.StringProp = My other str",
            "ComplexClass.DoubleProp = 4.321",
            "ComplexClass.StringProp = mystr");
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ConfText_GetsComplexTextOnSingleLine() {
        var subject = new ComplexClass();
        subject.StringProp = "mystr";
        subject.DoubleProp = 4.321;
        subject.Child.StringProp = "My other str";
        var actual = subject.ConfText(multiline: false);
        var expected = string.Join(";",
            "ComplexClass.Child.StringProp=My other str",
            "ComplexClass.DoubleProp=4.321",
            "ComplexClass.StringProp=mystr");
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ConfText_GetsComplexTextOnSingleLineWithEmptyKey() {
        var subject = new ComplexClass();
        subject.StringProp = "mystr";
        subject.DoubleProp = 4.321;
        subject.Child.StringProp = "My other str";
        var actual = subject.ConfText(key: "", multiline: false);
        var expected = "Child.StringProp=My other str;DoubleProp=4.321;StringProp=mystr";
        Assert.That(actual, Is.EqualTo(expected));
    }

    private static void ConfText_CanRoundTripComplexClass(Action<ComplexClass, ComplexClass> assert) {
        var expected = new ComplexClass();
        expected.StringProp = "mystr";
        expected.DoubleProp = 4.321;
        expected.Child.StringProp = "My other str";

        var text = expected.ConfText();
        var conf = new ConfContainer { Source = text };
        var actual = conf.Configure(new ComplexClass());
        assert(actual, expected);
    }

    [Test]
    public void ConfText_CanRoundTripComplexClassStringProp() {
        ConfText_CanRoundTripComplexClass((actual, expected) =>
            Assert.That(actual.StringProp, Is.EqualTo(expected.StringProp)));
    }

    [Test]
    public void ConfText_CanRoundTripComplexClassDoubleProp() {
        ConfText_CanRoundTripComplexClass((actual, expected) =>
            Assert.That(actual.DoubleProp, Is.EqualTo(expected.DoubleProp)));
    }

    [Test]
    public void ConfText_CanRoundTripComplexClassChild() {
        ConfText_CanRoundTripComplexClass((actual, expected) =>
            Assert.That(actual.Child.StringProp, Is.EqualTo(expected.Child.StringProp)));
    }

    [Test]
    public void ConfText_CanRoundTripComplexClassMultiline() {
        var expected = new ComplexClass();
        expected.StringProp = string.Join("\n", "mystr", "on", "", "multiple", "lines");
        expected.DoubleProp = 4.321;
        expected.Child.StringProp = "My other str";

        var text = expected.ConfText();
        var conf = new ConfContainer { Source = text };
        var actual = conf.Configure(new ComplexClass());
        Assert.That(actual.StringProp, Is.EqualTo(expected.StringProp));
    }

    [Test]
    public void ConfFrom_CanRoundTripComplexClassMultiline() {
        var expected = new ComplexClass();
        expected.StringProp = string.Join("\n", "mystr", "on", "", "multiple", "lines");
        expected.DoubleProp = 4.321;
        expected.Child.StringProp = "My other str";

        var text = expected.ConfText();
        var actual = new ComplexClass().ConfFrom(text);
        Assert.That(actual.StringProp, Is.EqualTo(expected.StringProp));
    }

    private class DictedClass {
        public Dictionary<int, string> DictOfStrings { get; } = new Dictionary<int, string>();
    }

    [Test]
    public void ConfText_GetsDictOfStrings() {
        var subject = new DictedClass();
        subject.DictOfStrings[0] = "hello";
        subject.DictOfStrings[1] = "world";
        var actual = subject.ConfText();
        var expected = string.Join(Environment.NewLine,
            "DictedClass.DictOfStrings[0] = hello",
            "DictedClass.DictOfStrings[1] = world");
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ConfText_CanRoundTripDictOfStrings() {
        var subject = new DictedClass();
        subject.DictOfStrings[0] = "hello";
        subject.DictOfStrings[1] = "world";
        var text = subject.ConfText();
        var conf = new ConfContainer { Source = text };
        var copy = conf.Configure(new DictedClass());
        var actual = copy.DictOfStrings;
        var expected = subject.DictOfStrings;
        CollectionAssert.AreEqual(expected, actual);
    }

    [Test]
    public void ConfFrom_CanRoundTripDictOfStrings() {
        var subject = new DictedClass();
        subject.DictOfStrings[0] = "hello";
        subject.DictOfStrings[1] = "world";
        var text = subject.ConfText();
        var copy = new DictedClass().ConfFrom(text);
        var actual = copy.DictOfStrings;
        var expected = subject.DictOfStrings;
        CollectionAssert.AreEqual(expected, actual);
    }

    private class ComplexDictedClass {
        public Dictionary<int, ComplexClass> Dict { get; } = new Dictionary<int, ComplexClass>();
    }

    [Test]
    public void ConfText_GetsComplexDictedClass() {
        var subject = new ComplexDictedClass();
        subject.Dict[0] = new ComplexClass();
        subject.Dict[0].Child.StringProp = "hello";
        subject.Dict[0].DoubleProp = 1.23;
        subject.Dict[0].StringProp = "world";
        subject.Dict[1] = new ComplexClass();
        subject.Dict[1].Child.StringProp = "HELLO";
        subject.Dict[1].DoubleProp = 2.34;
        subject.Dict[1].StringProp = "WORLD";
        var actual = subject.ConfText("subj");
        var expected = string.Join(Environment.NewLine,
            "subj.Dict[0].Child.StringProp = hello",
            "subj.Dict[0].DoubleProp = 1.23",
            "subj.Dict[0].StringProp = world",
            "subj.Dict[1].Child.StringProp = HELLO",
            "subj.Dict[1].DoubleProp = 2.34",
            "subj.Dict[1].StringProp = WORLD");
        Assert.That(actual, Is.EqualTo(expected));
    }

    private void ConfText_CanRoundTripComplexDictedClass(bool multiline) {
        var subject = new ComplexDictedClass();
        subject.Dict[0] = new ComplexClass();
        subject.Dict[0].Child.StringProp = "hello";
        subject.Dict[0].DoubleProp = 1.23;
        subject.Dict[0].StringProp = "world";
        subject.Dict[1] = new ComplexClass();
        subject.Dict[1].Child.StringProp = "HELLO";
        subject.Dict[1].DoubleProp = 2.34;
        subject.Dict[1].StringProp = "WORLD";
        var text = subject.ConfText();
        var conf = new ConfContainer { Source = text };
        var copy = conf.Configure(new ComplexDictedClass());
        Assert.That(copy.Dict[0].Child.StringProp, Is.EqualTo(subject.Dict[0].Child.StringProp));
        Assert.That(copy.Dict[0].DoubleProp, Is.EqualTo(subject.Dict[0].DoubleProp));
        Assert.That(copy.Dict[0].StringProp, Is.EqualTo(subject.Dict[0].StringProp));
        Assert.That(copy.Dict[1].Child.StringProp, Is.EqualTo(subject.Dict[1].Child.StringProp));
        Assert.That(copy.Dict[1].DoubleProp, Is.EqualTo(subject.Dict[1].DoubleProp));
        Assert.That(copy.Dict[1].StringProp, Is.EqualTo(subject.Dict[1].StringProp));
    }

    private void ConfFrom_CanRoundTripComplexDictedClass(bool multiline) {
        var subject = new ComplexDictedClass();
        subject.Dict[0] = new ComplexClass();
        subject.Dict[0].Child.StringProp = "hello";
        subject.Dict[0].DoubleProp = 1.23;
        subject.Dict[0].StringProp = "world";
        subject.Dict[1] = new ComplexClass();
        subject.Dict[1].Child.StringProp = "HELLO";
        subject.Dict[1].DoubleProp = 2.34;
        subject.Dict[1].StringProp = "WORLD";
        var text = subject.ConfText();
        var copy = new ComplexDictedClass().ConfFrom(text);
        Assert.That(copy.Dict[0].Child.StringProp, Is.EqualTo(subject.Dict[0].Child.StringProp));
        Assert.That(copy.Dict[0].DoubleProp, Is.EqualTo(subject.Dict[0].DoubleProp));
        Assert.That(copy.Dict[0].StringProp, Is.EqualTo(subject.Dict[0].StringProp));
        Assert.That(copy.Dict[1].Child.StringProp, Is.EqualTo(subject.Dict[1].Child.StringProp));
        Assert.That(copy.Dict[1].DoubleProp, Is.EqualTo(subject.Dict[1].DoubleProp));
        Assert.That(copy.Dict[1].StringProp, Is.EqualTo(subject.Dict[1].StringProp));
    }

    [Test]
    public void ConfText_CanRoundTripComplexDictedClass() {
        ConfText_CanRoundTripComplexDictedClass(true);
    }

    [Test]
    public void ConfText_CanRoundTripComplexDictedClassOnSingleLine() {
        ConfText_CanRoundTripComplexDictedClass(false);
    }

    private class ComplexListedClass {
        public List<ComplexClass> List { get; } = new List<ComplexClass>();
    }

    [Test]
    public void ConfText_GetsComplexListedClass() {
        var subject = new ComplexListedClass();
        subject.List.Add(new ComplexClass());
        subject.List[0].Child.StringProp = "hello";
        subject.List[0].DoubleProp = 1.23;
        subject.List[0].StringProp = "world";
        subject.List.Add(new ComplexClass());
        subject.List[1].Child.StringProp = "HELLO";
        subject.List[1].DoubleProp = 2.34;
        subject.List[1].StringProp = "WORLD";
        var actual = subject.ConfText();
        var expected = string.Join(Environment.NewLine,
            "ComplexListedClass.List[0].Child.StringProp = hello",
            "ComplexListedClass.List[0].DoubleProp = 1.23",
            "ComplexListedClass.List[0].StringProp = world",
            "ComplexListedClass.List[1].Child.StringProp = HELLO",
            "ComplexListedClass.List[1].DoubleProp = 2.34",
            "ComplexListedClass.List[1].StringProp = WORLD");
        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ConfText_CanRoundTripComplexListedClass(bool multiline) {
        var subject = new ComplexListedClass();
        subject.List.Add(new ComplexClass());
        subject.List[0].Child.StringProp = "hello";
        subject.List[0].DoubleProp = 1.23;
        subject.List[0].StringProp = "world";
        subject.List.Add(new ComplexClass());
        subject.List[1].Child.StringProp = "HELLO";
        subject.List[1].DoubleProp = 2.34;
        subject.List[1].StringProp = "WORLD";
        var text = subject.ConfText(multiline: multiline);
        var conf = new ConfContainer { Source = text };
        var copy = conf.Configure(new ComplexListedClass());
        Assert.That(copy.List[0].Child.StringProp, Is.EqualTo(subject.List[0].Child.StringProp));
        Assert.That(copy.List[0].DoubleProp, Is.EqualTo(subject.List[0].DoubleProp));
        Assert.That(copy.List[0].StringProp, Is.EqualTo(subject.List[0].StringProp));
        Assert.That(copy.List[1].Child.StringProp, Is.EqualTo(subject.List[1].Child.StringProp));
        Assert.That(copy.List[1].DoubleProp, Is.EqualTo(subject.List[1].DoubleProp));
        Assert.That(copy.List[1].StringProp, Is.EqualTo(subject.List[1].StringProp));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ConfFrom_CanRoundTripComplexListedClass(bool multiline) {
        var subject = new ComplexListedClass();
        subject.List.Add(new ComplexClass());
        subject.List[0].Child.StringProp = "hello";
        subject.List[0].DoubleProp = 1.23;
        subject.List[0].StringProp = "world";
        subject.List.Add(new ComplexClass());
        subject.List[1].Child.StringProp = "HELLO";
        subject.List[1].DoubleProp = 2.34;
        subject.List[1].StringProp = "WORLD";
        var text = subject.ConfText(multiline: multiline);
        var copy = new ComplexListedClass().ConfFrom(text);
        Assert.That(copy.List[0].Child.StringProp, Is.EqualTo(subject.List[0].Child.StringProp));
        Assert.That(copy.List[0].DoubleProp, Is.EqualTo(subject.List[0].DoubleProp));
        Assert.That(copy.List[0].StringProp, Is.EqualTo(subject.List[0].StringProp));
        Assert.That(copy.List[1].Child.StringProp, Is.EqualTo(subject.List[1].Child.StringProp));
        Assert.That(copy.List[1].DoubleProp, Is.EqualTo(subject.List[1].DoubleProp));
        Assert.That(copy.List[1].StringProp, Is.EqualTo(subject.List[1].StringProp));
    }

    [Test]
    public void ConfText_CanRoundTripComplexListedClassWithBracketedContent1() {
        var subject = new ComplexListedClass();
        subject.List.Add(new ComplexClass());
        subject.List[0].Child.StringProp = "hello";
        subject.List[0].DoubleProp = 1.23;
        subject.List[0].StringProp = @"world
on more than one

line";
        subject.List.Add(new ComplexClass());
        subject.List[1].Child.StringProp = @"HELLO there

            how's it going?";
        subject.List[1].DoubleProp = 2.34;
        subject.List[1].StringProp = "WORLD";
        var text = subject.ConfText();
        var conf = new ConfContainer { Source = text };
        var copy = conf.Configure(new ComplexListedClass());
        Assert.That(copy.List[0].Child.StringProp, Is.EqualTo(subject.List[0].Child.StringProp));
        Assert.That(copy.List[0].DoubleProp, Is.EqualTo(subject.List[0].DoubleProp));
        Assert.That(copy.List[0].StringProp, Is.EqualTo(subject.List[0].StringProp));
        Assert.That(copy.List[1].Child.StringProp, Is.EqualTo(subject.List[1].Child.StringProp));
        Assert.That(copy.List[1].DoubleProp, Is.EqualTo(subject.List[1].DoubleProp));
        Assert.That(copy.List[1].StringProp, Is.EqualTo(subject.List[1].StringProp));
    }

    [Test]
    public void ConfText_CanRoundTripComplexListedClassWithBracketedContent2() {
        var subject = new ComplexListedClass();
        subject.List.Add(new ComplexClass());
        subject.List[0].Child.StringProp = @"hello
            here's
            some more }
            {lines}";
        subject.List[0].DoubleProp = 1.23;
        subject.List[0].StringProp = @"world
on more than one

line";
        subject.List.Add(new ComplexClass());
        subject.List[1].Child.StringProp = @"HELLO there

            how's it going?";
        subject.List[1].DoubleProp = 2.34;
        subject.List[1].StringProp = @"{
WORLD   and    
            some other
            stuff}
}...";
        var text = subject.ConfText();
        var conf = new ConfContainer { Source = text };
        var copy = conf.Configure(new ComplexListedClass());
        Assert.That(copy.List[0].Child.StringProp, Is.EqualTo(subject.List[0].Child.StringProp));
        Assert.That(copy.List[0].DoubleProp, Is.EqualTo(subject.List[0].DoubleProp));
        Assert.That(copy.List[0].StringProp, Is.EqualTo(subject.List[0].StringProp));
        Assert.That(copy.List[1].Child.StringProp, Is.EqualTo(subject.List[1].Child.StringProp));
        Assert.That(copy.List[1].DoubleProp, Is.EqualTo(subject.List[1].DoubleProp));
        Assert.That(copy.List[1].StringProp, Is.EqualTo(subject.List[1].StringProp));
    }

    [Test]
    public void ConfFrom_CanRoundTripComplexListedClassWithBracketedContent2() {
        var subject = new ComplexListedClass();
        subject.List.Add(new ComplexClass());
        subject.List[0].Child.StringProp = @"hello
            here's
            some more }
            {lines}";
        subject.List[0].DoubleProp = 1.23;
        subject.List[0].StringProp = @"world
on more than one

line";
        subject.List.Add(new ComplexClass());
        subject.List[1].Child.StringProp = @"HELLO there

            how's it going?";
        subject.List[1].DoubleProp = 2.34;
        subject.List[1].StringProp = @"{
WORLD   and    
            some other
            stuff}
}...";
        var text = subject.ConfText();
        var copy = new ComplexListedClass().ConfFrom(text);
        Assert.That(copy.List[0].Child.StringProp, Is.EqualTo(subject.List[0].Child.StringProp));
        Assert.That(copy.List[0].DoubleProp, Is.EqualTo(subject.List[0].DoubleProp));
        Assert.That(copy.List[0].StringProp, Is.EqualTo(subject.List[0].StringProp));
        Assert.That(copy.List[1].Child.StringProp, Is.EqualTo(subject.List[1].Child.StringProp));
        Assert.That(copy.List[1].DoubleProp, Is.EqualTo(subject.List[1].DoubleProp));
        Assert.That(copy.List[1].StringProp, Is.EqualTo(subject.List[1].StringProp));
    }

    private class ClassWithListExposedAsICollection {
        public ICollection<Inner> Inners {
            get => _Inners ?? (_Inners = new List<Inner>());
            set => _Inners = value;
        }
        private ICollection<Inner> _Inners;

        public sealed class Inner {
            public double Value { get; set; }
        }
    }

    [Test]
    public void ConfText_GetsConfFromListExposedAsICollection() {
        var obj = new ClassWithListExposedAsICollection();
        obj.Inners.Add(new ClassWithListExposedAsICollection.Inner { Value = 1.1 });
        obj.Inners.Add(new ClassWithListExposedAsICollection.Inner { Value = 1.2 });
        obj.Inners.Add(new ClassWithListExposedAsICollection.Inner { Value = 1.3 });
        var actual = obj.ConfText("item");
        var expected = string.Join(Environment.NewLine,
            "item.Inners[0].Value = 1.1",
            "item.Inners[1].Value = 1.2",
            "item.Inners[2].Value = 1.3");
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ConfText_GetsConfOfList() {
        var list = new List<string> { "hello", "world", "hey", "earth" };
        var actual = list.ConfText();
        var expected = string.Join(Environment.NewLine,
            "String[0] = hello",
            "String[1] = world",
            "String[2] = hey",
            "String[3] = earth");
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ConfText_GetsConfOfListWithBracketedText() {
        var list = new List<string> { "hello", "world", "hey", @"
earth
            is on   
MANY


lines
" };
        var actual = list.ConfText();
        var expected = string.Join(Environment.NewLine,
            "String[0] = hello",
            "String[1] = world",
            "String[2] = hey",
            "String[3] = {" + @"

earth
            is on   
MANY


lines

}");
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ConfText_GetsConfOfDictionary() {
        var dict = new Dictionary<string, double> { { "hello", 1.23 }, { "World", 4.56 } };
        var actual = dict.ConfText();
        var expected = string.Join(Environment.NewLine,
            "Double[hello] = 1.23",
            "Double[World] = 4.56");
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ConfText_GetsConfOfDictionaryWithBracketedText() {
        var dict = new Dictionary<string, string> {
            { "hello", @"here's{
}some
text...
  
  
" },
            { "World", @"
}                      some
more lines{" } };
        var actual = dict.ConfText();

        var expected = @"String[hello] = {
here's{
}some
text...
  
  

}
String[World] = {

}                      some
more lines{
}";
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ConfText_CanRoundTripDictionary() {
        var obj1 = new ClassWithListExposedAsICollection();
        obj1.Inners.Add(new ClassWithListExposedAsICollection.Inner { Value = 1.1 });
        obj1.Inners.Add(new ClassWithListExposedAsICollection.Inner { Value = 1.2 });
        obj1.Inners.Add(new ClassWithListExposedAsICollection.Inner { Value = 1.3 });

        var obj2 = new ClassWithListExposedAsICollection();
        obj2.Inners.Add(new ClassWithListExposedAsICollection.Inner { Value = 2.1 });
        obj2.Inners.Add(new ClassWithListExposedAsICollection.Inner { Value = 2.2 });
        obj2.Inners.Add(new ClassWithListExposedAsICollection.Inner { Value = 2.3 });

        var dict = new Dictionary<string, ClassWithListExposedAsICollection> {
            { "obj1", obj1 },
            { "obj2", obj2 }
        };
        var text = dict.ConfText("item");
        var conf = new ConfContainer { Source = text };
        var copy = conf.Configure(key => new ClassWithListExposedAsICollection(), "item")
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        Assert.That(copy["obj1"].Inners.ElementAt(0).Value, Is.EqualTo(1.1));
        Assert.That(copy["obj1"].Inners.ElementAt(1).Value, Is.EqualTo(1.2));
        Assert.That(copy["obj1"].Inners.ElementAt(2).Value, Is.EqualTo(1.3));

        Assert.That(copy["obj2"].Inners.ElementAt(0).Value, Is.EqualTo(2.1));
        Assert.That(copy["obj2"].Inners.ElementAt(1).Value, Is.EqualTo(2.2));
        Assert.That(copy["obj2"].Inners.ElementAt(2).Value, Is.EqualTo(2.3));
    }

    private class ConfText_CanBePassedEmptyStringForKey_Helper : ComplexListedClass {
        public string MyName { get; set; }
        public float MyValue { get; set; }
    }

    [Test]
    public void ConfText_CanBePassedEmptyStringForKey() {
        var subject = new ConfText_CanBePassedEmptyStringForKey_Helper();
        subject.MyName = "thename";
        subject.MyValue = 3.21F;
        subject.List.Add(new ComplexClass());
        subject.List[0].Child.StringProp = "hello";
        subject.List[0].DoubleProp = 1.23;
        subject.List[0].StringProp = "world";
        subject.List.Add(new ComplexClass());
        subject.List[1].Child.StringProp = "HELLO";
        subject.List[1].DoubleProp = 2.34;
        subject.List[1].StringProp = "WORLD";
        var actual = subject.ConfText(key: "");
        var expected = string.Join(Environment.NewLine,
            "List[0].Child.StringProp = hello",
            "List[0].DoubleProp = 1.23",
            "List[0].StringProp = world",
            "List[1].Child.StringProp = HELLO",
            "List[1].DoubleProp = 2.34",
            "List[1].StringProp = WORLD",
            "MyName = thename",
            "MyValue = 3.21");
        Assert.That(actual, Is.EqualTo(expected));
    }

    private class ConfText_IsEmptyForObjectWithEmptyList_Object {
        public List<string> ListProp {
            get => _ListProp ?? (_ListProp = new List<string>());
            set => _ListProp = value;
        }
        private List<string> _ListProp;
    }

    [Test]
    public void ConfText_IsEmptyForObjectWithEmptyList() {
        var subject = new ConfText_IsEmptyForObjectWithEmptyList_Object();
        var actual = subject.ConfText(key: "");
        var expected = "";
        Assert.That(actual, Is.EqualTo(expected));
    }

    private sealed class ConfFrom_CanInstantiateItemsWithPrivateConstructor_Object {
        public List<MyItem> ItemList { get; set; }

        public sealed class MyItem {
            private MyItem() {
            }
            public string Foo { get; private set; }
        }
    }

    [Test]
    public void Foo() {
        var text = @"
                ItemList[0].Foo = Bar
                ItemList[1].Foo = Baz
            ";
        var actual = new ConfFrom_CanInstantiateItemsWithPrivateConstructor_Object().ConfFrom(text, key: "").ItemList.Select(item => item.Foo).ToList();
        var expected = new[] { "Bar", "Baz" };
        Assert.That(actual, Is.EqualTo(expected));
    }
}
