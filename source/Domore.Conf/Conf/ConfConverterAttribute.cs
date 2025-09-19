using System;

namespace Domore.Conf;

/// <summary>
/// Base decorator of members to be populated by conf content.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class ConfConverterAttribute : Attribute {
    internal virtual ConfValueConverter ConverterInstance { get; }

    internal ConfConverterAttribute() {
    }

    /// <summary>
    /// Gets the type of the converter.
    /// </summary>
    public Type ConverterType { get; }

    /// <summary>
    /// Specifies a converter type for a member.
    /// </summary>
    /// <param name="converterType">The type of the converter.</param>
    public ConfConverterAttribute(Type converterType) {
        ConverterType = converterType;
    }
}
