using Domore.Conf;
using Domore.Conf.Logs;
using System;
using System.IO;

namespace Domore.Logs;

internal sealed class LogConfFile : IDisposable {
    private sealed class Target {
        [ConfPopulatedCallback]
        private void Populate(IConf conf) {
            // This callback runs for explicit and watcher-driven applications.
            // Resolve and lease one manager for every pair in this file.
            Logging.Configure(target => LogConfPopulation.Configure(conf, target));
        }
    }

    private readonly ConfFile Agent;

    private void Dispose(bool disposing) {
        if (disposing) {
            Agent.ConfigureError -= Agent_ConfigureError;
            Agent.WatchError -= Agent_WatchError;
            Agent.Dispose();
        }
    }

    private void Agent_ConfigureError(object sender, ErrorEventArgs e) =>
        Logging.Notify(e?.GetException());

    private void Agent_WatchError(object sender, ErrorEventArgs e) =>
        Logging.Notify(e?.GetException());

    public LogConfFile(string path) {
        Agent = new ConfFile(path, key: "", target: new Target());
        Agent.ConfigureError += Agent_ConfigureError;
        Agent.WatchError += Agent_WatchError;
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
