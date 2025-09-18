using NUnit.Framework;

namespace Domore.Builds.Extensions;

[TestFixture]
internal sealed class SemVerExtensionTest {
    [Test]
    public void VersionSuffix_IsPreRelease() {
        var semVer = new SemVer(2, 3, 4, "alpha.5");
        var actual = SemVerExtension.VersionSuffix(semVer);
        var expected = "alpha.5";
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void VersionPrefix_ExcludesPreRelease() {
        var semVer = new SemVer(2, 3, 4, "alpha.5");
        var actual = SemVerExtension.VersionPrefix(semVer);
        var expected = "2.3.4";
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void AssemblyVersion_ExcludesPreRelease() {
        var semVer = new SemVer(2, 3, 4, "alpha.5");
        var actual = SemVerExtension.AssemblyVersion(semVer);
        var expected = "2.3.4";
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void FileVersion_ExcludesPreRelease() {
        var semVer = new SemVer(2, 3, 4, "alpha.5");
        var actual = SemVerExtension.FileVersion(semVer);
        var expected = "2.3.4";
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void InformationalVersion_IncludesPreRelease() {
        var semVer = new SemVer(2, 3, 4, "alpha.5");
        var actual = SemVerExtension.InformationalVersion(semVer);
        var expected = "2.3.4-alpha.5";
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void PackageVersion_IncludesPreRelease() {
        var semVer = new SemVer(2, 3, 4, "alpha.5");
        var actual = SemVerExtension.PackageVersion(semVer);
        var expected = "2.3.4-alpha.5";
        Assert.That(actual, Is.EqualTo(expected));
    }
}
