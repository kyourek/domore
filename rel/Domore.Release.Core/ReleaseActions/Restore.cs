using System.IO;

namespace Domore.ReleaseActions {
    internal class Restore : ReleaseAction {
        public override void Work() =>
            Process(
                "nuget",
                "restore",
                $"\"{Solution.Path}\"",
                "-MSBuildPath",
                $"\"{Path.GetDirectoryName(ProcessPath["msbuild"])}\"");
    }
}
