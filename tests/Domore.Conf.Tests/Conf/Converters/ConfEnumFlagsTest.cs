using Domore.Conf.Extensions;
using NUnit.Framework;
using System;

namespace Domore.Conf.Converters;

[TestFixture]
public sealed class ConfEnumFlagsTest {
    [Flags]
    private enum Colors {
        Red = 1,
        Green = 2,
        Blue = 4
    }

    private class Kid {
        [ConfEnumFlags]
        public Colors FavoriteColors { get; set; }

        [ConfEnumFlags, Conf("favs")]
        public Colors FavoriteColorsWithShortName { get; set; }

        [ConfEnumFlags(Separators = "#")]
        public Colors FavoriteColorsSeparatedByHash { get; set; }
    }

    [TestCase("red", Colors.Red)]
    [TestCase(" Green ", Colors.Green)]
    [TestCase("\t  BLUE \t  ", Colors.Blue)]
    [TestCase("red|green", Colors.Red | Colors.Green)]
    [TestCase("red,green", Colors.Red | Colors.Green)]
    [TestCase(" rEd , \t grEEn   ", Colors.Red | Colors.Green)]
    [TestCase("red | green", Colors.Red | Colors.Green)]
    [TestCase("green + blue", Colors.Blue | Colors.Green)]
    [TestCase("GREEN\t&red&\tBlue \t", Colors.Blue | Colors.Green | Colors.Red)]
    [TestCase("green + blue", Colors.Blue | Colors.Green)]
    [TestCase("RED/blue", Colors.Red | Colors.Blue)]
    [TestCase("green\\red\\blue", Colors.Red | Colors.Blue | Colors.Green)]
    public void ConvertsEnumFlags(string s, int expected) {
        var actual = new Kid().ConfFrom($"kid.favorite colors = {s}").FavoriteColors;
        Assert.That(actual, Is.EqualTo((Colors)expected));
    }

    [TestCase("green + blue", Colors.Blue | Colors.Green)]
    public void ConvertsEnumFlagsWithDifferentName(string s, int expected) {
        var actual = new Kid().ConfFrom($"kid.FAVs = {s}").FavoriteColorsWithShortName;
        Assert.That(actual, Is.EqualTo((Colors)expected));
    }

    [Test]
    public void ConvertsWithSpecifiedSeparators() {
        var actual = new Kid().ConfFrom($"kid.favoritecolorsseparatedByHash = Green # blue").FavoriteColorsSeparatedByHash;
        var expected = Colors.Green | Colors.Blue;
        Assert.That(actual, Is.EqualTo((Colors)expected));
    }
}
