namespace Domore.ReleaseActions {
    internal class Build : ReleaseAction {
        public string Configuration { get; set; }
        public string Platform { get; set; }

        public override void Work() =>
            Process("MSBuild",
                $"-restore",
                $"-t:Clean",
                $"-t:Rebuild",
                $"-p:Configuration=\"{Configuration}\"",
                $"-p:Platform=\"{Platform}\"",
                $"\"{Solution.Path}\"");
    }
}
