namespace Domore.ReleaseActions {
    internal class Clone : ReleaseAction {
        public string Stage { get; set; }

        public string Branch {
            get => _Branch ?? (_Branch = "master");
            set => _Branch = value;
        }
        private string _Branch;

        public override void Work() {
            var path = CodeBase.Path;
            var repo = CodeBase.Repository;

            Process("git", "clone", repo, path);
            Process("git", "checkout", Branch);

            Solution.SetRepository(repo, Branch, Process("git", "rev-parse", "HEAD"));
        }
    }
}
