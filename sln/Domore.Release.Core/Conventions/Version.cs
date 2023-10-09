using System;

namespace Domore.Conventions {
    public sealed class Version {
        private Version(int major, int minor, int build, string stage, int revision) {
            Major = major;
            Minor = minor;
            Build = build;
            Stage = stage;
            Revision = revision;
        }

        public int Major { get; }
        public int Minor { get; }
        public int Build { get; }
        public int Revision { get; }
        public string Stage { get; }

        public string AssemblyVersion =>
            $"{Major}.{Minor}.{Build}.{Revision}";

        public string StagedVersion => Stage == null
            ? $"{Major}.{Minor}.{Build}.{Revision}"
            : $"{Major}.{Minor}.{Build}-{Stage}.{Revision}";

        public string VersionPrefix => Stage == null
            ? $"{Major}.{Minor}.{Build}.{Revision}"
            : $"{Major}.{Minor}.{Build}";

        public string VersionSuffix => Stage == null
            ? ""
            : $"{Stage}.{Revision}";

        public string FileVersion => AssemblyVersion;
        public string PackageVersion => StagedVersion;
        public string InformationalVersion => StagedVersion;

        public static Version ParseFileVersion(string value, string stage) {
            value = value ?? throw new ArgumentNullException(nameof(value));
            value = value.Trim();

            var parts = value.Split('.');
            var major = parts.Length > 0 ? parts[0] : "0";
            var minor = parts.Length > 1 ? parts[1] : "0";
            var build = parts.Length > 2 ? parts[2] : "0";
            var revsn = parts.Length > 3 ? parts[3] : "0";

            return new Version(
                major: int.Parse(major),
                minor: int.Parse(minor),
                build: int.Parse(build),
                stage: string.IsNullOrWhiteSpace(stage) ? null : stage.Trim(),
                revision: int.Parse(revsn));
        }

        public Version NextRevision() => new Version(
            major: Major,
            minor: Minor,
            build: Build,
            stage: Stage,
            revision: Revision + 1);
    }
}
