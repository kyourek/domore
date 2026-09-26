using NUnit.Framework;
using System;
using System.IO;

namespace Domore.IO;

[TestFixture]
public class StreamTextSourceFileTest {
    [Test]
    public void StreamLength_RefreshesFileMetadata() {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
        var subject = new StreamTextSourceFile(new FileInfo(path));

        try {
            Assert.That(() => subject.StreamLength, Throws.TypeOf<FileNotFoundException>());

            File.WriteAllBytes(path, new byte[] { 1, 2, 3 });
            Assert.That(subject.StreamLength, Is.EqualTo(3));

            File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4, 5 });
            Assert.That(subject.StreamLength, Is.EqualTo(5));
        }
        finally {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }
    }
}
