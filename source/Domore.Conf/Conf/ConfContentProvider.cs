using Domore.Conf.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Domore.Conf {
    internal sealed class ConfContentProvider : ConfContentProviderBase {
        private FileOrTextContentProvider FileOrText => _FileOrText ??= new();
        private FileOrTextContentProvider _FileOrText;

        private string ConfFile => _ConfFile ??= GetConfFile();
        private string _ConfFile;

        private static string GetConfFile() {
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

        public sealed override ConfContent GetConfContent(object source, IEnumerable<object> sources, ConfContentProviderContext context) {
            source = string.IsNullOrWhiteSpace(source?.ToString())
                ? ConfFile
                : source;
            return FileOrText.GetConfContent(source, sources, context);
        }
    }
}
