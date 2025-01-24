using System.Diagnostics;

namespace Domore.Logs.Service {
    internal sealed class DebugLog : ILogService {
        void ILogService.Log(string name, string data, LogSeverity severity) {
            Debug.WriteLine(data, name);
        }

        void ILogService.Complete() {
        }
    }
}
