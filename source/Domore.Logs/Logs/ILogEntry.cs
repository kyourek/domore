using System;
using System.Collections.Generic;

namespace Domore.Logs; 
/// <summary>
/// A log message entry.
/// </summary>
public interface ILogEntry {
    /// <summary>
    /// Gets the log type.
    /// </summary>
    Type LogType { get; }

    /// <summary>
    /// Gets the log name.
    /// </summary>
    string LogName { get; }

    /// <summary>
    /// Gets the log date.
    /// </summary>
    DateTime LogDate { get; }

    /// <summary>
    /// Gets the log severity.
    /// </summary>
    LogSeverity LogSeverity { get; }

    /// <summary>
    /// Gets the log message list.
    /// </summary>
    IEnumerable<string> LogList { get; }
}
