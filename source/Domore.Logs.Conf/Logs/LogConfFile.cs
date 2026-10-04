using Domore.Conf;
using System;
using System.IO;

namespace Domore.Logs; 
internal sealed class LogConfFile : IDisposable {
    private readonly ConfFile Agent;

    private void Agent_Error(object sender, ErrorEventArgs e) {
        Logging.Notify(e.GetException());
    }

    public LogConfFile(string path) {
        Agent = new ConfFile(path, key: "", target: Logging.Config);
        Agent.ConfigureError += Agent_Error;
        Agent.WatchError += Agent_Error;
    }

    public void Configure(bool? watch = null) {
        Agent.Configure(watch);
    }

    public void Dispose() {
        Agent.ConfigureError -= Agent_Error;
        Agent.WatchError -= Agent_Error;
        Agent.Dispose();
    }
}
