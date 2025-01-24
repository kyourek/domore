using System.Collections.Generic;
using System.Linq;

namespace Domore.ReleaseActions {
    internal class Pack : ReleaseAction {
        public string Configuration { get; set; }
        public string Platform { get; set; }
        public IList<string> Package { get; } = new List<string>();

        public override void Work() {
            foreach (var package in Package) {
                var proj = Solution.Projects.Single(p => p.Name == package);
                var path = proj.Path;
                Process("MSBuild",
                    $"-t:pack",
                    $"-p:Configuration=\"{Configuration}\"",
                    $"-p:Platform=\"{Platform}\"",
                    $"-p:IncludeSymbols=true",
                    $"-p:IncludeSource=true",
                    $"\"{path}\"");
            }
        }
    }
}
