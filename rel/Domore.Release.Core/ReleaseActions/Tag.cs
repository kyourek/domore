namespace Domore.ReleaseActions {
    internal class Tag : ReleaseAction {
        public string Stage { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }

        public override void Work() {
            var userName = UserName;
            if (userName != null) {
                Process("git", "config", "user.name", $"\"{userName}\"");
            }
            var userEmail = UserEmail;
            if (userEmail != null) {
                Process("git", "config", "user.email", $"\"{userEmail}\"");
            }
            var tag = Solution.GetVersion(Stage).StagedVersion;
            Process("git", "commit", "-a", "-m", $"\"(Auto-)Commit version '{tag}'.\"");
            Process("git", "push");
            Process("git", "tag", "-a", tag, "-m", $"\"(Auto-)Tag version '{tag}'.\"");
            Process("git", "push", "origin", tag);
        }
    }
}
