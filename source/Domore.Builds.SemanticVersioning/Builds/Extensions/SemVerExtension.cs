using System;

namespace Domore.Builds.Extensions;

public static class SemVerExtension {
    public static bool StagedRevision(this SemVer semVer, out string stage, out int revision) {
        if (null == semVer) throw new ArgumentNullException(nameof(semVer));
        var prerelease = semVer.PreRelease?.Trim() ?? "";
        if (prerelease == "") {
            stage = null;
            revision = 0;
            return false;
        }
        var parts = prerelease.Split('.');
        if (parts.Length == 1) {
            stage = parts[0];
            revision = 0;
            return true;
        }
        stage = parts[0];
        revision = int.TryParse(parts[1], out var r) ? r : -1;
        return parts.Length == 2 && revision >= 0;
    }

    public static string StagedRevision(string stage, int revision) {
        return revision == 0
            ? $"{stage}"
            : $"{stage}.{revision}";
    }

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
