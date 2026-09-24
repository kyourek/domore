using Domore.Conf.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Domore.Conf;

internal sealed class ConfContentProvider : ConfContentProviderBase {
    private FileOrTextContentProvider FileOrText => field ??= new();
    private string ConfFile => field ??= GetConfFile();

    private static string GetConfFile() {
        var appFile = Assembly.GetEntryAssembly()?.Location?.Trim() ?? "";
        if (appFile == "") {
            var appName = AppDomain.CurrentDomain.FriendlyName?.Trim() ?? "";
            if (appName == "") {
                return "";
            }
            appFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, appName);
        }
        var confFile = Path.ChangeExtension(appFile, ".conf");
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
