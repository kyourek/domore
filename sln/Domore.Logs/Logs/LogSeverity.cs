namespace Domore.Logs {
    /// <summary>
    /// Log message severity
    /// </summary>
    public enum LogSeverity {
        /// <summary>
        /// No severity
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Debug severity
        /// </summary>
        Debug = 1,

        /// <summary>
        /// Informational severity
        /// </summary>
        Info = 2,

        /// <summary>
        /// Warning severity
        /// </summary>
        Warn = 3,

        /// <summary>
        /// Error severity
        /// </summary>
        Error = 4,

        /// <summary>
        /// Critical severity
        /// </summary>
        Critical = 5
    }
}
