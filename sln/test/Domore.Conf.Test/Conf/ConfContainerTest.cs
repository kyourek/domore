using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Domore.Conf {
    [TestFixture]
    public sealed class ConfContainerTest {
        private object Content;

        private ConfContainer Subject {
            get => _Subject ?? (_Subject = new ConfContainer { Source = Content });
            set => _Subject = value;
        }
        private ConfContainer _Subject;

        [SetUp]
        public void SetUp() {
            Content = null;
            Subject = null;
        }

        private class Man {
            [ConfConverter(typeof(DogConfValueConverter))]
            public Dog BestFriend { get; set; }
        }

        private class Dog {
            public string Color { get; set; }
        }

        private class DogConfValueConverter : ConfValueConverter {
            public override object Convert(string value, ConfValueConverterState state) {
                return state.Conf.Configure(new Dog(), key: value);
            }
        }

        [Test]
        public void Configure_UsesConfTypeConverter() {
            Content = @"
                Penny.color = red
                Man.Best friend = Penny
            ";
            var man = Subject.Configure(new Man());
            Assert.AreEqual("red", man.BestFriend.Color);
        }

        [TestCase("penny.Color")]
        [TestCase("man.bestfriend")]
        [TestCase("Man.Best friend")]
        public void Lookup_ContainsKeys(string key) {
            Content = @"
                Penny.color = red
                Man.Best friend = Penny
            ";
            Assert.That(Subject.Lookup.Contains(key));
        }

        [TestCase("  penny . Color  \t", "red")]
        [TestCase("man.best FRIEND ", "Penny")]
        [TestCase("\tMa n.BestFriend", "Penny")]
        public void Lookup_ReturnsKeyValue(string key, string value) {
            Content = @"
                Penny.color = red
                Man.Best friend = Penny
            ";
            Assert.That(Subject.Lookup.Value(key), Is.EqualTo(value));
        }

        [Test]
        public void Lookup_ReturnsAllForKey() {
            Content = @"
                Penny.color = red
                Man.Best friend = Penny
                penny .  color  = brown


                PENNy    .COLOR   = Red and White   
            ";
            CollectionAssert.AreEqual(Subject.Lookup.All("penny.color"), new[] { "red", "brown", "Red and White" });
        }

        private class ManWithCat : Man {
            [Conf(ignore: false)]
            public Cat Cat { get; set; }
        }

        [Test]
        public void Configure_DoesNotIgnoreProperty() {
            Content = @"
                Penny.color = red
                Man.Best friend = Penny
                Man.Cat.Color = black
            ";
            var man = Subject.Configure(new ManWithCat(), "Man");
            Assert.AreEqual("black", man.Cat.Color);
        }

        private class ManWithIgnoredCat : Man {
            [Conf(ignore: true)]
            public Cat Cat { get; set; }
        }

        [Test]
        public void Configure_IgnoresMansCat() {
            Content = @"
                Penny.color = red
                Man.Best friend = Penny
                Man.Cat.Color = black
            ";
            var man = Subject.Configure(new ManWithIgnoredCat(), "Man");
            Assert.IsNull(man.Cat);
        }

        private class ManWithIgnoredCat2 : Man {
            [Conf(ignoreSet: true, ignoreGet: false)]
            public Cat Cat { get; set; }
        }

        [Test]
        public void Configure_IgnoresMansCat2() {
            Content = @"
                Penny.color = red
                Man.Best friend = Penny
                Man.Cat.Color = black
            ";
            var man = Subject.Configure(new ManWithIgnoredCat2(), "Man");
            Assert.IsNull(man.Cat);
        }

        private class ObjWithIgnoredProp {
            public string NotIgnored { get; set; }

            [Conf(ignore: true)]
            public string YesIgnored { get; set; }
        }

        [Test]
        public void Configure_IgnoresIgnoredProperty() {
            Content = @"
                Obj.NotIgnored = 1
                Obj.YesIgnored = 1
            ";
            var obj = Subject.Configure(new ObjWithIgnoredProp(), "Obj");
            Assert.IsNull(obj.YesIgnored);
        }

        private class Kid { public Pet Pet { get; set; } }
        private class Pet { }
        private class Cat : Pet { public string Color { get; set; } }

        [Test]
        public void Configure_CreatesInstanceOfType() {
            Content = @"Kid.Pet = Domore.Conf.ConfContainerTest+Cat, Domore.Conf.Test";
            var kid = Subject.Configure(new Kid());
            Assert.That(kid.Pet, Is.InstanceOf(typeof(Cat)));
        }

        private class Mom { public IList<string> Jobs { get; } = new Collection<string>(); }

        [Test]
        public void Configure_AddsToList() {
            Content = @"
                Mom.Jobs[0] = chef
                Mom.jobs[1] = Nurse
                mom.jobs[2] = accountant";
            var mom = Subject.Configure(new Mom());
            Assert.That(mom.Jobs, Is.EqualTo(new[] { "chef", "Nurse", "accountant" }));
        }

        private class NumContainer { public ICollection<double> Nums { get; } = new List<double>(); }

        [Test]
        public void Configure_AddsConvertedValuesToList() {
            Content = @"
                Cont.Nums[0] = 1.23
                Cont.nums[1] = 2.34
                Cont.nums[2] = 3.45";
            var cont = Subject.Configure(new NumContainer(), "cont");
            Assert.That(cont.Nums, Is.EqualTo(new[] { 1.23, 2.34, 3.45 }));
        }

        [Test]
        public void Configure_RespectsLastListedIndexOfList() {
            Content = @"
                Cont.Nums[0] = 1.23
                Cont.nums[1] = 2.34
                Cont.nums[1] = 2.00
                Cont.nums[2] = 3.45";
            var cont = Subject.Configure(new NumContainer(), "cont");
            Assert.That(cont.Nums, Is.EqualTo(new[] { 1.23, 2.00, 3.45 }));
        }

        [Test]
        public void Configure_SetsListItemsToDefault() {
            Content = @"
                Cont.Nums[0] = 1.23
                Cont.nums[1] = 2.34
                Cont.nums[2] = 3.45
                Cont.nums[5] = 5.67";
            var cont = Subject.Configure(new NumContainer(), "cont");
            Assert.That(cont.Nums, Is.EqualTo(new[] { 1.23, 2.34, 3.45, 0.0, 0.0, 5.67 }));
        }

        [Test]
        public void Configure_SetsListItemsToNull() {
            Content = @"
                Mom.Jobs[1] = chef
                Mom.jobs[3] = Nurse
                mom.jobs[7] = accountant";
            var mom = Subject.Configure(new Mom());
            Assert.That(mom.Jobs, Is.EqualTo(new[] { null, "chef", null, "Nurse", null, null, null, "accountant" }));
        }

        private class IntContainer { public IDictionary<string, int> Dict { get; } = new Dictionary<string, int>(); }

        [Test]
        public void Configure_SetsDictionaryValues() {
            Content = @"
                cont.Dict[first] = 1
                cont.dict[Third] = 3";
            var cont = Subject.Configure(new IntContainer(), "cont");
            Assert.That(cont.Dict, Is.EqualTo(new Dictionary<string, int> { { "first", 1 }, { "Third", 3 } }));
        }

        private class Infant {
            public string Weight { get; set; }
            public int DiaperSize { get; set; }
            public Mom Mom { get; } = new Mom();
        }

        [Test]
        public void Configure_CanSetValuesWithoutKey() {
            Content = @"
                Weight = 12.3 lb
                Diaper size = 1
                Mom.Jobs[0] = chef
                Mom.jobs[1] = Nurse
                mom.jobs[2] = accountant            ";
            var infant = Subject.Configure(new Infant(), "");
            Assert.That(infant.Weight, Is.EqualTo("12.3 lb"));
        }

        [Test]
        public void Configure_SetsSecondValueWithoutKey() {
            Content = @"
                Weight = 12.3 lb
                Diaper size = 1
                Mom.Jobs[0] = chef
                Mom.jobs[1] = Nurse
                mom.jobs[2] = accountant
            ";
            var infant = Subject.Configure(new Infant(), "");
            Assert.That(infant.DiaperSize, Is.EqualTo(1));
        }

        [Test]
        public void Configure_SetsComplexTypeValuesWithoutKey() {
            Content = @"
                Weight = 12.3 lb
                Diaper size = 1
                Mom.Jobs[0] = chef
                Mom.jobs[1] = Nurse
                mom.jobs[2] = accountant
            ";
            var infant = Subject.Configure(new Infant(), "");
            Assert.That(infant.Mom.Jobs[1], Is.EqualTo("Nurse"));
        }

        [Test]
        public void Configure_SetsDeepValues() {
            Content = @"
                kid.Weight = 12.3 lb
                kid.Diaper size = 1
                kid.Mom.Jobs[0] = chef
                kid.Mom.jobs[1] = Nurse
                kid.mom.jobs[2] = accountant
            ";
            var infant = Subject.Configure(new Infant(), "Kid");
            Assert.That(infant.Mom.Jobs[2], Is.EqualTo("accountant"));
        }

        [Test]
        public void Configure_ReturnsCollection() {
            Content = @"
                kid[0].weight = 3
                kid[0].diapersize = 1
                kid[1].weight = 15
                kid[1].diapersize = 2
                kid[2].weight = 26
                kid[2].diapersize = 4
            ";
            var kids = Subject.Configure(() => new Infant(), "Kid").ToList();
            Assert.That(kids.Count, Is.EqualTo(3));
        }

        [TestCase(0, "Weight", "3")]
        [TestCase(0, "DiaperSize", 1)]
        [TestCase(1, "Weight", "15")]
        [TestCase(1, "DiaperSize", 2)]
        [TestCase(2, "Weight", "26")]
        [TestCase(2, "DiaperSize", 4)]
        public void Configure_ConfiguresCollection(int index, string propertyName, object expected) {
            Content = @"
                kid[0].weight = 3
                kid[0].diapersize = 1
                kid[1].weight = 15
                kid[1].diapersize = 2
                kid[2].weight = 26
                kid[2].diapersize = 4
            ";
            var kids = Subject.Configure(() => new Infant(), "Kid").ToList();
            Assert.That(typeof(Infant).GetProperty(propertyName).GetValue(kids[index], null), Is.EqualTo(expected));
        }

        [TestCase(0, "Weight", "3")]
        [TestCase(0, "DiaperSize", 1)]
        [TestCase(1, "Weight", "15")]
        [TestCase(1, "DiaperSize", 0)]
        [TestCase(2, "Weight", "26")]
        [TestCase(2, "DiaperSize", 4)]
        public void Configure_IgnoresConfigurationOfDifferentCollection(int index, string propertyName, object expected) {
            Content = @"
                kid[0].weight = 3
                kid[0].diapersize = 1
                kid[1].weight = 15
                notakid[1].diapersize = 2
                kid[2].weight = 26
                kid[2].diapersize = 4
            ";
            var kids = Subject.Configure(() => new Infant(), "Kid").ToList();
            Assert.That(typeof(Infant).GetProperty(propertyName).GetValue(kids[index], null), Is.EqualTo(expected));
        }

        [Test]
        public void Configure_ConfiguresCollectionDeeply() {
            Content = @"
                kid[0].weight = 3
                kid[0].diapersize = 1
                kid[1].weight = 15
                kid[1].diapersize = 2
                kid[1].Mom.JOBS[0] = nurse0
                kid[1].Mom.JOBS[1] = Nurse1
                kid[1].Mom.JOBS[2] = nurse2
                kid[2].weight = 26
                kid[2].diapersize = 4
            ";
            var kids = Subject.Configure(() => new Infant(), "Kid").ToList();
            var kid = kids.Single(k => k.DiaperSize == 2);
            Assert.That(kid.Mom.Jobs[1], Is.EqualTo("Nurse1"));
        }

        private class KeyedInfant : Infant { public string Key { get; set; } }

        [TestCase("Num 0", "Weight", "3")]
        [TestCase("Num 0", "DiaperSize", 1)]
        [TestCase("num1", "Weight", "15")]
        [TestCase("num1", "DiaperSize", 2)]
        [TestCase("num 2", "Weight", "26")]
        [TestCase("num  2", "DiaperSize", 4)]
        public void Configure_ConfiguresPairs(string index, string propertyName, object expected) {
            Content = @"
                kid[Num 0].weight = 3
                kid[num 0].diapersize = 1
                kid[num1].weight = 15
                kid[NUM1].diapersize = 2
                kid[num 2].weight = 26
                kid[num  2].diapersize = 4
            ";
            var kids = Subject.Configure(k => new KeyedInfant { Key = k }, "Kid", StringComparer.OrdinalIgnoreCase).ToDictionary(pair => pair.Key, pair => pair.Value);
            Assert.That(typeof(KeyedInfant).GetProperty(propertyName).GetValue(kids[index], null), Is.EqualTo(expected));
        }

        [Test]
        public void Configure_DoesNotNormalizeKeys() {
            Content = @"
                kid[Num 0].weight = 3
                kid[num 0].diapersize = 1
                kid[num1].weight = 15
                kid[NUM1].diapersize = 2
                kid[num 2].weight = 26
                kid[num  2].diapersize = 4
            ";
            var kids = Subject.Configure(k => new KeyedInfant { Key = k }, "Kid").ToList();
            var expected = new List<string> { "Num 0", "num 0", "num1", "NUM1", "num 2", "num  2" };
            var actual = kids.Select(kid => kid.Key).ToList();
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Configure_PaysAttentionToCollection() {
            Content = @"
                kid  [Num 0].weight = 3
                k id[num 0].diapersize = 1
                wiz[num1].weight = 15
                kid[NUM1].diapersize = 2
                KID[num 2].weight = 26
                Kid[num  2].diapersize = 4
            ";
            var kids = Subject.Configure(k => new KeyedInfant { Key = k }, "Kid").ToList();
            var expected = new List<string> { "Num 0", "num 0", "NUM1", "num 2", "num  2" };
            var actual = kids.Select(kid => kid.Key).ToList();
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Configure_UsesSuppliedKeyComparer() {
            Content = @"
                kid[Num 0].weight = 3
                kid[num 0].diapersize = 1
                kid [num1].weight = 15
                kid[NUM1].diapersize = 2
                kid[num 2].weight = 26
                kid[num  2].diapersize = 4
            ";
            var kids = Subject.Configure(k => new KeyedInfant { Key = k }, "Kid", StringComparer.OrdinalIgnoreCase).ToList();
            var expected = new List<string> { "Num 0", "num1", "num 2", "num  2" };
            var actual = kids.Select(kid => kid.Key).ToList();
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Configure_ConfiguresSingleItem() {
            Content = @"
                kid.weight = 3
                kid.diapersize = 1
                kid.Mom.JOBS[0] = nurse0
                kid.Mom.JOBS[1] = Nurse1
                kid.Mom.JOBS[2] = nurse2
            ";
            var kids = Subject.Configure(() => new Infant(), "KID").ToList();
            var kid = kids.Single();
            Assert.That(kid.Mom.Jobs[2], Is.EqualTo("nurse2"));
        }

        [Test]
        public void Configure_ConfiguresSingleItemWithBracketedValues() {
            Content = @"
                kid.weight = 3
                kid.diapersize = 1
                kid.Mom.JOBS[0] = nurse0
                kid.Mom.JOBS[1] = {
                    Nurse and
                    chauffeur and
                    cook
                }
                kid.Mom.JOBS[2] = nurse2
            ";
            var kids = Subject.Configure(() => new Infant(), "KID").ToList();
            var kid = kids.Single();
            var actual = kid.Mom.Jobs[1];
            var expected =
@"                    Nurse and
                    chauffeur and
                    cook";
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Configure_DoesNotTrimBracketedValues() {
            Content = @"
                kid.weight = 3
                kid.diapersize = 1
                kid.Mom.JOBS[0] = nurse0
                kid.Mom.JOBS[1] = {
   
                    Nurse and            
                    chauffeur and    


                    cook                             

        
                }
                kid.Mom.JOBS[2] = nurse2
            ";
            var kids = Subject.Configure(() => new Infant(), "KID").ToList();
            var kid = kids.Single();
            var actual = kid.Mom.Jobs[1];
            var expected = @"   
                    Nurse and            
                    chauffeur and    


                    cook                             

        ";
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Configure_DoesNotSetValueWithEmptyBracketedText() {
            Content = @"
                kid.weight = 3
                kid.diapersize = 1
                kid.Mom.JOBS[0] = nurse0
                kid.Mom.JOBS[1] = {
   
        
                }
                kid.Mom.JOBS[2] = nurse2
            ";
            var kids = Subject.Configure(() => new Infant(), "KID").ToList();
            var kid = kids.Single();
            var actual = kid.Mom.Jobs[1];
            var expected = default(string);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Configure_UsesClassName() {
            Content = @"
                infant.weight = 3
                infant.diapersize = 1
                infant.Mom.JOBS[0] = nurse0
                infant.Mom.JOBS[1] = Nurse1
                infant.Mom.JOBS[2] = nurse2
            ";
            var kids = Subject.Configure(() => new Infant()).ToList();
            var kid = kids.Single();
            Assert.That(kid.Mom.Jobs[1], Is.EqualTo("Nurse1"));
        }

        [Test]
        public void Configure_CallsBackWithNullKey() {
            Content = @"
                kid[].diapersize = 1
                kid.weight = 15
                kid[ " + "\t" + @" ].weight = 26
                kid.diapersize = 4
            ";
            var kids = Subject.Configure(k => new KeyedInfant { Key = k }, "Kid").ToList();
            var kid = kids.Single(k => k.Key == null);
            Assert.That(kid.Value.Weight, Is.EqualTo("15"));
            Assert.That(kid.Value.DiaperSize, Is.EqualTo(4));
        }

        [Test]
        public void Configure_CallsBackWithEmptyString() {
            Content = @"
                kid[].diapersize = 1
                kid.weight = 15
                kid[ " + "\t" + @" ].weight = 26
                kid.diapersize = 4
            ";
            var kids = Subject.Configure(k => new KeyedInfant { Key = k }, "Kid").ToList();
            var kid = kids.Single(k => k.Key == "");
            Assert.That(kid.Value.Weight, Is.EqualTo("26"));
            Assert.That(kid.Value.DiaperSize, Is.EqualTo(1));
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
        public void Configure_AddsItemsToListExposedAsICollection() {
            Content = @"
                item.inners[0].value = 1.1
                item.inners[1].value = 1.2
                item.inners[2].value = 1.3
            ";
            var obj = Subject.Configure(new ClassWithListExposedAsICollection(), "item");
            CollectionAssert.AreEqual(new[] { 1.1, 1.2, 1.3 }, obj.Inners.Select(i => i.Value));
        }

        [Test]
        public void Sources_ReturnsStringSource() {
            Content = @"
                item.inners[0].value = 1.1
                item.inners[1].value = 1.2
                item.inners[2].value = 1.3
            ";
            var actual = Subject.Sources;
            var expected = new[] { Content.ToString() };
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestCase("item.inners[0].value", "1.1")]
        [TestCase("item.inners[1].value", "1.2")]
        [TestCase("item.inners[2].value", "1.3")]
        [TestCase("item  . inners\t[ 0 ]\t.value\t", "1.1")]
        [TestCase("item.Inners [\t 1\t ]  .value", "1.2")]
        [TestCase("ITEM.inners[   \t  2  \t  ] . VAlue  ", "1.3")]
        public void Lookup_GetsValueOfKeyWithIndex(string key, string value) {
            Content = @"
                item.inners[0].value = 1.1
                item.inners[1].value = 1.2
                item.inners[2].value = 1.3
            ";
            var actual = Subject.Lookup.Value(key);
            var expected = value;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Lookup_GetsAllValuesWithIndex() {
            Content = @"
                item.  inners[1].VALUE = 3.4
                item.inners[0].value = 1.1
                item.Inners[1].value = 1.2
                item.inners[2].value = 1.3
                item.inners[   1  ].value = 5.6
                item.inners[ 1  ].value = 7.8
            ";
            var actual = Subject.Lookup.All("ITEM . INNERS [ 1 ] . VALUE");
            var expected = new[] { "3.4", "1.2", "5.6", "7.8" };
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Lookup_RespectsSpacesInsideIndex() {
            Content = @"
                item.  inners[ a b ].VALUE = 3.4
                item.inners[0].value = 1.1
                item.Inners[ ab ].value = 1.2
                item.inners[2].value = 1.3
                item.inners[a b].value = 5.6
                item.inners[    a b    ].value = 7.8
            ";
            var actual = Subject.Lookup.All("ITEM . INNERS [ a b ] . VALUE");
            var expected = new[] { "3.4", "5.6", "7.8" };
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Lookup_RespectsCaseInsideIndex() {
            Content = @"
                item.  inners[ a B ].VALUE = 3.4
                item.inners[0].value = 1.1
                item.Inners[ ab ].value = 1.2
                item.inners[2].value = 1.3
                item.inners[a b].value = 5.6
                item.inners[    a b    ].value = 7.8
            ";
            var actual = Subject.Lookup.All("ITEM . INNERS [ a b ] . VALUE");
            var expected = new[] { "5.6", "7.8" };
            CollectionAssert.AreEqual(expected, actual);
        }

        private class ObjWithOptionalNames {
            [Conf("sp", "stringproperty")]
            public string StrProp { get; set; }
        }

        [TestCase("sp")]
        [TestCase("strprop")]
        [TestCase("stringproperty")]
        [TestCase("STRINGproperty")]
        public void Configure_RespectsConfAttributeNames(string name) {
            Content = $"obj.{name} = hello world";
            var obj = Subject.Configure(new ObjWithOptionalNames(), "obj");
            Assert.That(obj.StrProp, Is.EqualTo("hello world"));
        }

        [TestCase("sp")]
        [TestCase("strprop")]
        [TestCase("stringproperty")]
        [TestCase("STRINGproperty")]
        public void Configure_RespectsConfAttributeNamesWithoutKey(string name) {
            Content = $"{name} = hello world";
            var obj = Subject.Configure(new ObjWithOptionalNames(), "");
            Assert.That(obj.StrProp, Is.EqualTo("hello world"));
        }

        [TestCase("spp")]
        [TestCase("sstrprop")]
        [TestCase("stringroperty")]
        [TestCase("STINGproperty")]
        public void Configure_DoesNotUseNameNotInAttributeList(string name) {
            Content = $"{name} = hello world";
            var obj = Subject.Configure(new ObjWithOptionalNames(), "");
            Assert.That(obj.StrProp, Is.Not.EqualTo("hello world"));
        }

        private class ClassWithNamedListExposedAsICollection {
            [Conf("Inner")]
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
        public void Configure_AddsItemsToListExposedAsICollection2() {
            Content = @"
                item.inner[0].value = 1.1
                item.inner[1].value = 1.2
                item.inner[2].value = 1.3
            ";
            var obj = Subject.Configure(new ClassWithNamedListExposedAsICollection(), "item");
            CollectionAssert.AreEqual(new[] { 1.1, 1.2, 1.3 }, obj.Inners.Select(i => i.Value));
        }

        [Test]
        public void Configure_GetsNameOfActualObjectType() {
            Content = "Cat.Color = plaid";
            var pet = Subject.Configure<Pet>(new Cat());
            var cat = (Cat)pet;
            Assert.That(cat.Color, Is.EqualTo("plaid"));
        }

        private class ClassWithStringPropertiesForConverterTest {
            [ConfConverter(typeof(Converter))]
            public string Foo { get; set; }

            [ConfConverter(typeof(Converter))]
            public string[] Bar { get; set; }

            private class Converter : ConfValueConverter {
                private string Convert(string value) {
                    if (value == "\\r") return "\r";
                    if (value == "\\n") return "\n";
                    if (value == "\\r\\n") return "\r\n";
                    return value;
                }

                public override object Convert(string value, ConfValueConverterState state) {
                    if (state.Property.Name == nameof(Foo)) {
                        return Convert(value);
                    }
                    if (state.Property.Name == nameof(Bar)) {
                        if (value != null) {
                            return value
                                .Split()
                                .Where(s => !string.IsNullOrWhiteSpace(s))
                                .Select(s => Convert(s))
                                .ToArray();
                        }
                        return value;
                    }
                    throw new ArgumentException();
                }
            }
        }

        [TestCase("Foo = \\r", "\r")]
        [TestCase("Foo = \\n", "\n")]
        [TestCase("Foo = \\r\\n", "\r\n")]
        public void Configure_UsesPrivateConverterClassInstance(string content, string expected) {
            Content = content;
            var inst = Subject.Configure(new ClassWithStringPropertiesForConverterTest(), key: "");
            var actual = inst.Foo;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Configure_UsesPrivateConverterClassInstanceToConvertToArray() {
            Content = "Bar = \\r \\n";
            var inst = Subject.Configure(new ClassWithStringPropertiesForConverterTest(), key: "");
            var actual = inst.Bar;
            CollectionAssert.AreEqual(new[] { "\r", "\n" }, actual);
        }

        [Test]
        public void Lookup_GetsMultiLineValue() {
            Content = @"
                here is some = {


                    text 1
                    text 2
                    
                }
            ";
            var actual = Subject.Lookup.Value("HEREISSOME");
            var expected = @"

                    text 1
                    text 2
                    ";
            Assert.That(actual, Is.EqualTo(expected));
        }

        public sealed class EnumIndexParameter {
            public enum MyEnum {
                Tiny,
                Small,
                Big
            }

            public Dictionary<MyEnum, string> Dict { get; set; }
        }

        [TestCase(EnumIndexParameter.MyEnum.Tiny, "this is tiny")]
        [TestCase(EnumIndexParameter.MyEnum.Big, "this is BIG")]
        public void Configure_SetsIndexedPropertiesWithEnumIndex(EnumIndexParameter.MyEnum key, string expected) {
            Content = @"
                dict[tiny] = this is tiny
                dict[BIG] = this is BIG
            ";
            var actual = Subject.Configure(new EnumIndexParameter(), key: "").Dict[key];
            Assert.That(actual, Is.EqualTo(expected));
        }

        private sealed class Shipwreck {
            public int Depth { get; private set; }
        }

        [Test]
        public void Configure_SetsPropertiesWithPrivateSetter() {
            Content = "depth = 25";
            var actual = Subject.Configure(new Shipwreck(), key: "").Depth;
            var expected = 25;
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
