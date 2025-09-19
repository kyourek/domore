namespace Domore.Logs; 
/// <summary>
/// Logging methods.
/// </summary>
public interface ILog {
    /// <summary>
    /// Determines if messages at the specified <paramref name="severity"/> will be logged.
    /// </summary>
    /// <param name="severity">The severity of the message.</param>
    /// <returns>True if messages of the specified <paramref name="severity"/> will be logged. Otherwise, false.</returns>
    bool Enabled(LogSeverity severity);

    /// <summary>
    /// Logs a message at the specified <paramref name="severity"/>.
    /// </summary>
    /// <param name="severity">The severity of the log.</param>
    /// <param name="data">The log message data.</param>
    void Data(LogSeverity severity, params object[] data);

    /// <summary>
    /// Determines if messages of with a severity of <see cref="LogSeverity.Critical"/> will be logged.
    /// </summary>
    /// <returns>True if messages with <see cref="LogSeverity.Critical"/> severity will be logged. Otherwise, false.</returns>
    bool Critical();

    /// <summary>
    /// Logs a message with a severity of <see cref="LogSeverity.Critical"/>
    /// </summary>
    /// <param name="data">The log message data.</param>
    void Critical(params object[] data);

    /// <summary>
    /// Determines if messages of with a severity of <see cref="LogSeverity.Info"/> will be logged.
    /// </summary>
    /// <returns>True if messages with <see cref="LogSeverity.Info"/> severity will be logged. Otherwise, false.</returns>
    bool Info();

    /// <summary>
    /// Logs a message with a severity of <see cref="LogSeverity.Info"/>
    /// </summary>
    /// <param name="data">The log message data.</param>
    void Info(params object[] data);

    /// <summary>
    /// Determines if messages of with a severity of <see cref="LogSeverity.Error"/> will be logged.
    /// </summary>
    /// <returns>True if messages with <see cref="LogSeverity.Error"/> severity will be logged. Otherwise, false.</returns>
    bool Error();

    /// <summary>
    /// Logs a message with a severity of <see cref="LogSeverity.Error"/>
    /// </summary>
    /// <param name="data">The log message data.</param>
    void Error(params object[] data);

    /// <summary>
    /// Determines if messages of with a severity of <see cref="LogSeverity.Debug"/> will be logged.
    /// </summary>
    /// <returns>True if messages with <see cref="LogSeverity.Debug"/> severity will be logged. Otherwise, false.</returns>
    bool Debug();

    /// <summary>
    /// Logs a message with a severity of <see cref="LogSeverity.Debug"/>
    /// </summary>
    /// <param name="data">The log message data.</param>
    void Debug(params object[] data);

    /// <summary>
    /// Determines if messages of with a severity of <see cref="LogSeverity.Warn"/> will be logged.
    /// </summary>
    /// <returns>True if messages with <see cref="LogSeverity.Warn"/> severity will be logged. Otherwise, false.</returns>
    bool Warn();

    /// <summary>
    /// Logs a message with a severity of <see cref="LogSeverity.Warn"/>
    /// </summary>
    /// <param name="data">The log message data.</param>
    void Warn(params object[] data);
}
