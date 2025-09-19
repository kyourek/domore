using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Threading.Tasks;

[TestFixture]
public sealed class TaskCacheTest {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private static async Task<T> Get<T>(T obj) {
        return obj;
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    [Test]
    public async Task Ready_ReturnsResult() {
        var expected = new object();
        var subject = new TaskCache<object>(async _ => await Get(expected));
        var actual = await subject.Ready(CancellationToken.None);
        Assert.That(actual, Is.SameAs(expected));
    }

    [TestCase(1)]
    [TestCase(10)]
    [TestCase(100)]
    [TestCase(1000)]
    public void Ready_ReturnsTheSameResultConcurrently(int n) {
        var entry = 0;
        var result = new object();
        var subject = new TaskCache<object>(async _ => {
            return 0 == entry++
                ? await Get(result)
                : null;
        });
        var results = new List<object>();
        Enumerable.Range(0, n).ToList().ForEach(_ => ThreadPool.QueueUserWorkItem(async _ => {
            var item = await subject.Ready(CancellationToken.None);
            lock (results) {
                results.Add(item);
            }
        }));
        while (results.Count < n) {
            Thread.Sleep(0);
        }
        var expected = Enumerable.Range(0, n).Select(_ => result);
        var actual = results;
        Assert.That(actual, Is.EqualTo(expected));        
    }

    [Test]
    public async Task Ready_ThrowsTheExceptionThrownFromFactory() {
        var expected = new Exception();
        var actual = default(Exception);
        var subject = new TaskCache<object>(_ => throw expected);
        var task = subject.Ready(CancellationToken.None);
        try {
            await task;
        }
        catch (Exception ex) {
            actual = ex;
        }
        Assert.That(actual, Is.SameAs(expected));
    }

    [Test]
    public async Task Ready_ThrowsMostRecentExceptionThrownFromFactory() {
        var n = 1;
        var ex1 = new ArgumentException();
        var ex2 = new InvalidOperationException();
        var actual = new List<Exception>();
        var subject = new TaskCache<object>(_ => throw (1 == n++ ? ex1 : ex2));
        try {
            await subject.Ready(CancellationToken.None);
        }
        catch (Exception ex) {
            actual.Add(ex);
        }
        try {
            await subject.Ready(CancellationToken.None);
        }
        catch (Exception ex) {
            actual.Add(ex);
        }
        var expected = new Exception[] { ex1, ex2 };
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task Ready_ThrowsExceptionWhenCanceled() {
        using (var tokenSource = new CancellationTokenSource()) {
            var actual = default(OperationCanceledException);
            var subject = new TaskCache<object>(async token => {
                token.ThrowIfCancellationRequested();
                return await Get(new object());
            });
            tokenSource.Cancel();
            try {
                await subject.Ready(tokenSource.Token);
            }
            catch (OperationCanceledException ex) {
                actual = ex;
            }
#if NET40
            Assert.That(actual, Is.Not.Null);
#else
            Assert.That(actual.CancellationToken, Is.EqualTo(tokenSource.Token));
#endif
        }
    }

    [Test]
    public async Task Refresh_ResetsState() {
        var n = 0;
        var actual = new List<object>();
        var expected = new[] { new object(), new object() };
        var subject = new TaskCache<object>.WithRefresh(async _ => await Get(expected[n++]));
        actual.Add(await subject.Ready(CancellationToken.None));
        await subject.Refresh(CancellationToken.None);
        actual.Add(await subject.Ready(CancellationToken.None));
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task Refreshed_CallsFactoryAgain() {
        var n = 0;
        var actual = new List<object>();
        var expected = new[] { new object(), new object() };
        var subject = new TaskCache<object>.WithRefresh(async _ => await Get(expected[n++]));
        actual.Add(await subject.Ready(CancellationToken.None));
        actual.Add(await subject.Refreshed(CancellationToken.None));
        Assert.That(actual, Is.EqualTo(expected));
    }
}
