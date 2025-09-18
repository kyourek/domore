using NUnit.Framework;
using System;
using System.Linq;
using System.Numerics;

namespace Domore.Builds;

[TestFixture]
public sealed class SemVerTest {
    public static object[] ValidSemanticVersions => new[]
    {
        "0.0.4",
        "1.2.3",
        "10.20.30",
        "1.1.2-prerelease+meta",
        "1.1.2+meta",
        "1.1.2+meta-valid",
        "1.0.0-alpha",
        "1.0.0-beta",
        "1.0.0-alpha.beta",
        "1.0.0-alpha.beta.1",
        "1.0.0-alpha.1",
        "1.0.0-alpha0.valid",
        "1.0.0-alpha.0valid",
        "1.0.0-alpha-a.b-c-somethinglong+build.1-aef.1-its-okay",
        "1.0.0-rc.1+build.1",
        "2.0.0-rc.1+build.123",
        "1.2.3-beta",
        "10.2.3-DEV-SNAPSHOT",
        "1.2.3-SNAPSHOT-123",
        "1.0.0",
        "2.0.0",
        "1.1.7",
        "2.0.0+build.1848",
        "2.0.1-alpha.1227",
        "1.0.0-alpha+beta",
        "1.2.3----RC-SNAPSHOT.12.9.1--.12+788",
        "1.2.3----R-S.12.9.1--.12+meta",
        "1.2.3----RC-SNAPSHOT.12.9.1--.12",
        "1.0.0+0.build.1-rc.10000aaa-kk-0.1",
        "99999999999999999999999.999999999999999999.99999999999999999",
        "1.0.0-0A.is.legal"
    };

    public static object[] InvalidSemanticVersions => new[]
    {
        "1",
        "1.2",
        "1.2.3-0123",
        "1.2.3-0123.0123",
        "1.1.2+.123",
        "+invalid",
        "-invalid",
        "-invalid+invalid",
        "-invalid.01",
        "alpha",
        "alpha.beta",
        "alpha.beta.1",
        "alpha.1",
        "alpha+beta",
        "alpha_beta",
        "alpha.",
        "alpha..",
        "beta",
        "1.0.0-alpha_beta",
        "-alpha.",
        "1.0.0-alpha..",
        "1.0.0-alpha..1",
        "1.0.0-alpha...1",
        "1.0.0-alpha....1",
        "1.0.0-alpha.....1",
        "1.0.0-alpha......1",
        "1.0.0-alpha.......1",
        "01.1.1",
        "1.01.1",
        "1.1.01",
        "1.2",
        "1.2.3.DEV",
        "1.2-SNAPSHOT",
        "1.2.31.2.3----RC-SNAPSHOT.12.09.1--..12+788",
        "1.2-RC-SNAPSHOT",
        "-1.0.3-gamma+b7718",
        "+justmeta",
        "9.8.7+meta+meta",
        "9.8.7-whatever+meta+meta",
        "99999999999999999999999.999999999999999999.99999999999999999----RC-SNAPSHOT.12.09.1--------------------------------..12"
    };

    [TestCaseSource(nameof(ValidSemanticVersions))]
    public void Parse_DoesNotThrow(string v) {
        Assert.That(SemVer.Parse(v), Is.Not.Null);
    }

    [TestCaseSource(nameof(InvalidSemanticVersions))]
    public void Parse_ThrowsFormatException(string v) {
        Assert.That(() => SemVer.Parse(v), Throws.InstanceOf<FormatException>());
    }

    [TestCaseSource(nameof(ValidSemanticVersions))]
    public void TryParse_SucceedsWithValidSemanticVersions(string v) {
        Assert.That(SemVer.TryParse(v, out _), Is.True);
    }

    [TestCaseSource(nameof(InvalidSemanticVersions))]
    public void TryParse_FailsWithInvalidSemanticVersions(string v) {
        Assert.That(SemVer.TryParse(v, out _), Is.False);
    }

    [TestCaseSource(nameof(ValidSemanticVersions))]
    public void TryParse_SetsVersionWhenSuccessful(string v) {
        SemVer.TryParse(v, out var semver);
        Assert.That($"{semver}", Is.EqualTo(v));
    }

    [TestCaseSource(nameof(InvalidSemanticVersions))]
    public void TryParse_SetsOutputToNullWhenUnsuccessful(string v) {
        SemVer.TryParse(v, out var semver);
        Assert.That(semver, Is.Null);
    }

    [TestCaseSource(nameof(ValidSemanticVersions))]
    public void Equals_IsTrueForSameVersion(string v) {
        var a = SemVer.Parse(v);
        var b = SemVer.Parse(v);
        Assert.That(a, Is.EqualTo(b));
    }

    [TestCaseSource(nameof(ValidSemanticVersions))]
    public void Equals_IsFalseWhenMajorDiffers(string v) {
        var a = SemVer.Parse(v);
        var b = new SemVer(a.Major + 1, a.Minor, a.Patch, a.PreRelease, a.Build);
        Assert.That(a, Is.Not.EqualTo(b));
    }

    [TestCaseSource(nameof(ValidSemanticVersions))]
    public void Equals_IsFalseWhenMinorDiffers(string v) {
        var a = SemVer.Parse(v);
        var b = new SemVer(a.Major, a.Minor + 1, a.Patch, a.PreRelease, a.Build);
        Assert.That(a, Is.Not.EqualTo(b));
    }

    [TestCaseSource(nameof(ValidSemanticVersions))]
    public void Equals_IsFalseWhenPatchDiffers(string v) {
        var a = SemVer.Parse(v);
        var b = new SemVer(a.Major, a.Minor, a.Patch + 1, a.PreRelease, a.Build);
        Assert.That(a, Is.Not.EqualTo(b));
    }

    [TestCaseSource(nameof(ValidSemanticVersions))]
    public void Equals_IsFalseWhenPreReleaseDiffers(string v) {
        var a = SemVer.Parse(v);
        var b = new SemVer(a.Major, a.Minor, a.Patch, string.IsNullOrWhiteSpace(a.PreRelease) ? "pre" : null, a.Build);
        Assert.That(a, Is.Not.EqualTo(b));
    }

    [TestCaseSource(nameof(ValidSemanticVersions))]
    public void Equals_IsFalseWhenBuildDiffers(string v) {
        var a = SemVer.Parse(v);
        var b = new SemVer(a.Major, a.Minor, a.Patch, a.PreRelease, string.IsNullOrWhiteSpace(a.Build) ? "bld" : null);
        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void CompareTo_SortsVersions() {
        var input = new[]
        {
            "1.0.0-rc.1",
            "1.0.0-beta",
            "1.0.0-alpha.beta",
            "1.0.0-beta.11",
            "1.0.0-beta.2",
            "1.0.0-alpha",
            "1.0.0",
            "1.0.0-alpha.1"
        };
        var expected = new[]
        {
            "1.0.0-alpha",
            "1.0.0-alpha.1",
            "1.0.0-alpha.beta",
            "1.0.0-beta",
            "1.0.0-beta.2",
            "1.0.0-beta.11",
            "1.0.0-rc.1",
            "1.0.0"
        };
        var actual = input
            .Select(SemVer.Parse)
            .OrderBy(v => v)
            .Select(v => $"{v}")
            .ToList();
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Zero_Major_IsZero() {
        Assert.That(SemVer.Zero.Major, Is.EqualTo((BigInteger)0));
    }

    [Test]
    public void Zero_Minor_IsZero() {
        Assert.That(SemVer.Zero.Minor, Is.EqualTo((BigInteger)0));
    }

    [Test]
    public void Zero_Patch_IsZero() {
        Assert.That(SemVer.Zero.Patch, Is.EqualTo((BigInteger)0));
    }

    [Test]
    public void Zero_PreRelease_IsNull() {
        Assert.That(SemVer.Zero.PreRelease, Is.Null);
    }

    [Test]
    public void Zero_Build_IsNull() {
        Assert.That(SemVer.Zero.Build, Is.Null);
    }

    [Test]
    public void Zero_EqualsItself() {
        Assert.That(SemVer.Zero.Equals(SemVer.Zero));
    }

    [Test]
    public void Zero_EqualsAnotherInstanceOfZero() {
        Assert.That(SemVer.Zero.Equals(new SemVer(0, 0, 0, null, null)));
    }

    [Test]
    public void Zero_EqualsAnotherInstanceOfZeroViaOperator() {
        Assert.That(SemVer.Zero == new SemVer(0, 0, 0, null, null));
    }

    [TestCase("1.1.0", "1.0.0")]
    [TestCase("1.0.1", "1.0.0")]
    [TestCase("1.1.1", "1.1.0")]
    [TestCase("1.1.2", "1.1.1")]
    [TestCase("1.1.1-alpha.2", "1.1.1-alpha.1")]
    [TestCase("1.1.1-beta", "1.1.1-alpha")]
    public void GreaterThan_ReturnsTrue(string a, string b) {
        var y = SemVer.Parse(a);
        var z = SemVer.Parse(b);
        Assert.That(y > z);
    }

    [TestCase("1.1.0", "1.1.0")]
    [TestCase("1.0.1", "1.0.2")]
    [TestCase("1.1.1", "1.2.0")]
    [TestCase("1.1.1-alpha.2", "1.1.1-alpha.2")]
    [TestCase("1.1.1-alpha", "1.1.1-beta")]
    public void GreaterThan_ReturnsFalse(string a, string b) {
        var y = SemVer.Parse(a);
        var z = SemVer.Parse(b);
        Assert.That(y > z, Is.False);
    }

    [TestCase("1.0.0", "1.0.1")]
    [TestCase("21.21.21", "21.22.0")]
    [TestCase("21.21.21", "22.0.0")]
    [TestCase("100.9999.0-y.532", "100.9999.0-z.532")]
    [TestCase("100.9999.0-y.532", "100.9999.0-y.533")]
    public void LessThan_ReturnsTrue(string a, string b) {
        var y = SemVer.Parse(a);
        var z = SemVer.Parse(b);
        Assert.That(y < z);
    }

    [TestCase("1.0.0", "1.0.1")]
    [TestCase("21.21.21", "21.22.0")]
    [TestCase("21.21.21", "22.0.0")]
    [TestCase("100.9999.0-y.532", "100.9999.0-z.532")]
    [TestCase("100.9999.0-y.532", "100.9999.0-y.533")]
    [TestCase("100.9999.0-y.533", "100.9999.0-y.533")]
    public void LessThan_ReturnsFalse(string a, string b) {
        var y = SemVer.Parse(a);
        var z = SemVer.Parse(b);
        Assert.That(z < y, Is.False);
    }

    [TestCase("1.0.0", "1.0.0")]
    [TestCase("1.1.0", "1.0.0")]
    [TestCase("1.0.1", "1.0.0")]
    [TestCase("1.1.0", "1.1.0")]
    [TestCase("1.1.1", "1.1.0")]
    [TestCase("1.1.1", "1.1.1")]
    [TestCase("1.1.2", "1.1.1")]
    [TestCase("1.1.1-alpha", "1.1.1-alpha")]
    [TestCase("1.1.1-alpha.1", "1.1.1-alpha.1")]
    [TestCase("1.1.1-alpha.2", "1.1.1-alpha.1")]
    [TestCase("1.1.1-beta", "1.1.1-alpha")]
    public void GreaterThanOrEqualTo_ReturnsTrue(string a, string b) {
        var y = SemVer.Parse(a);
        var z = SemVer.Parse(b);
        Assert.That(y >= z);
    }

    [TestCase("1.1.0", "1.0.0")]
    [TestCase("1.0.1", "1.0.0")]
    [TestCase("1.1.1", "1.1.0")]
    [TestCase("1.1.2", "1.1.1")]
    [TestCase("1.1.1-alpha.2", "1.1.1-alpha.1")]
    [TestCase("1.1.1-beta", "1.1.1-alpha")]
    public void GreaterThanOrEqualTo_ReturnsFalse(string a, string b) {
        var y = SemVer.Parse(a);
        var z = SemVer.Parse(b);
        Assert.That(z >= y, Is.False);
    }

    [TestCase("1.0.0", "1.0.0")]
    [TestCase("1.1.0", "1.0.0")]
    [TestCase("1.0.1", "1.0.0")]
    [TestCase("1.1.0", "1.1.0")]
    [TestCase("1.1.1", "1.1.0")]
    [TestCase("1.1.1", "1.1.1")]
    [TestCase("1.1.2", "1.1.1")]
    [TestCase("1.1.1-alpha", "1.1.1-alpha")]
    [TestCase("1.1.1-alpha.1", "1.1.1-alpha.1")]
    [TestCase("1.1.1-alpha.2", "1.1.1-alpha.1")]
    [TestCase("1.1.1-beta", "1.1.1-alpha")]
    public void LessThanOrEqualTo_ReturnsTrue(string a, string b) {
        var y = SemVer.Parse(a);
        var z = SemVer.Parse(b);
        Assert.That(z <= y);
    }

    [TestCase("1.1.0", "1.0.0")]
    [TestCase("1.0.1", "1.0.0")]
    [TestCase("1.1.1", "1.1.0")]
    [TestCase("1.1.2", "1.1.1")]
    [TestCase("1.1.1-alpha.2", "1.1.1-alpha.1")]
    [TestCase("1.1.1-beta", "1.1.1-alpha")]
    public void LessThanOrEqualTo_ReturnsFalse(string a, string b) {
        var y = SemVer.Parse(a);
        var z = SemVer.Parse(b);
        Assert.That(y <= z, Is.False);
    }
}
