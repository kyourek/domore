namespace Domore.Logs; 
/// <summary>
/// A provider of log services.
/// </summary>
public interface ILogService {
    /// <summary>
    /// Logs a message.
    /// </summary>
    /// <param name="name">The log name.</param>
    /// <param name="data">The log message data.</param>
    /// <param name="severity">The log severity</param>
    void Log(string name, string data, LogSeverity severity);

    /// <summary>
    /// Called when the services are complete.
    /// </summary>
    void Complete();
}
