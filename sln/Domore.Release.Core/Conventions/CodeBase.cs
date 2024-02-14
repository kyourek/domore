using System;
using System.Linq;
using PATH = System.IO.Path;

namespace Domore.Conventions {
    public sealed class CodeBase {
        private string Id =>
            _Id ?? (
            _Id = string.Join("_", Name, DateTime.Now.ToString("yyyyMMddhhmmss"), Guid.NewGuid().ToString("N")));
        private string _Id;

        public string Name =>
            _Name ?? (
            _Name = PATH.GetFileNameWithoutExtension(Repository.Split('/').Last()));
        private string _Name;

        public string Path =>
            _Path ?? (
            _Path = PATH.Combine(Root, Id));
        private string _Path;

        public Solution Solution =>
            _Solution ?? (
            _Solution = new Solution(Name, PATH.Combine(Path, "sln")));
        private Solution _Solution;

        public string Root { get; }
        public string Repository { get; }

        public CodeBase(string repository, string root = null) {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            Root = root ?? PATH.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify),
                "Domore",
                "Release",
                "CodeBase");
        }
    }
}
