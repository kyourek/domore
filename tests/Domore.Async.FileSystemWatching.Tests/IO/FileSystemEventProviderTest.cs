using NUnit.Framework;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.IO;

[TestFixture]
internal sealed class FileSystemEventProviderTest {
    private string TempPath;

    [SetUp]
    public void SetUp() {
        TempPath = Path.Combine(Path.GetTempPath(), "Domore", "Domore.Async.FileSystemWatching.Tests", nameof(FileSystemEventProviderTest));
        Directory.CreateDirectory(TempPath);
    }

    [TearDown]
    public void TearDown() {
        Directory.Delete(TempPath, recursive: true);
    }

    [Test]
    public void Path_IsSetInConstructor() {
        var subject = new FileSystemEventProvider(TempPath);
        var actual = subject.Path;
        var expected = TempPath;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Foo() {
        var path = Path.Combine(TempPath, nameof(Foo));
        File.WriteAllText(path, "foo");
        var ready = false;
        var actual = default(WatcherChangeTypes);
        var subject = new FileSystemEventProvider(TempPath);
        var events = subject.Events(ready: async _ => {
            ready = true;
            await Task.CompletedTask;
        });
        _ = Task.Run(async () => {
            await foreach (var e in events) {
                actual = e.ChangeType;
                break;
            }
        });
        SpinWait.SpinUntil(() => ready == true, 2500);
        File.WriteAllText(path, "bar");
        SpinWait.SpinUntil(() => actual != default, 2500);
        Assert.That(actual, Is.EqualTo(WatcherChangeTypes.Changed));
    }
}
