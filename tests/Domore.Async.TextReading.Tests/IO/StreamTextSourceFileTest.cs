using NUnit.Framework;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.IO;

[TestFixture]
public class StreamTextSourceFileTest {
    [Test]
    public void StreamLength_HonorsCancellationToken() {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
        var subject = new StreamTextSourceFile(new FileInfo(path));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.CatchAsync<OperationCanceledException>(
            async () => await subject.StreamLength(cancellation.Token));
    }

    [Test]
    public async Task StreamLength_RefreshesFileMetadata() {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
        var subject = new StreamTextSourceFile(new FileInfo(path));

        try {
            Assert.ThrowsAsync<FileNotFoundException>(async () => await subject.StreamLength(CancellationToken.None));

            File.WriteAllBytes(path, new byte[] { 1, 2, 3 });
            Assert.That(await subject.StreamLength(CancellationToken.None), Is.EqualTo(3));

            File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4, 5 });
            Assert.That(await subject.StreamLength(CancellationToken.None), Is.EqualTo(5));
        }
        finally {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }
    }
}
