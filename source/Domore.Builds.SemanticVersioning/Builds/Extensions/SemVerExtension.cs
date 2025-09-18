using System;

namespace Domore.Builds.Extensions;

public static class SemVerExtension {
    public static string AssemblyVersion(this SemVer semVer) {
        if (null == semVer) throw new ArgumentNullException(nameof(semVer));
        return $"{semVer.Major}.{semVer.Minor}.{semVer.Patch}";
    }

    public static string FileVersion(this SemVer semVer) {
        return AssemblyVersion(semVer);
    }

    public static string InformationalVersion(this SemVer semVer) {
        if (null == semVer) throw new ArgumentNullException(nameof(semVer));
        return AssemblyVersion(semVer) + (semVer.PreRelease == null ? "" : $"-{semVer.PreRelease}");
    }

    public static string PackageVersion(this SemVer semVer) {
        return InformationalVersion(semVer);
    }

    public static string VersionPrefix(this SemVer semVer) {
        return AssemblyVersion(semVer);
    }

    public static string VersionSuffix(this SemVer semVer) {
        return semVer.PreRelease;
    }
}
