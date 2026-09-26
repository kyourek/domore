using System;
using System.IO;

namespace Domore.IO;

internal sealed class StreamTextSourceFile : StreamTextSource {
    public sealed override long StreamLength {
        get {
            FileInfo.Refresh();
            return FileInfo.Length;
        }
    }

    public FileInfo FileInfo { get; }

    public StreamTextSourceFile(FileInfo fileInfo) {
        FileInfo = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo));
    }

    public sealed override Stream StreamText() {
        /*
         * Buffering is disabled because StreamSequenceSegmenter reads into its own pooled buffers.
         */
        return new FileStream(FileInfo.FullName,
                              FileMode.Open,
                              FileAccess.Read,
                              FileShare.Read,
                              bufferSize: 1,
                              FileOptions.Asynchronous);
    }

    public sealed override string ToString() {
        return FileInfo.ToString();
    }
}
