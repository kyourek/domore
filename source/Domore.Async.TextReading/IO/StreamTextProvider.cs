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

    /// <summary>
    /// Converts a value to an <see cref="IStreamText"/>.
    /// </summary>
    /// <param name="value">An <see cref="IStreamText"/>, a file path, a <see cref="FileInfo"/>, or a local file <see cref="Uri"/>.</param>
    /// <returns>The converted source, or null if the value is null, unsupported, a non-file URI, or an invalid path.</returns>
    public IStreamText GetStreamingText(object value) {
        var result = ConvertValue(value);
        return result;
    }
}
