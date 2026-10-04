using Domore.Logs.Service;
using NUnit.Framework;
using System;
using System.Reflection;
using CONF = Domore.Conf.Conf;

namespace Domore.Logs;
[TestFixture]
public sealed class LogServiceProxyTest {
    [Test]
    public void CompleteDoesNotCreateServiceThatWasNeverUsed() {
        var proxy = new LogServiceProxy("unused", _ => { });
        var serviceField = typeof(LogServiceProxy).GetField(
            "_Service",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(serviceField, Is.Not.Null);
        Assert.That(serviceField.GetValue(proxy), Is.Null);

        proxy.Complete();

        Assert.That(serviceField.GetValue(proxy), Is.Null);
    }

    [Test]
    public void CompleteDoesNotPropagateServiceException() {
        var proxy = new LogServiceProxy("throwing", _ => { }) {
            Type = typeof(ThrowingCompleteService).AssemblyQualifiedName
        };
        Assert.That(proxy.Service, Is.InstanceOf<ThrowingCompleteService>());

        Assert.DoesNotThrow(proxy.Complete);
    }

    [Test]
    public void ConfigurationAppliesServicePropertiesSetBeforeType() {
        using var manager = new LogManager();
        var target = new { Log = manager };
        CONF.Contain(@"
log[f].service.name = configured-before-type.log
log[f].service.directory = configured-before-type
log[f].type = file
").Configure(target, key: "");

        var service = (FileLog)manager["f"].Service;

        Assert.Multiple(() => {
            Assert.That(service.Name, Is.EqualTo("configured-before-type.log"));
            Assert.That(service.Directory, Is.EqualTo("configured-before-type"));
        });
    }

    private sealed class ThrowingCompleteService : ILogService {
        public ThrowingCompleteService() {
        }

        public void Log(string name, string data, LogSeverity severity) {
        }

        public void Complete() {
            throw new InvalidOperationException("completion failed");
        }
    }
}
