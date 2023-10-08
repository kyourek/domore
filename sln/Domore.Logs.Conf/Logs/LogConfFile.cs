using Domore.Conf;
using System;

namespace Domore.Logs {
    internal sealed class LogConfFile : IDisposable {
        private readonly ConfFile Agent;

        private void Dispose(bool disposing) {
            if (disposing) {
                Agent.Dispose();
            }
        }

        public LogConfFile(string path) {
            Agent = new ConfFile(path, key: "", target: Logging.Config);
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
}
