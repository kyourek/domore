using System.Diagnostics;

namespace Domore.Logs.Service {
    internal sealed class TraceLog : ILogService {
        public void Log(string name, string data, LogSeverity severity) {
            Trace.WriteLine(data, name);
        }
    }
}
