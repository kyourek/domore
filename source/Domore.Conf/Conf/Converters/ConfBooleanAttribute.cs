using System;
using System.Collections.Generic;
using CONVERT = System.Convert;

namespace Domore.Conf.Converters;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class ConfBooleanAttribute : ConfConverterAttribute {
    private static readonly Dictionary<string, bool> Map = new(StringComparer.OrdinalIgnoreCase) {
        { "true", true },
        { "false", false },
        { "yes", true },
        { "no", false },
        { "1", true },
        { "0", false }
    };

    internal sealed override ConfValueConverter ConverterInstance => _ConverterInstance ??=
        new ValueConverter();
    private ConfValueConverter _ConverterInstance;

    private sealed class ValueConverter : ConfValueConverter.Internal {
        protected sealed override object Convert(bool @internal, string value, ConfValueConverterState state) {
            if (value is null) {
                throw new ArgumentNullException(nameof(value));
            }
            if (Map.TryGetValue(value, out var b) == false) {
                if (Map.TryGetValue(value.Trim(), out b) == false) {
                    try {
                        b = CONVERT.ToBoolean(value);
                    }
                    catch {
                        throw new ConfValueConverterException(this, value, state,
                            $"Invalid value: {value} (Expected one of <{string.Join("|", Map.Keys)}>)");
                    }
                }
            }
            return b;
        }
    }
}
