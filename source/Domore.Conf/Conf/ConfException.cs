using System;

namespace Domore.Conf;

/// <summary>
/// Base of exceptions thrown in the conf framework.
/// </summary>
public class ConfException : Exception {
    /// <summary>
    /// Creates a new conf exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception, if one exists.</param>
    public ConfException(string message, Exception innerException) : base(message, innerException) {
    }
}
