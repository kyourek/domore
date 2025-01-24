using Domore.Conf.Text;
using Domore.IO;
using System;
using System.Collections.Generic;
using FILE = System.IO.File;

namespace Domore.Conf.IO {
    internal sealed class FileOrTextContentProvider : ConfContentProviderBase {
        private PathFormatter PathFormatter => _PathFormatter ??= new();
        private PathFormatter _PathFormatter;

        private TextContentProvider Text => _Text ??= new();
        private TextContentProvider _Text;

        private FileContentProvider File => _File ??= new();
        private FileContentProvider _File;

        public sealed override ConfContent GetConfContent(object source, IEnumerable<object> sources, ConfContentProviderContext context) {
            var file = $"{source}".Trim();
            if (file != "") {
                var expand = PathFormatter.Expand(Environment.ExpandEnvironmentVariables(file));
                var exists = FILE.Exists(expand);
                if (exists) {
                    return File.GetConfContent(expand, sources, context);
                }
            }
            return Text.GetConfContent(source, sources, context);
        }
    }
}
