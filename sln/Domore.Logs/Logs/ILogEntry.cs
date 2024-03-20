using System;
using System.Collections.Generic;

namespace Domore.Logs {
    public interface ILogEntry {
        Type LogType { get; }
        string LogName { get; }
        DateTime LogDate { get; }
        LogSeverity LogSeverity { get; }
        IEnumerable<string> LogList { get; }
    }
}
    