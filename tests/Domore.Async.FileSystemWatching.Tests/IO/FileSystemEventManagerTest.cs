using Domore.IO.FileSystemEventSubscriptions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    [Test]
    public async Task Events_ChangeType_IsChangedMultipleTimes() {
        var path = Path.Combine(TempPath, nameof(Events_ChangeType_IsChanged));
        var actual = default(int);
        File.WriteAllText(path, "foo");
        await Subject.Add(
            path: TempPath,
            options: new() { NotifyFilter = NotifyFilters.LastWrite },
            token: default,
            subscription: new ProxyFileSystemEventSubscription {
                Agent = async (e, _) => {
                    if (e.ChangeType == WatcherChangeTypes.Changed) {
                        Interlocked.Increment(ref actual);
                    }
                    await Task.CompletedTask;
                }
            });
        await Task.Delay(250);
        File.WriteAllText(path, "bar");
        File.WriteAllText(path, "baz");
        File.WriteAllText(path, "cud");
        SpinWait.SpinUntil(() => actual > 2, 2500);
        Assert.That(actual, Is.GreaterThanOrEqualTo(3));
    }

    [Test]
    public async Task Events_ChangeType_IsChangedForMultipleSubscriptions() {
        var path = Path.Combine(TempPath, nameof(Events_ChangeType_IsChanged));
        var actual = new HashSet<object>();
        var subs = Enumerable.Range(1, 10).Select(i => {
            var 
            sub = new ProxyFileSystemEventSubscription();
            sub.Agent = async (e, _) => {
                if (e.ChangeType == WatcherChangeTypes.Changed) {
                    lock (actual) {
                        actual.Add(sub);
                    }
                }
            };
            return sub;
        });
        File.WriteAllText(path, "foo");
        await Task.WhenAll(subs.Select(sub => Subject.Add(
            path: TempPath,
            options: null,
            token: default,
            subscription: sub)));
        await Task.Delay(250);
        File.WriteAllText(path, "bar");
        SpinWait.SpinUntil(() => actual.Count >= 10, 2500);
        Assert.That(actual, Has.Count.EqualTo(10));
    }
}
