using System;

namespace Domore.Conf;

/// <summary>
/// The exception thrown when an error occurs during value conversion.
/// </summary>
public sealed class ConfValueConverterException : ConfException {
    private static string GetMessage(ConfValueConverter converter, string value, ConfValueConverterState state, Exception innerException) {
        var expectedType = state?.Property?.PropertyType;
        var expectedTypeName = expectedType?.Name?.ToLowerInvariant();
        return $"Invalid value: {value} (Expected one of <{expectedTypeName}>)";
    }

    private ConfValueConverterException(ConfValueConverter converter, string value, ConfValueConverterState state, string message, Exception innerException) : base(message, innerException) {
        Value = value;
        State = state;
        Converter = converter;
    }

    internal ConfValueConverterException(ConfValueConverter converter, string value, ConfValueConverterState state, Exception innerException)
        : this(converter, value, state, GetMessage(converter, value, state, innerException), innerException) {
    }

    internal ConfValueConverterException(ConfValueConverter converter, string value, ConfValueConverterState state, string message)
        : this(converter, value, state, message, innerException: null) {
    }

    /// <summary>
    /// Gets the original value that should have been converted.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the instance of <see cref="ConfValueConverter"/> that threw the exception.
    /// </summary>
    public ConfValueConverter Converter { get; }

    /// <summary>
    /// Gets the state object relevant at the time of the exception.
    /// </summary>
    public ConfValueConverterState State { get; }
}
