using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.IO;

internal sealed class StreamTextSourceFile : StreamTextSource {
    public sealed override Task<long> StreamLength(CancellationToken cancellationToken) {
        return Task.Run(
            () => {
                FileInfo.Refresh();
                return FileInfo.Length;
            },
            cancellationToken);
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
