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
}
