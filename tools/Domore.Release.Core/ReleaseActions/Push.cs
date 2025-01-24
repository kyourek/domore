using System.Collections.Generic;
using System.IO;

namespace Domore.ReleaseActions {
    internal class Push : ReleaseAction {
        public string Stage { get; set; }
        public string Configuration { get; set; }
        public string Platform { get; set; }
        public IList<string> Source { get; } = new List<string>();
        public IList<string> Package { get; } = new List<string>();

        public override void Work() {
            foreach (var package in Package) {
                var packageVer = Solution.GetVersion(Stage);
                var packageDir = Path.Combine(Solution.Root, package, "bin", Platform, Configuration);
                var packageFil = Path.Combine(packageDir, $"{package}.{packageVer.StagedVersion}.nupkg");

                foreach (var source in Source) {
                    Process("nuget", "push", $"\"{packageFil}\"", "-Source", source);
                }
            }
        }
    }
}
