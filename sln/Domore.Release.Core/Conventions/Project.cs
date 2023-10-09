using FILE = System.IO.File;
using PATH = System.IO.Path;

namespace Domore.Conventions {
    public sealed class Project {
        public string Root { get; }
        public string Extension { get; }

        public string Name =>
            PATH.GetFileName(Root);

        public string Path =>
            PATH.Combine(Root, Name + Extension);

        public string BinPath =>
            PATH.Combine(Root, "bin");

        public bool Exists =>
            FILE.Exists(Path);

        public Project(string root, string extension) {
            Root = root;
            Extension = extension;
        }
    }
}
