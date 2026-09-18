using Domore.IO.FileSystemEventSubscriptions;
using NUnit.Framework;
using System;

namespace Domore.IO;

[TestFixture]
internal sealed class FileSystemEventResultTest {
    [Test]
    public void Properties_AreSetCorrectly() {
        var exception = new InvalidOperationException();
        var subscription = new ProxyFileSystemEventSubscription();
        var result = new FileSystemEventResult {
            Canceled = true,
            Exception = exception,
            Subscription = subscription
        };
        using (Assert.EnterMultipleScope()) {
            Assert.That(result.Canceled);
            Assert.That(result.Exception, Is.SameAs(exception));
            Assert.That(result.Subscription, Is.SameAs(subscription));
        }
    }

    [Test]
    public void Properties_DefaultToSuccessfulResult() {
        var result = new FileSystemEventResult();
        using (Assert.EnterMultipleScope()) {
            Assert.That(result.Canceled, Is.False);
            Assert.That(result.Exception, Is.Null);
            Assert.That(result.Subscription, Is.Null);
        }
    }
}
