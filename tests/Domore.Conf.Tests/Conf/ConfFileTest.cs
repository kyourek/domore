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

    [Test]
    public void SetsProperties() {
        Content = "Foo.Bar = Hello, World!";
        var foo = new Foo();
        Target = foo;
        Subject.Configure();
        Assert.That(foo.Bar, Is.EqualTo("Hello, World!"));
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
