using System;
using System.Collections.Generic;

namespace Domore.Logs; 
/// <summary>
/// Delegate for log events.
/// </summary>
/// <param name="sender">The object that raised the event.</param>
/// <param name="e">The event arguments.</param>
public delegate void LogEventHandler(object sender, LogEventArgs e);

/// <summary>
/// Log event arguments.
/// </summary>
public sealed class LogEventArgs : EventArgs, ILogEntry {
    internal ILogEntry Agent { get; }

    internal LogEventArgs(ILogEntry agent) {
        Agent = agent ?? throw new ArgumentNullException(nameof(agent));
    }

    /// <summary>
    /// Gets the log type.
    /// </summary>
    public Type LogType => Agent.LogType;

    /// <summary>
    /// Gets the log name.
    /// </summary>
    public string LogName => Agent.LogName;

    /// <summary>
    /// Gets the log date.
    /// </summary>
    public DateTime LogDate => Agent.LogDate;

    /// <summary>
    /// Gets the log severity.
    /// </summary>
    public LogSeverity LogSeverity => Agent.LogSeverity;

    /// <summary>
    /// Gets the log message data.
    /// </summary>
    public IEnumerable<string> LogList => Agent.LogList;
}
