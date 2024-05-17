using Domore.Conf.Extensions;
using NUnit.Framework;
using System.Collections.Generic;

namespace Domore.Conf {
    [TestFixture]
    public sealed class ConfHelpAttributeTest {
        private class Thing {
            [ConfHelp("This is the name of the thing.")]
            public string Name { get; set; }
        }

        [Test]
        public void TextIsPlacedAboveProperty() {
            var actual = new Thing { Name = "foo" }.ConfText(key: "");
            var expected =
@"# This is the name of the thing.
Name = foo";
            Assert.That(actual, Is.EqualTo(expected));
        }

        private class Thing2 : Thing {
            [ConfHelp(
                "This value is very important.\r\n" +
                "Be very careful with it because\n" +
                "it may self destruct.")]
            public double Value { get; set; }
        }

        [Test]
        public void MultilineTextIsFormatted() {
            var actual = new Thing2 { Name = "Foo", Value = 1.23 }.ConfText(key: "");
            var expected =
@"# This is the name of the thing.
Name = Foo

# This value is very important.
# Be very careful with it because
# it may self destruct.
Value = 1.23";
            Assert.That(actual, Is.EqualTo(expected));
        }

        private class Thing3 : Thing {
            [ConfHelp(@"
                This value is very important.
                Be very careful with it because
                it may self destruct.")]
            public double Value { get; set; }
        }

        [Test]
        public void MultilineTextIsTrimmed() {
            var actual = new Thing3 { Name = "Foo", Value = 1.23 }.ConfText(key: "");
            var expected =
@"# This is the name of the thing.
Name = Foo

# This value is very important.
# Be very careful with it because
# it may self destruct.
Value = 1.23";
            Assert.That(actual, Is.EqualTo(expected));
        }

        private class Thing4 : Thing {
            [ConfHelp(@"
                This value is very important.
                Be very careful with it because
                it may self destruct.
            ")]
            public double Value { get; set; }
        }

        [Test]
        public void MultilineTextIsTrimmedFromEnd() {
            var actual = new Thing4 { Name = "Foo", Value = 1.23 }.ConfText(key: "");
            var expected =
@"# This is the name of the thing.
Name = Foo

# This value is very important.
# Be very careful with it because
# it may self destruct.
Value = 1.23";
            Assert.That(actual, Is.EqualTo(expected));
        }

        private class HasAThing {
            [ConfHelp(@"
                The ID is the identifier.")]
            public long ID { get; set; }

            public Thing4 Thing { get; set; }
        }

        [Test]
        public void PropertyValueHelpIsExpanded() {
            var actual = new HasAThing { ID = 4321, Thing = new() { Name = "Bar", Value = 2.34 } }.ConfText(key: "");
            var expected =
@"# The ID is the identifier.
ID = 4321

# This is the name of the thing.
Thing.Name = Bar

# This value is very important.
# Be very careful with it because
# it may self destruct.
Thing.Value = 2.34";
            Assert.That(actual, Is.EqualTo(expected));
        }

        private class HasAThing2 {
            [ConfHelp(@"
                The ID is the identifier.")]
            public long ID { get; set; }

            [ConfHelp(@"
                This thing
                doesn't matter
                much.
            ")]
            public Thing4 Thing { get; set; }
        }

        [Test]
        public void TextOnAPropertyOfAnAttributedClassIsIncluded() {
            var actual = new HasAThing2 { ID = 4321, Thing = new() { Name = "Bar", Value = 2.34 } }.ConfText(key: "");
            var expected =
@"# The ID is the identifier.
ID = 4321

# This thing
# doesn't matter
# much.

# This is the name of the thing.
Thing.Name = Bar

# This value is very important.
# Be very careful with it because
# it may self destruct.
Thing.Value = 2.34";
            Assert.That(actual, Is.EqualTo(expected));
        }

        private class HasAListOfThings : HasAThing {
            [ConfHelp(@"
                This is a list

                OF THINGS!!")]
            public List<Thing4> List { get; set; }
        }

        [Test]
        public void TextIsAppliedToAList() {
            var actual = new HasAListOfThings {
                ID = 5432,
                Thing = new() { Name = "FooBar", Value = 23.45 },
                List = new() {
                    new() { Name = "thing1", Value = 1 },
                    new() { Name = "Thing 2", Value = 2 }
                }
            }.ConfText(key: "");
            var expected =
@"# The ID is the identifier.
ID = 5432

# This is a list
# 
# OF THINGS!!
List[0].Name = thing1
List[0].Value = 1
List[1].Name = Thing 2
List[1].Value = 2

# This is the name of the thing.
Thing.Name = FooBar

# This value is very important.
# Be very careful with it because
# it may self destruct.
Thing.Value = 23.45";
            Assert.That(actual, Is.EqualTo(expected));
        }

        private class HasAListOfThings2 : HasAThing2 {
            [ConfHelp(@"
                This is a list

                OF THINGS!!  
  

            ")]
            public List<Thing4> List { get; set; }
        }

        [Test]
        public void TextIsAppliedToAList2() {
            var actual = new HasAListOfThings2 {
                ID = 5432,
                Thing = new() { Name = "FooBar", Value = 23.45 },
                List = new() {
                    new() { Name = "thing1", Value = 1 },
                    new() { Name = "Thing 2", Value = 2 }
                }
            }.ConfText(key: "");
            var expected =
@"# The ID is the identifier.
ID = 5432

# This is a list
# 
# OF THINGS!!
List[0].Name = thing1
List[0].Value = 1
List[1].Name = Thing 2
List[1].Value = 2

# This thing
# doesn't matter
# much.

# This is the name of the thing.
Thing.Name = FooBar

# This value is very important.
# Be very careful with it because
# it may self destruct.
Thing.Value = 23.45";
            Assert.That(actual, Is.EqualTo(expected));
        }

        private class IndentedHelp {
            [ConfHelp(@"
                Here you have a few options
                  1. Don't do anything
                  2. Do something
                    a. (This is the best option)
                  3. Wait and Evaluate
            ")]
            public string TheStr { get; set; }
            public double TheVal { get; set; }
        }

        [Test]
        public void IndentedTextIsPreserved() {
            var actual = new IndentedHelp { TheStr = "OK" }.ConfText();
            var expected =
@"# Here you have a few options
#   1. Don't do anything
#   2. Do something
#     a. (This is the best option)
#   3. Wait and Evaluate
IndentedHelp.TheStr = OK
IndentedHelp.TheVal = 0";
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void TextIsNotIncludedOnSingleLineConf() {
            var actual = new IndentedHelp { TheStr = "OK" }.ConfText(multiline: false);
            var expected = "IndentedHelp.TheStr=OK;IndentedHelp.TheVal=0";
            Assert.That(actual, Is.EqualTo(expected));
        }

        private class BlankHelp : IndentedHelp {
            [ConfHelp("")]
            public string TheXml { get; set; }
        }

        [Test]
        public void BlankHelpIsHandledCorrectly() {
            var actual = new BlankHelp { TheStr = "OK", TheXml = "<ok/>" }.ConfText();
            var expected =
@"# Here you have a few options
#   1. Don't do anything
#   2. Do something
#     a. (This is the best option)
#   3. Wait and Evaluate
BlankHelp.TheStr = OK
BlankHelp.TheVal = 0

# 
BlankHelp.TheXml = <ok/>";
            Assert.That(actual, Is.EqualTo(expected));
        }

        private class BlankHelp2 : IndentedHelp {
            [ConfHelp("  \t \t")]
            public string TheXml { get; set; }
        }

        [Test]
        public void BlankHelpIsHandledCorrectly2() {
            var actual = new BlankHelp2 { TheStr = "OK", TheXml = "<ok/>" }.ConfText();
            var expected =
@"# Here you have a few options
#   1. Don't do anything
#   2. Do something
#     a. (This is the best option)
#   3. Wait and Evaluate
BlankHelp2.TheStr = OK
BlankHelp2.TheVal = 0

# 
BlankHelp2.TheXml = <ok/>";
            Assert.That(actual, Is.EqualTo(expected));
        }

        private class NullHelp : IndentedHelp {
            [ConfHelp(null)]
            public string TheXml { get; set; }
        }

        [Test]
        public void NullHelpIsIgnored() {
            var actual = new NullHelp { TheStr = "OK", TheXml = "<ok/>" }.ConfText();
            var expected =
@"# Here you have a few options
#   1. Don't do anything
#   2. Do something
#     a. (This is the best option)
#   3. Wait and Evaluate
NullHelp.TheStr = OK
NullHelp.TheVal = 0
NullHelp.TheXml = <ok/>";
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
