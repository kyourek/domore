using System.Diagnostics;

namespace Domore.Logs.Service {
    internal sealed class TraceLog : ILogService {
        void ILogService.Log(string name, string data, LogSeverity severity) {
            Trace.WriteLine(data, name);
        }

        void ILogService.Complete() {
        }
    }
}
