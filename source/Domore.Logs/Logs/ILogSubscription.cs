using System;

namespace Domore.Logs {
    /// <summary>
    /// A log subscription.
    /// </summary>
    public interface ILogSubscription {
        /// <summary>
        /// Raised when <see cref="Threshold(Type)"/> changes.
        /// </summary>
        event EventHandler ThresholdChanged;

        /// <summary>
        /// Returns the severity threshold of received logs.
        /// </summary>
        /// <param name="type">The type whose threshold is returned.</param>
        /// <returns>The severity threshold of received logs.</returns>
        LogSeverity Threshold(Type type);

        /// <summary>
        /// Called when a log entry is logged.
        /// </summary>
        /// <param name="entry">The log entry.</param>
        void Receive(ILogEntry entry);
    }
}
