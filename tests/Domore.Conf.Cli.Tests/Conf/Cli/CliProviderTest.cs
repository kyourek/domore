using Domore.Conf.Converters;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Conf.Cli;

[TestFixture]
public partial class CliProviderTest {
    private CliSetup Setup;

    private CliProvider Subject {
        get => _Subject ??= new(Setup);
        set => _Subject = value;
    }
    private CliProvider _Subject;

    [SetUp]
    public void SetUp() {
        Setup = new();
        Subject = null;
    }

    private class Move {
        [CliArgument, CliRequired, ConfHelp(@"
                The direction of the move.")]
        public MoveDirection Direction { get; set; }

        [CliArgument(order: 1)]
        [ConfHelp(@"
                The speed of the move.
                May be fast or slow.")]
        public double Speed { get; set; }

        [CliDisplay(false)]
        public object DoNotDisplay { get; set; }
    }

    private enum MoveDirection {
        Up, Down, Left, Right
    }

    [Test]
    public void Configure_SetsCliArgument() {
        var move = Subject.Configure(new Move(), "left");
        Assert.That(move.Direction, Is.EqualTo(MoveDirection.Left));
    }

    [Test]
    public void Configure_SetsCliArgument1() {
        var move = Subject.Configure(new Move(), "RIGHT 55.5");
        Assert.That(move.Direction, Is.EqualTo(MoveDirection.Right));
        Assert.That(move.Speed, Is.EqualTo(55.5));
    }

    [Test]
    public void Configure_ThrowsExceptionIfRequiredPropertyNotFound() {
        using (Assert.EnterMultipleScope()) {
            var ex = Assert.Throws<CliRequiredNotFoundException>(() => Subject.Configure(new Move(), ""));
            Assert.That(
                ex.NotFound.Select(notFound => notFound.PropertyInfo),
                Does.Contain(typeof(Move).GetProperty(nameof(Move.Direction))));
        }
    }

    [Test]
    public void Configure_ThrowsExceptionIfRequiredPropertyNotFoundWithCorrectMessage() {
        Assert.That(
            () => Subject.Configure(new Move(), ""),
            Throws
                .InstanceOf<CliRequiredNotFoundException>()
                .With
                .Property(nameof(CliRequiredNotFoundException.Message))
                .EqualTo("Missing required: direction"));
    }

    [Test]
    public void Configure_ThrowsExceptionIfTooManyArgumentsGiven() {
        Assert.That(
            () => Subject.Configure(new Move(), "RIGHT 55.5 extra"),
            Throws
                .InstanceOf<CliArgumentNotFoundException>()
                .With
                .Property(nameof(CliArgumentNotFoundException.Argument))
                .EqualTo("extra"));
    }

    [Test]
    public void Configure_ThrowsExceptionIfTooManyArgumentsGivenWithCorrectMessage() {
        Assert.That(
            () => Subject.Configure(new Move(), "RIGHT 55.5 extra"),
            Throws
                .InstanceOf<CliArgumentNotFoundException>()
                .With
                .Property(nameof(CliArgumentNotFoundException.Message))
                .EqualTo("Unexpected argument: extra"));
    }

    [Test]
    public void Configure_ThrowsExceptionIfValueCannotBeConverted() {
        using (Assert.EnterMultipleScope()) {
            var ex = Assert.Throws<CliConversionException>(() => Subject.Configure(new Move(), "backwards"));
            Assert.That(ex.InnerException, Is.InstanceOf(typeof(ConfValueConverterException)));
            var ie = (ConfValueConverterException)ex.InnerException;
            Assert.That(ie.Value, Is.EqualTo("backwards"));
            Assert.That(ie.State.Property == typeof(Move).GetProperty(nameof(Move.Direction)));
        }
    }

    [Test]
    public void Display_DescribesCommand() {
        var actual = Subject.Display(new Move());
        var expected = "move direction<up/down/left/right> [speed<num>]";
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Display_UsesSetCommandName() {
        Setup = Setup.WithCommandName(t => t == typeof(Move) ? "mv" : null);
        var actual = Subject.Display(new Move());
        var expected = "mv direction<up/down/left/right> [speed<num>]";
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Manual_ReturnsManual() {
        var actual = Subject.Manual(new Move());
        var expected = @"
move direction<up/down/left/right> [speed<num>]

    direction    The direction of the move.

    speed        The speed of the move.
                 May be fast or slow.".Trim();
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Manual_UsesSetCommandName() {
        Setup = Setup.WithCommandName(t => t == typeof(Move) ? "mv" : null);
        var actual = Subject.Manual(new Move());
        var expected = @"
mv direction<up/down/left/right> [speed<num>]

    direction    The direction of the move.

    speed        The speed of the move.
                 May be fast or slow.".Trim();
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
        var bike = Subject.Configure(new Bike(), cli);
        Assert.That(bike.Move, Is.EqualTo(MoveDirection.Down));
        Assert.That(bike.Speed, Is.EqualTo(32.1));
    }

    [Test]
    public void Display_ShowsPropertyNames() {
        var actual = Subject.Display(new Bike());
        var expected = "bike move=<up/down/left/right> [speed=<num>]";
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Display_ShowsPropertyNamesWithCustomCommandName() {
        Setup = Setup.WithCommandName(_ => "go");
        var actual = Subject.Display(new Bike());
        var expected = "go move=<up/down/left/right> [speed=<num>]";
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
        var blend = Subject.Configure(new Blend(), "apples,bananas");
        Assert.That(blend.Fruits, Is.EqualTo(["apples", "bananas"]));
    }

    [Test]
    public void Configure_SetsListItemsOfType() {
        var blend = Subject.Configure(new Blend(), "apples,bananas nuts=cashews,almonds");
        Assert.That(blend.Nuts, Is.EqualTo([NutKind.Cashews, NutKind.Almonds]));
    }

    [Test]
    public void Display_ShowsList() {
        var actual = Subject.Display(new Blend());
        var expected = "blend fruits<,> [nuts=<,<peanuts/almonds/cashews>>]";
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Display_ShowsListWithCustomCommandName() {
        Setup = Setup.WithCommandName(_ => "blnd");
        var actual = Subject.Display(new Blend());
        var expected = "blnd fruits<,> [nuts=<,<peanuts/almonds/cashews>>]";
        Assert.That(actual, Is.EqualTo(expected));
    }

    private class Copy {
        [CliArgument]
        [CliRequired]
        [ConfHelp(@"
                Sets next or previous.")]
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
        var copy = Subject.Configure(new Copy(), alias);
        Assert.That(copy.Where, Is.EqualTo(expected));
    }

    [Test]
    public void Display_DisplaysOverrideOnEnumNames() {
        var actual = Subject.Display(new Copy());
        var expected = "copy where<(n)ext/(p)revious>";
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Display_DisplaysOverrideOnEnumNamesWithCustomCommandName() {
        Setup = Setup.WithCommandName(t => t == typeof(Copy) ? "cp" : null);
        var actual = Subject.Display(new Copy());
        var expected = "cp where<(n)ext/(p)revious>";
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Manual_DisplaysOverrideOnEnumNames() {
        var actual = Subject.Manual(new Copy());
        var expected = @"
copy where<(n)ext/(p)revious>

    where    Sets next or previous.".Trim();
        Assert.That(actual, Is.EqualTo(expected));
    }

    private class ClassWithNullableEnum {
        [CliArgument]
        public NextOrPrevious? Prop { get; set; }
    }

    [Test]
    public void Configure_SetsNullableEnumWithAlias() {
        var obj = Subject.Configure(new ClassWithNullableEnum(), "p");
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
        var member = Subject.Configure(new Member(), text);
        Assert.That(member.FullName, Is.EqualTo(fullName));
    }

    [TestCase("address='down the road'", "down the road")]
    [TestCase("address='\"down the road\"'", "\"down the road\"")]
    [TestCase("address=\"down the road\"", "down the road")]
    [TestCase("address=\"'down the road'\"", "'down the road'")]
    public void Configure_RespectsQuotesInSwitch(string text, string address) {
        var member = Subject.Configure(new Member(), text);
        Assert.That(member.Address, Is.EqualTo(address));
    }

    [Test]
    public void Configure_ArgumentCanFollowQuotedSwitch() {
        var member = Subject.Configure(new Member(), "address='down the road' \"John Doe\"");
        Assert.That(member.FullName, Is.EqualTo("John Doe"));
        Assert.That(member.Address, Is.EqualTo("down the road"));
    }

    [Test]
    public void Display_DisplaysMultipleCommands() {
        var commands = new object[] { new ValueWordClass(), new ValueWordClass2(), new ValueWordClass3() };
        var actual = Subject.Display(commands);
        var expected = @"
valuewordclass [word=<none/one>]
valuewordclass2 [word=<zero/single>]
valuewordclass3 [word=<z/s>]".Trim();
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Display_DisplaysMultipleCommandsWithCustomNames() {
        Setup = Setup.WithCommandName(t =>
            t == typeof(ValueWordClass) ? "foo" :
            t == typeof(ValueWordClass2) ? "bar" :
            t == typeof(ValueWordClass3) ? "baz" :
            null);
        var commands = new object[] { new ValueWordClass(), new ValueWordClass2(), new ValueWordClass3() };
        var actual = Subject.Display(commands);
        var expected = @"
foo [word=<none/one>]
bar [word=<zero/single>]
baz [word=<z/s>]".Trim();
        Assert.That(actual, Is.EqualTo(expected));
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
        var actual = Subject.Display(new ValueWordClass());
        var expected = "valuewordclass [word=<none/one>]";
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Display_DoesNotIncludeUnincludedEnumMembersWithCustomCommandName() {
        Setup = Setup.WithCommandName(_ => "foo");
        var actual = Subject.Display(new ValueWordClass());
        var expected = "foo [word=<none/one>]";
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
        var actual = Subject.Display(new ValueWordClass2());
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
        var actual = Subject.Display(new ValueWordClass3());
        var expected = "valuewordclass3 [word=<z/s>]";
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Display_DoesNotIncludeEnumMembersByOverrideWithCustomCommandName() {
        Setup = Setup.WithCommandName(t => t == typeof(ValueWordClass3) ? "vwc" : null);
        var actual = Subject.Display(new ValueWordClass3());
        var expected = "vwc [word=<z/s>]";
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
        var actual = Subject.Display(new FlagsEnumClass());
        var expected = "flagsenumclass [flags=<flag1|flag2|flag4>]";
        Assert.That(actual, Is.EqualTo(expected));
    }

    private class NullableFlagsEnumClass {
        public FlagsEnum? Flags { get; set; }
    }

    [Test]
    public void Display_UsesUnderlyingTypeOfNullable() {
        var actual = Subject.Display(new NullableFlagsEnumClass());
        var expected = "nullableflagsenumclass [flags=<flag1|flag2|flag4>]";
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Display_UsesUnderlyingTypeOfNullableWithCustomCommandName() {
        Setup = Setup.WithCommandName(_ => "bar");
        var actual = Subject.Display(new NullableFlagsEnumClass());
        var expected = "bar [flags=<flag1|flag2|flag4>]";
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
        var actual = Subject.Display(new ClassWithBoolAndStr());
        var expected = "classwithboolandstr [<somechars>] option=<true/false>";
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Display_DisplaysBooleanSwitchAndOptionalArgumentWithCustomCommandName() {
        Setup = Setup.WithCommandName(t => t == typeof(ClassWithBoolAndStr) ? "cwbas" : null);
        var actual = Subject.Display(new ClassWithBoolAndStr());
        var expected = "cwbas [<somechars>] option=<true/false>";
        Assert.That(actual, Is.EqualTo(expected));
    }

    private class ClassWithList {
        [CliRequired]
        [ConfListItems(Separator = ";")]
        public List<string> TheList { get; set; }
    }

    [Test]
    public void Display_DisplaysListSeparator() {
        var actual = Subject.Display(new ClassWithList());
        var expected = "classwithlist thelist=<;>";
        Assert.That(actual, Is.EqualTo(expected));
    }

    private class ClassWithReadonlyProperties {
        public List<string> ReadonlyList { get; } = null;
        public List<string> ReadWriteList { get; set; }
    }

    [Test]
    public void Display_DoesNotShowReadonlyProperties() {
        var actual = Subject.Display(new ClassWithReadonlyProperties());
        var expected = "classwithreadonlyproperties [readwritelist=<,>]";
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Display_DoesNotShowReadonlyPropertiesWithCustomCommandName() {
        Setup = Setup.WithCommandName(_ => "cmd");
        var actual = Subject.Display(new ClassWithReadonlyProperties());
        var expected = "cmd [readwritelist=<,>]";
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
        Subject.Configure(target, "Hello World! count 1 2 3 4");
        Assert.That(target.Arguments, Is.EqualTo(["Hello", "World!", "count", "1", "2", "3", "4"]));
    }

    [Test]
    public void Configure_TrimsArguments() {
        var obj = Subject.Configure(new ClassWithArgumentsList(), " \t 'Hello  World!'  \t  count \" 1 '2\t3 ' \t4  \"  ");
        Assert.That(obj.Arguments, Is.EqualTo(["Hello  World!", "count", "1 '2\t3 ' \t4"]));
    }

    [Test]
    public void Display_ShowsArgumentList() {
        Assert.That(Subject.Display(new ClassWithArgumentsList()), Is.EqualTo("classwithargumentslist [<arguments>]"));
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
        var error = Assert.Throws(typeof(CliValidationException), () => Subject.Configure(target, ""));
        var msg = error.Message;
        Assert.That(msg, Is.EqualTo("Message 1"));
    }

    [Test]
    public void Configure_ThrowsSecondCliValidationException() {
        var target = new TargetThatThrowsCliValidationException();
        var error = Assert.Throws(typeof(CliValidationException), () => Subject.Configure(target, "one=true"));
        var msg = error.Message;
        Assert.That(msg, Is.EqualTo("Message 2"));
    }

    private class TargetWithParameterSet {
        [CliParameters, ConfHelp(@"
                These are all the params.")]
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
        Subject.Configure(target, "one=true two=false");
        Assert.That(new[] { set["one"], set["two"] }, Is.EqualTo([true, false]));
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
        Subject.Configure(target, "not-A-property='Hello World!' FLAG=TRUE");
        using (Assert.EnterMultipleScope()) {
            Assert.That(target.Param["not-A-property"], Is.EqualTo("Hello World!"));
            Assert.That(target.Param["FLAG"], Is.EqualTo("TRUE"));
            Assert.That(target.Flag, Is.True);
        }
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
        Subject.Configure(target, "set.dict[hello]=world");
        Assert.That(target.Set.Dict["hello"], Is.EqualTo("world"));
    }

    [Test]
    public void Display_UsesDisplayOverrideForProperty() {
        var display = Subject.Display(new TargetWithNestedProperties());
        Assert.That(display, Is.EqualTo("targetwithnestedproperties [set.dict[<string>]=<string>]"));
    }

    [Test]
    public void Display_UsesDisplayOverrideForPropertyWithCustomCommandName() {
        Setup = Setup.WithCommandName(_ => "target");
        var display = Subject.Display(new TargetWithNestedProperties());
        Assert.That(display, Is.EqualTo("target [set.dict[<string>]=<string>]"));
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
        Subject.Configure(target, "the-value");
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
        Subject.Configure(target, "dothis theswitch=''");
        Assert.That(target.TheSwitch, Is.EqualTo(""));
    }

    [Test]
    public void EmptyStringSwitchesOverrideExistingValues() {
        var target = new TypeWithArgumentAndSwitch();
        target.TheSwitch = "some-value";
        Subject.Configure(target, "dothis theswitch=''");
        Assert.That(target.TheSwitch, Is.EqualTo(""));
    }

    [CliExample("foo 1", "Do one to foo.")]
    private class LongManualBase {
        [CliArgument, CliRequired, ConfHelp(@"
            This is a string argument.
            It's really important.
            Don't screw it up.")]
        public string ArgS { get; set; }

        [CliArgument, ConfHelp(@"
            The number is optional.
            Include it or don't, I don't care.")]
        public int ArgI { get; set; }

        [CliDisplay(false)]
        public string DontShowThis { get; set; }
    }

    [ConfHelp(@"
            This command has a long manual.
            The length is important because that's what we're testing, here.")]
    [CliExample("bar names=foo,baz speed=1.2", @"
            Does alot.
            You won't need any other commands.")]
    private class LongManualDerived : LongManualBase {
        [ConfHelp(@"
                A list of names.
                These are separated by the default separator.
            ")]
        public List<string> Names { get; set; }

        [CliDisplay(false)]
        public string DontShowThisEither { get; set; }

        [ConfHelp(@"
                Fast or slow?
                Pick one.")]
        public double Speed { get; set; }
    }

    [Test]
    public void Manual_ShowsALongManual() {
        var actual = Subject.Manual(new LongManualDerived());
        var expected = @"
longmanualderived <args> [argi<int>] [names=<,>] [speed=<num>]

    This command has a long manual.
    The length is important because that's what we're testing, here.

    args     This is a string argument.
             It's really important.
             Don't screw it up.

    argi     The number is optional.
             Include it or don't, I don't care.

    names    A list of names.
             These are separated by the default separator.

    speed    Fast or slow?
             Pick one.

ex. longmanualderived bar names=foo,baz speed=1.2
    Does alot.
    You won't need any other commands.

ex. longmanualderived foo 1
    Do one to foo.
            ".Trim();
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Manual_ShowsALongManualWithCustomCommandSpaceAndName() {
        Setup = Setup
            .WithCommandName(t => t == typeof(LongManualDerived) ? "my-cmd" : null)
            .WithCommandSpace(_ => "do-it");
        var actual = Subject.Manual(new LongManualDerived());
        var expected = @"
my-cmd <args> [argi<int>] [names=<,>] [speed=<num>]

    This command has a long manual.
    The length is important because that's what we're testing, here.

    args     This is a string argument.
             It's really important.
             Don't screw it up.

    argi     The number is optional.
             Include it or don't, I don't care.

    names    A list of names.
             These are separated by the default separator.

    speed    Fast or slow?
             Pick one.

ex. do-it my-cmd bar names=foo,baz speed=1.2
    Does alot.
    You won't need any other commands.

ex. do-it my-cmd foo 1
    Do one to foo.
".Trim();
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Manual_ShowsMultipleManuals() {
        var actual = Subject.Manual(new object[] { new TargetWithParameterSet(), new TypeWithArgumentAndSwitch(), new LongManualDerived() });
        var expected = @"
targetwithparameterset [params=<object>]

    params    These are all the params.

typewithargumentandswitch [<thearg>] [theswitch=<str>]

longmanualderived <args> [argi<int>] [names=<,>] [speed=<num>]

    This command has a long manual.
    The length is important because that's what we're testing, here.

    args     This is a string argument.
             It's really important.
             Don't screw it up.

    argi     The number is optional.
             Include it or don't, I don't care.

    names    A list of names.
             These are separated by the default separator.

    speed    Fast or slow?
             Pick one.

ex. longmanualderived bar names=foo,baz speed=1.2
    Does alot.
    You won't need any other commands.

ex. longmanualderived foo 1
    Do one to foo.
            ".Trim();
        Assert.That(actual, Is.EqualTo(expected));
    }
}
