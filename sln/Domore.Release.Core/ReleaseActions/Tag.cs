namespace Domore.ReleaseActions {
    internal class Tag : ReleaseAction {
        public string Stage { get; set; }

        public override void Work() {
            var tag = Solution.GetVersion(Stage).StagedVersion;
            Process("git", "commit", "-a", "-m", $"\"(Auto-)Commit version '{tag}'.\"");
            Process("git", "push");
            Process("git", "tag", "-a", tag, "-m", $"\"(Auto-)Tag version '{tag}'.\"");
            Process("git", "push", "origin", tag);
        }
    }
}
