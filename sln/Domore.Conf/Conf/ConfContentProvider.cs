using Domore.Conf.IO;
using System.Diagnostics;
using System.IO;

namespace Domore.Conf {
    internal sealed class ConfContentProvider : IConfContentProvider {
        private FileOrTextContentProvider FileOrText =>
            _FileOrText ?? (
            _FileOrText = new FileOrTextContentProvider());
        private FileOrTextContentProvider _FileOrText;

        private string ConfFile =>
            _ConfFile ?? (
            _ConfFile = GetConfFile());
        private string _ConfFile;

        private string GetConfFile() {
            var proc = Process.GetCurrentProcess();
            var procFile = proc?.MainModule?.FileName?.Trim() ?? "";
            if (procFile == "") {
                return "";
            }
            var confFile = Path.ChangeExtension(procFile, ".conf");
            var confFileExists = File.Exists(confFile);
            if (confFileExists == false && confFile.Contains(".vshost")) {
                confFile = confFile.Replace(".vshost", "");
                confFileExists = File.Exists(confFile);
            }
            if (confFileExists) {
                return confFile;
            }
            var confFileDefault = confFile + ".default";
            var confFileDefaultExists = File.Exists(confFileDefault);
            if (confFileDefaultExists) {
                try {
                    File.Copy(confFileDefault, confFile);
                    return confFile;
                }
                catch {
                    return confFileDefault;
                }
            }
            return "";
        }

        public ConfContent GetConfContent(object source) {
            source = string.IsNullOrWhiteSpace(source?.ToString())
                ? ConfFile
                : source;
            return FileOrText.GetConfContent(source);
        }
    }
}
