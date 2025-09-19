using Domore.Conf.Extensions;
using NUnit.Framework;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace Domore.Conf.Converters;

[TestFixture]
public sealed class ConfListItemsTest {
    private class Kid {
        [ConfListItems]
        public List<string> FavoriteColors { get; set; }

        [ConfListItems(Separator = "&"), Conf("pets")]
        public List<string> PetNames { get; set; }
    }

    [Test]
    public void ConvertsItemsIntoList() {
        var actual = new Kid().ConfFrom($"kid.FavoriteColors = Red, green,   BLUE").FavoriteColors;
        var expected = new[] { "Red", "green", "BLUE" };
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ConvertsItemsIntoListWithSpecifiedName() {
        var actual = new Kid().ConfFrom($"kid.pets= Little Bit & Penny").PetNames;
        var expected = new[] { "Little Bit", "Penny" };
        Assert.That(actual, Is.EqualTo(expected));
    }

    private class Pi {
        [ConfListItems]
        public List<int> Digits { get; set; }
    }

    [Test]
    public void ConvertsItemsIntoListOfInt() {
        var actual = new Pi().ConfFrom($"PI.digits = 3, 1,4 ").Digits;
        var expected = new[] { 3, 1, 4 };
        Assert.That(actual, Is.EqualTo(expected));
    }

    [TypeConverter(typeof(PairWithDefaultTypeConverter.Converter))]
    private class PairWithDefaultTypeConverter {
        public string Thing1 { get; set; }
        public double Thing2 { get; set; }

        public sealed class Converter : TypeConverter {
            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
                var s = $"{value}";
                var p = s.Split('&');
                return new PairWithDefaultTypeConverter {
                    Thing1 = p[0],
                    Thing2 = double.Parse(p[1])
                };
            }
        }
    }

    private class PairWithDefaultTypeConverters {
        [ConfListItems]
        public List<PairWithDefaultTypeConverter> Items { get; set; }
    }

    [Test]
    public void ConvertsUsingDefaultItemTypeConverter() {
        var items = new PairWithDefaultTypeConverters().ConfFrom($"Items = str1&1.2 , str2&2.3 , str3&3.4", key: "").Items;
        var actual = items.SelectMany(item => new object[] { item.Thing1, item.Thing2 }).ToArray();
        var expected = new object[] { items[0].Thing1, items[0].Thing2, items[1].Thing1, items[1].Thing2, items[2].Thing1, items[2].Thing2 };
        Assert.That(actual, Is.EqualTo(expected));
    }

    private class PairWithSpecifiedTypeConverter {
        public string Thing1 { get; set; }
        public double Thing2 { get; set; }

        public sealed class Converter : TypeConverter {
            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
                var s = $"{value}";
                var p = s.Split('&');
                return new PairWithSpecifiedTypeConverter {
                    Thing1 = p[0],
                    Thing2 = double.Parse(p[1])
                };
            }
        }
    }

    private class PairWithSpecifiedTypeConverters {
        [ConfListItems(ItemConverter = typeof(PairWithSpecifiedTypeConverter.Converter)), Conf("Things")]
        public List<PairWithSpecifiedTypeConverter> Items { get; set; }
    }

    [Test]
    public void ConvertsUsingSpecifiedItemTypeConverter() {
        var items = new PairWithSpecifiedTypeConverters().ConfFrom($"things = str1&1.2 , str2&2.3 , str3&3.4", key: "").Items;
        var actual = items.SelectMany(item => new object[] { item.Thing1, item.Thing2 }).ToArray();
        var expected = new object[] { items[0].Thing1, items[0].Thing2, items[1].Thing1, items[1].Thing2, items[2].Thing1, items[2].Thing2 };
        Assert.That(actual, Is.EqualTo(expected));
    }

    private class PairWithConfValueConverter {
        public string Thing1 { get; set; }
        public double Thing2 { get; set; }

        public sealed class Converter : ConfValueConverter {
            public override object Convert(string value, ConfValueConverterState state) {
                var s = $"{value}";
                var p = s.Split('&');
                return new PairWithConfValueConverter {
                    Thing1 = p[0],
                    Thing2 = double.Parse(p[1])
                };
            }
        }
    }

    private class PairWithConfValueConverters {
        [ConfListItems(ItemConverter = typeof(PairWithConfValueConverter.Converter))]
        public List<PairWithConfValueConverter> Items { get; set; }
    }

    [Test]
    public void ConvertsUsingConfValueConverter() {
        var items = new PairWithConfValueConverters().ConfFrom($"ITEMS = str1&1.2, str2&2.3 ,str3&3.4", key: "").Items;
        var actual = items.SelectMany(item => new object[] { item.Thing1, item.Thing2 }).ToArray();
        var expected = new object[] { items[0].Thing1, items[0].Thing2, items[1].Thing1, items[1].Thing2, items[2].Thing1, items[2].Thing2 };
        Assert.That(actual, Is.EqualTo(expected));
    }


    private class PairWithConfValueConvertersAndSeparator {
        [ConfListItems(Separator = "|", ItemConverter = typeof(PairWithConfValueConverter.Converter))]
        public List<PairWithConfValueConverter> Items { get; set; }
    }

    [Test]
    public void ConvertsUsingSpecifiedSeparator() {
        var items = new PairWithConfValueConvertersAndSeparator().ConfFrom($"ITEMS = str1&1.2| str2&2.3 |str3&3.4", key: "").Items;
        var actual = items.SelectMany(item => new object[] { item.Thing1, item.Thing2 }).ToArray();
        var expected = new object[] { items[0].Thing1, items[0].Thing2, items[1].Thing1, items[1].Thing2, items[2].Thing1, items[2].Thing2 };
        Assert.That(actual, Is.EqualTo(expected));
    }
}
