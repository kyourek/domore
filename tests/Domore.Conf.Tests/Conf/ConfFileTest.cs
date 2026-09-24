using Domore.Conf.Text;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading;

namespace Domore.Conf;

[TestFixture]
internal sealed class ConfFileTest {
    private object Target;
    private string Key;

    public string TempFile {
        get => field ??= Path.GetTempFileName();
        set => field = value;
    }

    private ConfFile Subject {
        get => field ??= new ConfFile(TempFile, Key, Target) { Delay = 250 };
        set => field = value;
    }

    private string Content {
        get => field;
        set => File.WriteAllText(TempFile, field = value);
    }

    [SetUp]
    public void SetUp() {
        Key = null;
        Subject = null;
        TempFile = null;
    }

    [TearDown]
    public void TearDown() {
        Subject?.Dispose();
        File.Delete(TempFile);
    }

    private sealed class Foo {
        public string Bar { get; set; }
        public int Baz { get; set; }
    }

    private sealed class FallbackContentProvider : IConfContentProvider {
        public ConfContent GetConfContent(object source) {
            return new TextContentProvider().GetConfContent("Foo.Bar = fallback", null, null);
        }
    }

    [Test]
    public void SetsProperties() {
        Content = "Foo.Bar = Hello, World!";
        var foo = new Foo();
        Target = foo;
        Subject.Configure();
        Assert.That(foo.Bar, Is.EqualTo("Hello, World!"));
    }

    [TestCase(false, "")]
    [TestCase(false, " \r\n")]
    [TestCase(true, null)]
    public void EmptyOrMissingFileDoesNotLoadDefaultConfiguration(bool deleteFile, string content) {
        var previousProvider = Conf.ContentProvider;
        var foo = new Foo();
        try {
            Conf.ContentProvider = new FallbackContentProvider();
            Target = foo;
            if (deleteFile) {
                File.Delete(TempFile);
            }
            else {
                Content = content;
            }

            Subject.Configure();

            Assert.That(foo.Bar, Is.Null);
        }
        finally {
            Conf.ContentProvider = previousProvider;
        }
    }

    [Test]
    public void ConfiguresRelativeFileInCurrentDirectory() {
        var file = Path.GetRandomFileName();
        var foo = new Foo();
        File.WriteAllText(file, "Foo.Bar = Hello, World!");
        try {
            using var confFile = new ConfFile(file, Key, foo);
            confFile.Configure(watch: true);
            Assert.That(foo.Bar, Is.EqualTo("Hello, World!"));
        }
        finally {
            File.Delete(file);
        }
    }

    [Test]
    public void RelativeIncludeResolvesFromConfFileDirectory() {
        var previousSpecial = Conf.Special;
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var rootPath = Path.Combine(directory, "root.conf");
        var includedPath = Path.Combine(directory, "included.conf");
        try {
            Directory.CreateDirectory(directory);
            Conf.Special = "@conf";
            File.WriteAllText(rootPath, "@conf.include = included.conf");
            File.WriteAllText(includedPath, "Foo.Baz = 37");
            var foo = new Foo();
            using var confFile = new ConfFile(rootPath, key: null, target: foo);

            confFile.Configure();

            Assert.That(foo.Baz, Is.EqualTo(37));
        }
        finally {
            Conf.Special = previousSpecial;
            File.Delete(rootPath);
            File.Delete(includedPath);
            Directory.Delete(directory);
        }
    }

    [Test]
    public void WatchesFileForChanges() {
        Content = "Foo.Bar = Hello, World!";
        var foo = new Foo();
        var hit = false;
        Target = foo;
        Subject.Configure(watch: true);
        Subject.Configured += (s, e) => {
            hit = true;
        };
        Content = "Foo.Bar = Hi, Earth.";
        SpinWait.SpinUntil(() => hit);
        Assert.That(foo.Bar, Is.EqualTo("Hi, Earth."));
    }

    [Test]
    public void WatchesFileWithZeroDelay() {
        Content = "Foo.Baz = 0";
        var foo = new Foo();
        Target = foo;
        Subject.Delay = 0;
        Subject.Configure(watch: true);

        Content = "Foo.Baz = 1";

        Assert.That(SpinWait.SpinUntil(() => foo.Baz == 1, TimeSpan.FromSeconds(5)), Is.True);
    }

    [Test]
    public void RejectsDelayLessThanTimeoutInfinite() {
        Assert.That(() => Subject.Delay = -2, Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void DisposeCancelsPendingReconfigure() {
        Content = "Foo.Baz = 0";
        var foo = new Foo();
        Target = foo;
        Subject.Delay = 200;
        Subject.Configure();
        var errors = 0;
        Subject.ConfigureError += (s, e) => errors++;
        Content = "Foo.Baz = invalid";
        Subject.Watcher_Event(
            sender: null,
            new FileSystemEventArgs(WatcherChangeTypes.Changed, Subject.Directory, Subject.Name));

        Subject.Dispose();
        Thread.Sleep(300);

        using (Assert.EnterMultipleScope()) {
            Assert.That(errors, Is.Zero);
            Assert.That(foo.Baz, Is.Zero);
        }
    }

    [Test]
    public void ConfigureHandlesMissingDirectory() {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "settings.conf");
        using var confFile = new ConfFile(path, Key, new Foo());

        Assert.DoesNotThrow(() => confFile.Configure());
    }

    [Test]
    public void DelaysConfigureBetweenChanges() {
        Content = "Foo.Bar = Hello, World!";
        var foo = new Foo();
        var hit = 0;
        Target = foo;
        Subject.Configure(watch: true);
        Subject.Configured += (s, e) => {
            hit++;
        };
        Content = "Foo.Bar = Hi, Earth.";
        Thread.Sleep(100);
        Content = "foo . bar = Hi, Mars";
        SpinWait.SpinUntil(() => hit > 0);
        Assert.That(hit, Is.EqualTo(1));
        Assert.That(foo.Bar, Is.EqualTo("Hi, Mars"));
    }

    [Test]
    public void StopsWatching() {
        Content = "Foo.Bar = Hello, World!";
        var foo = new Foo();
        var hit = 0;
        Target = foo;
        Subject.Configure(watch: true);
        Subject.Configured += (s, e) => {
            hit++;
        };
        Content = "Foo.Bar = Hi, Earth.";
        SpinWait.SpinUntil(() => hit > 0);
        Subject.Configure(watch: false);
        Thread.Sleep(100);
        Content = "foo . bar = Hi, Mars";
        Thread.Sleep(750);
        Assert.That(hit, Is.EqualTo(1));
        Assert.That(foo.Bar, Is.EqualTo("Hi, Earth."));
    }

    [Test]
    public void ThrowsObjectDisposedException() {
        using (Subject) {
        }
        Assert.That(() => Subject.Configure(), Throws.TypeOf<ObjectDisposedException>());
    }

    [Test]
    public void WorksOKAfterLotsOfWrites() {
        var foo = new Foo();
        Target = foo;
        Content = "Foo.Baz = 0";
        Subject.Configure(watch: true);
        for (var i = 1; i < 100; i++) {
            Content = $"Foo.Baz = {i}";
        }
        SpinWait.SpinUntil(() => foo.Baz == 99);
        Assert.Pass();
    }
}
