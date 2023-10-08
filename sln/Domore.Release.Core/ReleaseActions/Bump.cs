namespace Domore.ReleaseActions {
    internal class Bump : ReleaseAction {
        public string Stage { get; set; }

        public override void Work() {
            var thisVersion = Solution.GetVersion(Stage);
            var nextVersion = thisVersion.NextRevision();

            Solution.SetVersion(nextVersion);
        }
    }
}
