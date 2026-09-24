using Domore.Conf.Converters;
using Domore.Conf.Extensions;
using NUnit.Framework;
using System.Collections.Generic;

namespace Domore.Conf;

[TestFixture]
internal sealed class ConfCultureTest {
    private sealed class NumericValues {
        public decimal Amount { get; set; }
        public double Measure { get; set; }
        public List<double> Items { get; set; } = [];
        public Dictionary<double, double> Mapping { get; set; } = [];

        [ConfListItems]
        public List<double> ConvertedItems { get; set; } = [];
    }

    private sealed class NullableIndexMap {
        private readonly Dictionary<double, string> Values = [];

        public string this[double? index] {
            get => Values[index.Value];
            set => Values[index.Value] = value;
        }
    }

    private sealed class NullableIndexedValues {
        public NullableIndexMap Values { get; set; } = new();
    }

    [Test]
    [SetCulture("de-DE")]
    [SetUICulture("de-DE")]
    public void ConfTextFormatsNumbersInvariantly() {
        var values = new NumericValues {
            Amount = 1234.5m,
            Measure = 67.89,
            Items = [2.5],
            Mapping = { [3.5] = 4.5 }
        };

        var text = values.ConfText(key: "");

        using (Assert.EnterMultipleScope()) {
            Assert.That(text, Does.Contain("Amount = 1234.5"));
            Assert.That(text, Does.Contain("Measure = 67.89"));
            Assert.That(text, Does.Contain("Items[0] = 2.5"));
            Assert.That(text, Does.Contain("Mapping[3.5] = 4.5"));
        }

        var roundTrip = new NumericValues().ConfFrom(text, key: "");
        using (Assert.EnterMultipleScope()) {
            Assert.That(roundTrip.Amount, Is.EqualTo(values.Amount));
            Assert.That(roundTrip.Measure, Is.EqualTo(values.Measure));
            Assert.That(roundTrip.Items, Is.EqualTo(values.Items));
            Assert.That(roundTrip.Mapping, Is.EqualTo(values.Mapping));
        }
    }

    [Test]
    [SetCulture("de-DE")]
    [SetUICulture("de-DE")]
    public void ConfTextParsesInvariantNumbersUnderLocalizedCulture() {
        const string text = """
            NumericValues.Amount = 1234.5
            NumericValues.Measure = 67.89
            NumericValues.Items[0] = 2.5
            NumericValues.Mapping[3.5] = 4.5
            NumericValues.ConvertedItems = 5.5, 6.5
            """;

        var values = new NumericValues().ConfFrom(text);

        using (Assert.EnterMultipleScope()) {
            Assert.That(values.Amount, Is.EqualTo(1234.5m));
            Assert.That(values.Measure, Is.EqualTo(67.89));
            Assert.That(values.Items, Is.EqualTo(new[] { 2.5 }));
            Assert.That(values.Mapping[3.5], Is.EqualTo(4.5));
            Assert.That(values.ConvertedItems, Is.EqualTo(new[] { 5.5, 6.5 }));
        }
    }

    [Test]
    [SetCulture("de-DE")]
    [SetUICulture("de-DE")]
    public void ConfTextParsesNullableIndexInvariantlyUnderLocalizedCulture() {
        const string text = "NullableIndexedValues.Values[1.5] = localized";

        var values = new NullableIndexedValues().ConfFrom(text);

        Assert.That(values.Values[1.5], Is.EqualTo("localized"));
    }

    private sealed class SingleIndexMap {
        private readonly Dictionary<int, string> Values = [];

        public string this[int index] {
            get => Values[index];
            set => Values[index] = value;
        }
    }

    private sealed class SingleIndexedValues {
        public SingleIndexMap Values { get; set; } = new();
    }

    [Test]
    public void ConfTextRejectsExcessIndexerParts() {
        const string text = "SingleIndexedValues.Values[0,1] = extra";

        Assert.That(
            () => new SingleIndexedValues().ConfFrom(text),
            Throws.TypeOf<ConfException>().With.Message.Contains("expects 1 index parts"));
    }
}
