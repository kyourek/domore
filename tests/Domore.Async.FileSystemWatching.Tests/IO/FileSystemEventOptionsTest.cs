using NUnit.Framework;
using System.IO;

namespace Domore.IO;

[TestFixture]
internal sealed class FileSystemEventOptionsTest {
    [Test]
    public void Equals_ReturnsTrue_ForIdenticalOptions() {
        var a = new FileSystemEventOptions {
            FileFilter = "*.txt",
            IncludeSubdirectories = true,
            InternalBufferSize = 4096,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
        };
        var b = new FileSystemEventOptions {
            FileFilter = "*.txt",
            IncludeSubdirectories = true,
            InternalBufferSize = 4096,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
        };
        using (Assert.EnterMultipleScope()) {
            Assert.That(a.Equals(b));
            Assert.That(b.Equals(a));
            Assert.That(a.Equals((object)b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }
    }

    [Test]
    public void Equals_ReturnsFalse_ForDifferentOptions() {
        var a = new FileSystemEventOptions {
            FileFilter = "*.txt",
            IncludeSubdirectories = true,
            InternalBufferSize = 4096,
            NotifyFilter = NotifyFilters.FileName
        };
        var b = new FileSystemEventOptions {
            FileFilter = "*.log",
            IncludeSubdirectories = false,
            InternalBufferSize = 2048,
            NotifyFilter = NotifyFilters.LastWrite
        };
        using (Assert.EnterMultipleScope()) {
            Assert.That(a.Equals(b), Is.False);
            Assert.That(b.Equals(a), Is.False);
            Assert.That(a.Equals((object)b), Is.False);
            Assert.That(a.GetHashCode(), Is.Not.EqualTo(b.GetHashCode()));
        }
    }

    [Test]
    public void Equals_ReturnsFalse_WhenOtherIsNull() {
        var b = default(FileSystemEventOptions);
        var a = new FileSystemEventOptions {
            FileFilter = "*.txt",
            IncludeSubdirectories = true,
            InternalBufferSize = 4096,
            NotifyFilter = NotifyFilters.FileName
        };
        using (Assert.EnterMultipleScope()) {
            Assert.That(a.Equals(b), Is.False);
            Assert.That(a.Equals((object)b), Is.False);
        }
    }

    [Test]
    public void GetHashCode_IsConsistent_ForSameValues() {
        var a = new FileSystemEventOptions {
            FileFilter = "*.txt",
            IncludeSubdirectories = false,
            InternalBufferSize = null,
            NotifyFilter = null
        };
        var b = new FileSystemEventOptions {
            FileFilter = "*.txt",
            IncludeSubdirectories = false,
            InternalBufferSize = null,
            NotifyFilter = null
        };
        Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
    }

    [Test]
    public void Properties_AreSetCorrectly() {
        var options = new FileSystemEventOptions {
            FileFilter = "*.cs",
            IncludeSubdirectories = true,
            InternalBufferSize = 8192,
            NotifyFilter = NotifyFilters.DirectoryName
        };
        using (Assert.EnterMultipleScope()) {
            Assert.That(options.FileFilter, Is.EqualTo("*.cs"));
            Assert.That(options.IncludeSubdirectories);
            Assert.That(options.InternalBufferSize, Is.EqualTo(8192));
            Assert.That(options.NotifyFilter, Is.EqualTo(NotifyFilters.DirectoryName));
        }
    }
}