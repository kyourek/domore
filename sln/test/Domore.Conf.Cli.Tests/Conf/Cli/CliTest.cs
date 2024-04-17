using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Conf.Cli {
    using Converters;

    [TestFixture]
    public partial class CliTest {
        private class Move {
            [CliRequired]
            [CliArgument]
            public MoveDirection Direction { get; set; }

            [CliArgument(order: 1)]
            public double Speed { get; set; }

            [CliDisplay(false)]
            public object DoNotDisplay { get; set; }
        }

        private enum MoveDirection {
            Up, Down, Left, Right
        }

        [Test]
        public void Configure_SetsCliArgument() {
            var move = Cli.Configure(new Move(), "left");
            Assert.That(move.Direction, Is.EqualTo(MoveDirection.Left));
        }

        [Test]
        public void Configure_SetsCliArgument1() {
            var move = Cli.Configure(new Move(), "RIGHT 55.5");
            Assert.That(move.Direction, Is.EqualTo(MoveDirection.Right));
            Assert.That(move.Speed, Is.EqualTo(55.5));
        }

        [Test]
        public void Configure_ThrowsExceptionIfRequiredPropertyNotFound() {
            var ex = Assert.Throws<CliRequiredNotFoundException>(() => Cli.Configure(new Move(), ""));
            Assert.That(ex.NotFound
                .Select(notFound => notFound.PropertyInfo)
                .Contains(typeof(Move).GetProperty(nameof(Move.Direction))));
        }

        [Test]
        public void Configure_ThrowsExceptionIfTooManyArgumentsGiven() {
            var ex = Assert.Throws<CliArgumentNotFoundException>(() => Cli.Configure(new Move(), "RIGHT 55.5 extra"));
            Assert.That(ex.Argument == "extra");
        }

        [Test]
        public void Configure_ThrowsExceptionIfValueCannotBeConverted() {
            var ex = Assert.Throws<CliConversionException>(() => Cli.Configure(new Move(), "backwards"));
            Assert.That(ex.InnerException, Is.InstanceOf(typeof(ConfValueConverterException)));
            var ie = (ConfValueConverterException)ex.InnerException;
            Assert.That(ie.Value, Is.EqualTo("backwards"));
            Assert.That(ie.State.Property == typeof(Move).GetProperty(nameof(Move.Direction)));
        }

        [Test]
        public void Display_DescribesCommand() {
            var actual = Cli.Display(new Move());
            var expected = "move direction<up/down/left/right> [speed<num>]";
            Assert.That(actual, Is.EqualTo(expected));
        }

        private class Bike {
            [CliRequired]
            public MoveDirection Move { get; set; }
            public double Speed { get; set; }
        }

        [TestCase("move=down speed=32.1")]
        [TestCase("bike move=down speed=32.1")]
        [TestCase("BIKE Move=doWN SPEED=32.1")]
        public void Configure_SetsCliProperties(string cli) {
            var bike = Cli.Configure(new Bike(), cli);
            Assert.That(bike.Move, Is.EqualTo(MoveDirection.Down));
            Assert.That(bike.Speed, Is.EqualTo(32.1));
        }

        [Test]
        public void Display_ShowsPropertyNames() {
            var actual = Cli.Display(new Bike());
            var expected = "bike move=<up/down/left/right> [speed=<num>]";
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Display_ShowsPropertyNamesWhenCommandNameIsSkipped() {
            var actual = Cli.Display(new Bike(), CliDisplayOptions.SkipCommandName);
            var expected = "move=<up/down/left/right> [speed=<num>]";
            Assert.That(actual, Is.EqualTo(expected));
        }

        private class Blend {
            [CliArgument]
            [CliRequired]
            public List<string> Fruits { get; set; }
            public List<NutKind> Nuts { get; set; }
        }

        private enum NutKind {
            Peanuts, Almonds, Cashews
        }

        [Test]
        public void Configure_SetsListItems() {
            var blend = Cli.Configure(new Blend(), "apples,bananas");
            CollectionAssert.AreEqual(new[] { "apples", "bananas" }, blend.Fruits);
        }

        [Test]
        public void Configure_SetsListItemsOfType() {
            var blend = Cli.Configure(new Blend(), "apples,bananas nuts=cashews,almonds");
            CollectionAssert.AreEqual(new[] { NutKind.Cashews, NutKind.Almonds }, blend.Nuts);
        }

        [Test]
        public void Display_ShowsList() {
            var actual = Cli.Display(new Blend());
            var expected = "blend fruits<,> [nuts=<,<peanuts/almonds/cashews>>]";
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Display_ShowsListWhenCommandNameIsSkipped() {
            var actual = Cli.Display(new Blend(), CliDisplayOptions.SkipCommandName);
            var expected = "fruits<,> [nuts=<,<peanuts/almonds/cashews>>]";
            Assert.That(actual, Is.EqualTo(expected));
        }

        private class Copy {
            [CliArgument]
            [CliRequired]
            public NextOrPrevious Where { get; set; }
        }

        private enum NextOrPrevious {
            [Conf("n")]
            [CliDisplayOverride("(n)ext")]
            Next,

            [Conf("p", "prev")]
            [CliDisplayOverride("(p)revious")]
            Previous
        }

        [TestCase("n", NextOrPrevious.Next)]
        [TestCase("NEXT", NextOrPrevious.Next)]
        [TestCase("p", NextOrPrevious.Previous)]
        [TestCase("Prev", NextOrPrevious.Previous)]
        [TestCase("previous", NextOrPrevious.Previous)]
        public void Configure_SetsEnumWithAlias(string alias, object expected) {
            var copy = Cli.Configure(new Copy(), alias);
            Assert.That(copy.Where, Is.EqualTo(expected));
        }

        [Test]
        public void Display_DisplaysOverrideOnEnumNames() {
            var actual = Cli.Display(new Copy());
            var expected = "copy where<(n)ext/(p)revious>";
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Display_DisplaysOverrideOnEnumNamesWhenCommandNameIsSkipped() {
            var actual = Cli.Display(new Copy(), CliDisplayOptions.SkipCommandName);
            var expected = "where<(n)ext/(p)revious>";
            Assert.That(actual, Is.EqualTo(expected));
        }

        private class ClassWithNullableEnum {
            [CliArgument]
            public NextOrPrevious? Prop { get; set; }
        }

        [Test]
        public void Configure_SetsNullableEnumWithAlias() {
            var obj = Cli.Configure(new ClassWithNullableEnum(), "p");
            Assert.That(obj.Prop, Is.EqualTo(NextOrPrevious.Previous));
        }

        private class Member {
            [CliArgument]
            public string FullName { get; set; }
            public string Address { get; set; }
        }

        [TestCase("'George Washington Carver'", "George Washington Carver")]
        [TestCase("'George \"Washington\" Carver'", "George \"Washington\" Carver")]
        [TestCase("\"George Washington Carver\"", "George Washington Carver")]
        [TestCase("\"George 'Washington' Carver\"", "George 'Washington' Carver")]
        public void Configure_RespectsQuotesInArgument(string text, string fullName) {
            var member = Cli.Configure(new Member(), text);
            Assert.That(member.FullName, Is.EqualTo(fullName));
        }

        [TestCase("address='down the road'", "down the road")]
        [TestCase("address='\"down the road\"'", "\"down the road\"")]
        [TestCase("address=\"down the road\"", "down the road")]
        [TestCase("address=\"'down the road'\"", "'down the road'")]
        public void Configure_RespectsQuotesInSwitch(string text, string address) {
            var member = Cli.Configure(new Member(), text);
            Assert.That(member.Address, Is.EqualTo(address));
        }

        [Test]
        public void Configure_ArgumentCanFollowQuotedSwitch() {
            var member = Cli.Configure(new Member(), "address='down the road' \"John Doe\"");
            Assert.That(member.FullName, Is.EqualTo("John Doe"));
            Assert.That(member.Address, Is.EqualTo("down the road"));
        }

        private enum ValueWord {
            None = 0,
            [CliDisplay(include: false)]
            Zero = 0,
            One = 1,
            [CliDisplay(include: false)]
            Single = 1
        }

        private class ValueWordClass {
            public ValueWord Word { get; set; }
        }

        [Test]
        public void Display_DoesNotIncludeUnincludedEnumMembers() {
            var actual = Cli.Display(new ValueWordClass());
            var expected = "valuewordclass [word=<none/one>]";
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Display_DoesNotIncludeUnincludedEnumMembersWhenCommandNameIsSkipped() {
            var actual = Cli.Display(new ValueWordClass(), CliDisplayOptions.SkipCommandName);
            var expected = "[word=<none/one>]";
            Assert.That(actual, Is.EqualTo(expected));
        }

        private enum ValueWord2 {
            None = 0,
            [CliDisplay(include: true)]
            Zero = 0,
            One = 1,
            [CliDisplay(include: true)]
            Single = 1
        }

        private class ValueWordClass2 {
            public ValueWord2 Word { get; set; }
        }

        [Test]
        public void Display_DoesNotIncludeEnumMembersByDefault() {
            var actual = Cli.Display(new ValueWordClass2());
            var expected = "valuewordclass2 [word=<zero/single>]";
            Assert.That(actual, Is.EqualTo(expected));
        }

        private enum ValueWord3 {
            None = 0,
            [CliDisplay(true)]
            [CliDisplayOverride("z")]
            Zero = 0,
            One = 1,
            [CliDisplay(true)]
            [CliDisplayOverride("s")]
            Single = 1
        }

        private class ValueWordClass3 {
            public ValueWord3 Word { get; set; }
        }

        [Test]
        public void Display_DoesNotIncludeEnumMembersByOverride() {
            var actual = Cli.Display(new ValueWordClass3());
            var expected = "valuewordclass3 [word=<z/s>]";
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Display_DoesNotIncludeEnumMembersByOverrideWhenCommandNameIsSkipped() {
            var actual = Cli.Display(new ValueWordClass3(), CliDisplayOptions.SkipCommandName);
            var expected = "[word=<z/s>]";
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Flags]
        private enum FlagsEnum {
            Flag1 = 1,
            Flag2 = 2,
            Flag4 = 4
        }

        private class FlagsEnumClass {
            public FlagsEnum Flags { get; set; }
        }

        [Test]
        public void Display_SeparatesEnumFlagsWithPipe() {
            var actual = Cli.Display(new FlagsEnumClass());
            var expected = "flagsenumclass [flags=<flag1|flag2|flag4>]";
            Assert.That(actual, Is.EqualTo(expected));
        }

        private class NullableFlagsEnumClass {
            public FlagsEnum? Flags { get; set; }
        }

        [Test]
        public void Display_UsesUnderlyingTypeOfNullable() {
            var actual = Cli.Display(new NullableFlagsEnumClass());
            var expected = "nullableflagsenumclass [flags=<flag1|flag2|flag4>]";
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Display_UsesUnderlyingTypeOfNullableWhenCommandNameIsSkipped() {
            var actual = Cli.Display(new NullableFlagsEnumClass(), CliDisplayOptions.SkipCommandName);
            var expected = "[flags=<flag1|flag2|flag4>]";
            Assert.That(actual, Is.EqualTo(expected));
        }

        private class ClassWithBoolAndStr {
            [CliArgument]
            public string SomeChars { get; set; }
            [CliRequired]
            public bool Option { get; set; }
        }

        [Test]
        public void Display_DisplaysBooleanSwitchAndOptionalArgument() {
            var actual = Cli.Display(new ClassWithBoolAndStr());
            var expected = "classwithboolandstr [<somechars>] option=<true/false>";
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Display_DisplaysBooleanSwitchAndOptionalArgumentWhenCommandNameIsSkipped() {
            var actual = Cli.Display(new ClassWithBoolAndStr(), CliDisplayOptions.SkipCommandName);
            var expected = "[<somechars>] option=<true/false>";
            Assert.That(actual, Is.EqualTo(expected));
        }

        private class ClassWithList {
            [CliRequired]
            [ConfListItems(Separator = ";")]
            public List<string> TheList { get; set; }
        }

        [Test]
        public void Display_DisplaysListSeparator() {
            var actual = Cli.Display(new ClassWithList());
            var expected = "classwithlist thelist=<;>";
            Assert.That(actual, Is.EqualTo(expected));
        }

        private class ClassWithReadonlyProperties {
            public List<string> ReadonlyList { get; } = null;
            public List<string> ReadWriteList { get; set; }
        }

        [Test]
        public void Display_DoesNotShowReadonlyProperties() {
            var actual = Cli.Display(new ClassWithReadonlyProperties());
            var expected = "classwithreadonlyproperties [readwritelist=<,>]";
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Display_DoesNotShowReadonlyPropertiesWhenCommandNameIsSkipped() {
            var actual = Cli.Display(new ClassWithReadonlyProperties(), CliDisplayOptions.SkipCommandName);
            var expected = "[readwritelist=<,>]";
            Assert.That(actual, Is.EqualTo(expected));
        }

        private class ClassWithArgumentsList {
            [CliArguments]
            public List<string> Arguments { get; set; }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Configure_AddsToArguments(bool @switch) {
            var target = @switch
                ? new ClassWithArgumentsList { Arguments = null }
                : new ClassWithArgumentsList { Arguments = new List<string>() };
            Cli.Configure(target, "Hello World! count 1 2 3 4");
            CollectionAssert.AreEqual(new[] { "Hello", "World!", "count", "1", "2", "3", "4" }, target.Arguments);
        }

        [Test]
        public void Configure_TrimsArguments() {
            var obj = Cli.Configure(new ClassWithArgumentsList(), " \t 'Hello  World!'  \t  count \" 1 '2\t3 ' \t4  \"  ");
            CollectionAssert.AreEqual(new[] { "Hello  World!", "count", "1 '2\t3 ' \t4" }, obj.Arguments);
        }

        [Test]
        public void Display_ShowsArgumentList() {
            Assert.That(Cli.Display(new ClassWithArgumentsList()), Is.EqualTo("classwithargumentslist [<arguments>]"));
        }

        private class TargetThatThrowsCliValidationException {
            public bool One { get; set; }
            public bool Two { get; set; }

            [CliValidation(2, "Message 2")]
            public bool ValidateTwo() {
                return Two;
            }

            [CliValidation(1, "Message 1")]
            public bool ValidateOne() {
                return One;
            }
        }

        [Test]
        public void Configure_ThrowsCliValidationException() {
            var target = new TargetThatThrowsCliValidationException();
            var error = Assert.Throws(typeof(CliValidationException), () => Cli.Configure(target, ""));
            var msg = error.Message;
            Assert.That(msg, Is.EqualTo("Message 1"));
        }

        [Test]
        public void Configure_ThrowsSecondCliValidationException() {
            var target = new TargetThatThrowsCliValidationException();
            var error = Assert.Throws(typeof(CliValidationException), () => Cli.Configure(target, "one=true"));
            var msg = error.Message;
            Assert.That(msg, Is.EqualTo("Message 2"));
        }

        private class TargetWithParameterSet {
            [CliParameters]
            public object Params {
                get => _Params ?? (_Params = new Dictionary<string, bool>());
                set => _Params = value;
            }
            private object _Params;
        }

        [Test]
        public void Configure_AddsToParameterSet() {
            var target = new TargetWithParameterSet();
            var set = (Dictionary<string, bool>)target.Params;
            Cli.Configure(target, "one=true two=false");
            CollectionAssert.AreEqual(new[] { true, false }, new[] { set["one"], set["two"] });
        }

        private class TargetWithParameterSet2 {
            public bool Flag { get; set; }

            [CliParameters]
            public Dictionary<string, string> Param {
                get => _Param ?? (_Param = new Dictionary<string, string>());
                set => _Param = value;
            }
            private Dictionary<string, string> _Param;
        }

        [Test]
        public void Configure_SetsPropertyWhileAddingToParameterSet() {
            var target = new TargetWithParameterSet2();
            Cli.Configure(target, "not-A-property='Hello World!' FLAG=TRUE");
            Assert.That(target.Param["not-A-property"], Is.EqualTo("Hello World!"));
            Assert.That(target.Param["FLAG"], Is.EqualTo("TRUE"));
            Assert.That(target.Flag, Is.True);
        }

        private class TargetWithNestedProperties {
            [CliDisplayOverride("[set.dict[<string>]=<string>]")]
            public TargetWithDict Set { get; set; } = new TargetWithDict();

            public class TargetWithDict {
                public Dictionary<string, string> Dict { get; set; } = new Dictionary<string, string>();
            }
        }

        [Test]
        public void Configure_SetsNestedProperties() {
            var target = new TargetWithNestedProperties();
            Cli.Configure(target, "set.dict[hello]=world");
            Assert.That(target.Set.Dict["hello"], Is.EqualTo("world"));
        }

        [Test]
        public void Display_UsesDisplayOverrideForProperty() {
            var display = Cli.Display(new TargetWithNestedProperties());
            Assert.That(display, Is.EqualTo("targetwithnestedproperties [set.dict[<string>]=<string>]"));
        }

        [Test]
        public void Display_UsesDisplayOverrideForPropertyWhenCommandNameIsSkipped() {
            var display = Cli.Display(new TargetWithNestedProperties(), CliDisplayOptions.SkipCommandName);
            Assert.That(display, Is.EqualTo("[set.dict[<string>]=<string>]"));
        }

        private class HeirarchyBase {
        }

        private class HeirarchyDerived : HeirarchyBase {
            [CliArgument]
            [CliRequired]
            public string TheArg { get; set; }
        }

        [Test]
        public void InstancesOfDerivedTypesAreConfigured() {
            HeirarchyBase target = new HeirarchyDerived();
            Cli.Configure(target, "the-value");
            Assert.That(((HeirarchyDerived)target).TheArg, Is.EqualTo("the-value"));
        }

        private class TypeWithArgumentAndSwitch {
            [CliArgument]
            public string TheArg { get; set; }

            public string TheSwitch { get; set; }
        }

        [Test]
        public void StringSwitchesCanBeSetToEmptyStrings() {
            var target = new TypeWithArgumentAndSwitch();
            Cli.Configure(target, "dothis theswitch=''");
            Assert.That(target.TheSwitch, Is.EqualTo(""));
        }

        [Test]
        public void EmptyStringSwitchesOverrideExistingValues() {
            var target = new TypeWithArgumentAndSwitch();
            target.TheSwitch = "some-value";
            Cli.Configure(target, "dothis theswitch=''");
            Assert.That(target.TheSwitch, Is.EqualTo(""));
        }
    }
}
