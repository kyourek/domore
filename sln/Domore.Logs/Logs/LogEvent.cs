using System;
using System.Collections.Generic;

namespace Domore.Logs {
    public delegate void LogEventHandler(object sender, LogEventArgs e);

    public sealed class LogEventArgs : EventArgs, ILogEntry {
        internal ILogEntry Agent { get; }

        internal LogEventArgs(ILogEntry agent) {
            Agent = agent ?? throw new ArgumentNullException(nameof(agent));
        }

        public Type LogType => Agent.LogType;
        public string LogName => Agent.LogName;
        public DateTime LogDate => Agent.LogDate;
        public LogSeverity LogSeverity => Agent.LogSeverity;
        public IEnumerable<string> LogList => Agent.LogList;
    }
}
