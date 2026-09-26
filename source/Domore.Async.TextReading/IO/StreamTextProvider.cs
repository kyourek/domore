using System;
using System.IO;

namespace Domore.IO;

internal sealed class StreamTextProvider {
    private static IStreamText ConvertValue(object value) {
        if (value is null) {
            return null;
        }
        if (value is IStreamText streamText) {
            return streamText;
        }
        if (value is FileInfo fileInfo) {
            return new StreamTextSourceFile(fileInfo);
        }
        if (value is Uri uri) {
            if (uri.IsFile) {
                return FromFile(uri.LocalPath);
            }
            return null;
        }
        if (value is string s) {
            if (!string.IsNullOrWhiteSpace(s)) {
                return FromFile(s);
            }
            return null;
        }
        return null;
    }

    private static StreamTextSourceFile FromFile(string path) {
        try {
            var fullPath = Path.GetFullPath(path, AppContext.BaseDirectory);
            return new StreamTextSourceFile(new FileInfo(fullPath));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) {
            return null;
        }
    }

    public IStreamText GetStreamingText(object value) {
        var result = ConvertValue(value);
        return result;
    }
}
