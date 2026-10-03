using Domore.Conf;
using System;

namespace Domore.Logs;

internal sealed class LogConfFile : IDisposable {
    private sealed class Target {
        [ConfPopulatedCallback]
        private void Populate(IConf conf) {
            // This callback runs for explicit and watcher-driven applications.
            // Resolve and lease one manager for every pair in this file.
            Logging.Configure(target => conf.Configure(target, key: ""));
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
