using System.Diagnostics;
using System.IO;

namespace Domore.Conf {
    using IO;

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
            var procFile = proc.MainModule.FileName;
            var confFile = Path.ChangeExtension(procFile, ".conf");
            if (confFile.Contains(".vshost") && File.Exists(confFile) == false) {
                confFile = confFile.Replace(".vshost", "");
            }
            if (File.Exists(confFile)) {
                return confFile;
            }
            var confFileDefault = confFile + ".default";
            if (File.Exists(confFileDefault)) {
                File.Copy(confFileDefault, confFile);
                return confFile;
            }
            return "";
        }

        public ConfContent GetConfContent(object source) {
            var s = source?.ToString()?.Trim() ?? "";
            if (s == "") {
                source = ConfFile;
            }
            return FileOrText.GetConfContent(source);
        }
    }
}
