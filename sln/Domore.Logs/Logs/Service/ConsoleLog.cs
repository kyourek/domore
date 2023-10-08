using System;

namespace Domore.Logs.Service {
    internal sealed class ConsoleLog : ILogService {
        public void Log(string name, string data, LogSeverity severity) {
            Console.WriteLine(data);
        }
    }
}
