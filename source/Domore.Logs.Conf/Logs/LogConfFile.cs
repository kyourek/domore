using Domore.Conf;
using System;

namespace Domore.Logs;

internal sealed class LogConfFile : IDisposable {
    private sealed class Target {
        public object Log {
            get {
                var config = Logging.Config;
                var property = config.GetType().GetProperty(nameof(Log));
                return property?.GetValue(config, null) ??
                    throw new InvalidOperationException("The logging configuration has no Log target.");
            }
        }
    }

    private readonly ConfFile Agent;

    private void Dispose(bool disposing) {
        if (disposing) {
            Agent.Dispose();
        }
    }

    public LogConfFile(string path) {
        Agent = new ConfFile(path, key: "", target: new Target());
    }

    public void Configure(bool? watch = null) {
        Agent.Configure(watch);
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~LogConfFile() {
        Dispose(false);
    }
}
