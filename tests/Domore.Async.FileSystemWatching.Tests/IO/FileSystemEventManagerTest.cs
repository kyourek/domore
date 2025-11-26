using Domore.IO.FileSystemEventSubscriptions;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.IO;

[TestFixture]
internal sealed class FileSystemEventManagerTest {
    private string TempPath;

    public FileSystemEventManager Subject {
        get => field ??= new();
        set => field = value;
    }

    [SetUp]
    public void SetUp() {
        Subject = null;
        TempPath = Path.Combine(
            Path.GetTempPath(),
            "Domore.Async.FileSystemWatching.Tests",
            nameof(FileSystemEventManagerTest),
            Guid.NewGuid().ToString());
        Directory.CreateDirectory(TempPath);
    }

    [TearDown]
    public void TearDown() {
        Directory.Delete(TempPath, recursive: true);
    }

    [Test]
    public async Task Events_ChangeType_IsChanged() {
        var path = Path.Combine(TempPath, nameof(Events_ChangeType_IsChanged));
        var actual = default(WatcherChangeTypes);
        File.WriteAllText(path, "foo");
        await Subject.Add(
            path: TempPath,
            options: null,
            token: default,
            subscription: new ProxyFileSystemEventSubscription {
                Agent = async (e, _) => {
                    actual = e.ChangeType;
                    await Task.CompletedTask;
                }
            });
        await Task.Delay(250);
        File.WriteAllText(path, "bar");
        SpinWait.SpinUntil(() => actual != default, 2500);
        Assert.That(actual, Is.EqualTo(WatcherChangeTypes.Changed));
    }
}
