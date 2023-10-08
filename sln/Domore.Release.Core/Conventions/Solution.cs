using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using PATH = System.IO.Path;

namespace Domore.Conventions {
    public class Solution {
        public string Root { get; }
        public string Name { get; }

        public string Path =>
            _Path ?? (
            _Path = PATH.Combine(Root, $"{Name}.sln"));
        private string _Path;

        public string File => 
            _File ?? (
            _File = PATH.GetFileName(Path));
        private string _File;

        public string Properties => 
            _Properties ?? (
            _Properties = PATH.Combine(Root, "Directory.Build.props"));
        private string _Properties;

        public IEnumerable<Project> Projects {
            get {
                var directories = Directory.GetDirectories(Root);
                foreach (var directory in directories) {
                    var extensions = new[] { ".csproj" };
                    foreach (var extension in extensions) {
                        var project = new Project(directory, extension);
                        if (project.Exists) {
                            yield return project;
                            break;
                        }
                    }
                }
            }
        }

        public Solution(string name, string root) {
            Name = name;
            Root = root;
        }

        public Version GetVersion(string stage) {
            var propsPath = Properties;
            var propsXDoc = XDocument.Load(propsPath);
            var propertyGroup = propsXDoc.Root.Element("PropertyGroup");

            var fileVersion = propertyGroup.Element("FileVersion").Value;
            var fullVersion = Version.ParseFileVersion(fileVersion, stage);

            return fullVersion;
        }

        public void SetVersion(Version value) {
            var propsPath = Properties;
            var propsXDoc = XDocument.Load(propsPath);
            var propGroup = propsXDoc.Root.Element("PropertyGroup");

            propGroup.Element("VersionPrefix").Value = value.VersionPrefix;
            propGroup.Element("VersionSuffix").Value = value.VersionSuffix;
            propGroup.Element("AssemblyVersion").Value = value.AssemblyVersion;
            propGroup.Element("InformationalVersion").Value = value.InformationalVersion;
            propGroup.Element("FileVersion").Value = value.FileVersion;
            propGroup.Element("PackageVersion").Value = value.PackageVersion;

            propsXDoc.Save(propsPath);
        }

        public void SetRepository(string url, string branch, string commit) {
            var propsPath = Properties;
            var propsXDoc = XDocument.Load(propsPath);
            var propGroup = propsXDoc.Root.Element("PropertyGroup");

            propGroup.Element("RepositoryUrl").Value = url;
            propGroup.Element("RepositoryBranch").Value = branch;
            propGroup.Element("RepositoryCommit").Value = commit;

            propsXDoc.Save(propsPath);
        }
    }
}
