using System;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Domore.Builds;

/// <summary>
/// Instances of this type represent a semantic version. See https://semver.org.
/// </summary>
public sealed class SemVer : IEquatable<SemVer>, IComparable, IComparable<SemVer> {
    private string String { get; }

    private int HashCode => _HashCode ??= String.GetHashCode();
    private int? _HashCode;

    /// <summary>
    /// See https://semver.org
    /// See also https://regex101.com/r/vkijKf/1/
    /// </summary>
    private static readonly Regex Regex = new(
        @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$",
        RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    private static GroupCollection Match(string s) {
        if (null == s) throw new ArgumentNullException(nameof(s));
        var matches = Regex.Matches(s);
        if (matches.Count == 0) {
            throw new FormatException("No matches");
        }
        if (matches.Count != 1) {
            throw new FormatException("More than one match");
        }
        return matches[0].Groups;
    }

    /// <summary>
    /// Gets the version 0.0.0, with no pre-release or build information.
    /// </summary>
    public static readonly SemVer Zero = new(0, 0, 0, null, null);

    /// <summary>
    /// Gets the major part of the version, i.e. the x in x.0.0.
    /// </summary>
    public BigInteger Major { get; }

    /// <summary>
    /// Gets the minor part of the version, i.e. the x in 0.x.0.
    /// </summary>
    public BigInteger Minor { get; }

    /// <summary>
    /// Gets the patch part of the version, i.e. the x in 0.0.x.
    /// </summary>
    public BigInteger Patch { get; }

    /// <summary>
    /// Gets the pre-release data of the version. Pre-release data occurs after <see cref="Patch"/>
    /// and is preceded by a dash, e.g. 1.0.0-alpha.
    /// </summary>
    public string PreRelease { get; }

    /// <summary>
    /// Gets the build data of the version. build data occurs after <see cref="PreRelease"/> or
    /// <see cref="Patch"/> and is preceded by a plus, e.g. 1.0.0+e198a2
    /// </summary>
    public string Build { get; }

    /// <summary>
    /// Creates a new instance of a semantic version.
    /// </summary>
    /// <param name="major">The major part of the version, i.e. the x in x.0.0.</param>
    /// <param name="minor">The minor part of the version, i.e. the x in 0.x.0.</param>
    /// <param name="patch">The patch part of the version, i.e. the x in 0.0.x.</param>
    /// <param name="preRelease">The pre-release data of the version.</param>
    /// <param name="build">The build data of the version.</param>
    /// <exception cref="ArgumentException"></exception>
    public SemVer(BigInteger major, BigInteger minor, BigInteger patch, string preRelease = null, string build = null) {
        Major = major;
        Minor = minor;
        Patch = patch;
        PreRelease = string.IsNullOrWhiteSpace(preRelease) ? null : preRelease.Trim();
        Build = string.IsNullOrWhiteSpace(build) ? null : build.Trim();
        String = $"{Major}.{Minor}.{Patch}"
            + (PreRelease != null ? $"-{PreRelease}" : "")
            + (Build != null ? $"+{Build}" : "");
        try {
            Match(String);
        }
        catch (Exception ex) {
            throw new ArgumentException(message: "Invalid", innerException: ex);
        }
    }

    /// <summary>
    /// Gets the complete version string.
    /// </summary>
    /// <returns>The complete version string.</returns>
    public sealed override string ToString() {
        return String;
    }

    /// <summary>
    /// Gets the hash code of the version.
    /// </summary>
    /// <returns>The hash code of the version.</returns>
    public sealed override int GetHashCode() {
        return HashCode;
    }

    /// <summary>
    /// Determines whether or not this instance is equal to <paramref name="obj"/>.
    /// </summary>
    /// <param name="obj">The object to check for equality with this instance.</param>
    /// <returns>True if the two instances are considered equal. Otherwise, false.</returns>
    public sealed override bool Equals(object obj) {
        return Equals(obj as SemVer);
    }

    /// <summary>
    /// Determines whether or not this instance is equal to <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The instance to check for equality with this instance.</param>
    /// <returns>True if the two instances are considered equal. Otherwise, false.</returns>
    public bool Equals(SemVer other) {
        return
            other != null &&
            other.String == String;
    }

    /// <summary>
    /// Compares <paramref name="obj"/> to the instance.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>The sort order result of the comparison.</returns>
    public int CompareTo(object obj) {
        return CompareTo(obj as SemVer);
    }

    /// <summary>
    /// Compares <paramref name="other"/> to the instance.
    /// </summary>
    /// <param name="other">The instance to compare.</param>
    /// <returns>The sort order result of the comparison.</returns>
    public int CompareTo(SemVer other) {
        static int compare(string preReleaseA, string preReleaseB) {
            var b = preReleaseB?.Trim() ?? "";
            var a = preReleaseA?.Trim() ?? "";
            if (a == b) {
                return 0;
            }
            if (a == "") {
                return 1;
            }
            var bparts = b.Split('.');
            var aparts = a.Split('.');
            for (var i = 0; i < aparts.Length; i++) {
                if (bparts.Length == i) {
                    break;
                }
                var bi = BigInteger.TryParse(bparts[i], out var _bi) ? _bi : default(BigInteger?);
                var ai = BigInteger.TryParse(aparts[i], out var _ai) ? _ai : default(BigInteger?);
                if (ai.HasValue) {
                    if (bi.HasValue) {
                        if (ai > bi) {
                            return 1;
                        }
                        if (ai < bi) {
                            return -1;
                        }
                        continue;
                    }
                    else {
                        return -1;
                    }
                }
                else {
                    if (bi.HasValue) {
                        return 1;
                    }
                }
                var c = aparts[i].CompareTo(bparts[i]);
                if (c != 0) {
                    return c;
                }
            }
            return aparts.Length.CompareTo(bparts.Length);
        }
        if (other is null) {
            return 1;
        }
        var major = Major.CompareTo(other.Major);
        if (major != 0) {
            return major;
        }
        var minor = Minor.CompareTo(other.Minor);
        if (minor != 0) {
            return minor;
        }
        var patch = Patch.CompareTo(other.Patch);
        if (patch != 0) {
            return patch;
        }
        if (PreRelease == other.PreRelease) {
            return StringComparer.InvariantCultureIgnoreCase.Compare(Build, other.Build);
        }
        if (PreRelease == null) {
            return 1;
        }
        if (other.PreRelease == null) {
            return -1;
        }
        return compare(PreRelease, other.PreRelease);
    }

    /// <summary>
    /// Creates a new instance copied from this instance with changes according to the provided properties.
    /// </summary>
    /// <param name="major">The major part of the version, i.e. the x in x.0.0, or null to use the value of the current instance.</param>
    /// <param name="minor">The minor part of the version, i.e. the x in 0.x.0, or null to use the value of the current instance.</param>
    /// <param name="patch">The patch part of the version, i.e. the x in 0.0.x, or null to use the value of the current instance.</param>
    /// <param name="preRelease">The pre-release data of the version, or null to use the value of the current instance.</param>
    /// <param name="build">The build data of the version, or null to use the value of the current instance.</param>
    /// <returns>A new instance copied from this instance with changes according to the provided properties.</returns>
    public SemVer Change(BigInteger? major = null, BigInteger? minor = null, BigInteger? patch = null, string preRelease = null, string build = null) {
        return new SemVer(
            major: major ?? Major,
            minor: minor ?? Minor,
            patch: patch ?? Patch,
            preRelease: preRelease ?? PreRelease,
            build: build ?? Build);
    }

    public SemVer Bump(bool major = false, bool minor = false, bool patch = true) {
        return Change(
            major: major ? (Major + 1) : null,
            minor: major ? 0 : minor ? (Minor + 1) : null,
            patch: major ? 0 : minor ? 0 : patch ? (Patch + 1) : null);
    }

    /// <summary>
    /// Creates a new instance from the string <paramref name="s"/>.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <returns>A new instance created from the string <paramref name="s"/>.</returns>
    public static SemVer Parse(string s) {
        var groups = Match(s);
        return new SemVer(
            major: BigInteger.Parse(groups["1"].Value, CultureInfo.InvariantCulture),
            minor: BigInteger.Parse(groups["2"].Value, CultureInfo.InvariantCulture),
            patch: BigInteger.Parse(groups["3"].Value, CultureInfo.InvariantCulture),
            preRelease: groups["4"].Value,
            build: groups["5"].Value);
    }

    /// <summary>
    /// Attempts to parse <paramref name="s"/> into a new instance of <see cref="SemVer"/>.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="version">The parsed instance of <see cref="SemVer"/>.</param>
    /// <returns>True if parsing was successful. Otherwise, false.</returns>
    public static bool TryParse(string s, out SemVer version) {
        try {
            version = Parse(s);
            return true;
        }
        catch {
            version = null;
            return false;
        }
    }

    public static bool operator ==(SemVer a, SemVer b) {
        if (a is null) {
            if (b is null) {
                return true;
            }
            return false;
        }
        return a.Equals(b);
    }

    public static bool operator !=(SemVer a, SemVer b) {
        return !(a == b);
    }

    public static bool operator >=(SemVer a, SemVer b) {
        if (a is null || b is null) {
            return false;
        }
        return a.CompareTo(b) >= 0;
    }

    public static bool operator >(SemVer a, SemVer b) {
        if (a is null || b is null) {
            return false;
        }
        return a.CompareTo(b) > 0;
    }

    public static bool operator <=(SemVer a, SemVer b) {
        if (a is null || b is null) {
            return false;
        }
        return a.CompareTo(b) <= 0;
    }

    public static bool operator <(SemVer a, SemVer b) {
        if (a is null || b is null) {
            return false;
        }
        return a.CompareTo(b) < 0;
    }
}
