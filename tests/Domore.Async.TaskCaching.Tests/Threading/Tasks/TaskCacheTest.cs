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

    [Test]
    public void Constructor_ThrowsArgumentNullExceptionIfFactoryIsNull() {
        Assert.That(() => new TaskCache<object>(null), Throws.ArgumentNullException);
    }

    [Test]
    public void Ready_ThrowsInvalidOperationExceptionIfFactoryReturnsNull() {
        var subject = new TaskCache<object>(_ => null);
        Assert.That(async () => await subject.Ready(CancellationToken.None), Throws.InvalidOperationException);
    }

    [Test]
    public void Result_IsDefaultBeforeTaskCompletes() {
        var subject = new TaskCache<object>(async _ => await Get(new object()));
        Assert.That(subject.Result, Is.Null);
    }

    [Test]
    public async Task Result_IsCachedValueAfterTaskCompletes() {
        var expected = new object();
        var subject = new TaskCache<object>(async _ => await Get(expected));
        await subject.Ready(CancellationToken.None);
        Assert.That(subject.Result, Is.SameAs(expected));
    }

    [Test]
    public async Task Result_IsDefaultAfterRefresh() {
        var subject = new TaskCache<object>.WithRefresh(async _ => await Get(new object()));
        await subject.Ready(CancellationToken.None);
        await subject.Refresh(CancellationToken.None);
        Assert.That(subject.Result, Is.Null);
    }

    [Test]
    public async Task Ready_CallsFactoryOnlyOnceAfterSuccess() {
        var calls = 0;
        var subject = new TaskCache<object>(async _ => {
            Interlocked.Increment(ref calls);
            return await Get(new object());
        });
        await subject.Ready(CancellationToken.None);
        await subject.Ready(CancellationToken.None);
        await subject.Ready(CancellationToken.None);
        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public async Task Ready_CachesResultOfFirstSuccessAfterFailures() {
        var n = 0;
        var expected = new object();
        var subject = new TaskCache<object>(async _ => {
            if (n++ < 2) {
                throw new InvalidOperationException();
            }
            return await Get(expected);
        });
        for (var i = 0; i < 2; i++) {
            try {
                await subject.Ready(CancellationToken.None);
            }
            catch (InvalidOperationException) {
            }
        }
        var actual = await subject.Ready(CancellationToken.None);
        Assert.That(actual, Is.SameAs(expected));
    }

    [Test]
    public async Task Ready_DoesNotCacheResultOfTaskStartedBeforeRefresh() {
        var n = 0;
        var gate = new TaskCompletionSource<object>();
        var expected = new[] { new object(), new object() };
        var subject = new TaskCache<object>.WithRefresh(async _ => {
            var result = expected[n++];
            if (result == expected[0]) {
                await gate.Task;
            }
            return result;
        });
        var stale = subject.Ready(CancellationToken.None);
        await subject.Refresh(CancellationToken.None);
        gate.SetResult(null);
        await stale;
        var actual = await subject.Ready(CancellationToken.None);
        Assert.That(actual, Is.SameAs(expected[1]));
    }

    [Test]
    public async Task Ready_CancelsTheUnderlyingOperationForAllCallers() {
        var gate = new TaskCompletionSource<object>();
        var subject = new TaskCache<object>(async token => {
            await gate.Task;
            token.ThrowIfCancellationRequested();
            return new object();
        });
        using (var tokenSource = new CancellationTokenSource()) {
            var first = subject.Ready(tokenSource.Token);
            var second = subject.Ready(CancellationToken.None);
            tokenSource.Cancel();
            gate.SetResult(null);
            try {
                await first;
            }
            catch (OperationCanceledException) {
            }
            Assert.That(async () => await second, Throws.InstanceOf<OperationCanceledException>());
        }
    }

    [Test]
    public async Task Ready_CallsFactoryAgainAfterCancellation() {
        var expected = new object();
        var subject = new TaskCache<object>(async token => {
            await Get(new object());
            token.ThrowIfCancellationRequested();
            return expected;
        });
        using (var tokenSource = new CancellationTokenSource()) {
            tokenSource.Cancel();
            try {
                await subject.Ready(tokenSource.Token);
            }
            catch (OperationCanceledException) {
            }
        }
        var actual = await subject.Ready(CancellationToken.None);
        Assert.That(actual, Is.SameAs(expected));
    }
}
