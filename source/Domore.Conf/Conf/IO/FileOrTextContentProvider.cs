using Domore.Conf.Text;
using Domore.IO;
using System;
using System.Collections.Generic;
using System.IO;
using FILE = System.IO.File;

namespace Domore.Conf.IO;

internal sealed class FileOrTextContentProvider : ConfContentProviderBase {
    private PathFormatter PathFormatter => field ??= new();
    private TextContentProvider Text => field ??= new();
    private FileContentProvider File => field ??= new();

    public sealed override ConfContent GetConfContent(object source,
                                                      IEnumerable<object> sources,
                                                      ConfContentProviderContext context) {
        var file = $"{source}".Trim();
        if (file != "" && file.IndexOfAny(Path.GetInvalidPathChars()) < 0) {
            var expand = PathFormatter.Expand(Environment.ExpandEnvironmentVariables(file));
            if (expand.IndexOfAny(Path.GetInvalidPathChars()) < 0) {
                var path = context?.ResolvePath(expand) ?? expand;
                if (FILE.Exists(path)) {
                    return File.GetConfContent(path, sources, context);
                }
            }
        }
        return Text.GetConfContent(source, sources, context);
    }
}
