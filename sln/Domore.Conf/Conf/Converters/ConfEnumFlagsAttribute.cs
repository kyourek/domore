using System;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Conf.Converters {
    using Extensions;

    public sealed class ConfEnumFlagsAttribute : ConfConverterAttribute {
        internal sealed override ConfValueConverter ConverterInstance =>
            _ConverterInstance ?? (
            _ConverterInstance = new ValueConverter { Separators = Separators });
        private ConfValueConverter _ConverterInstance;

        public string Separators {
            get => _Separators;
            set {
                if (_Separators != value) {
                    _Separators = value;
                    _ConverterInstance = null;
                }
            }
        }
        private string _Separators;

        private sealed class ValueConverter : ConfValueConverter.Internal {
            private static readonly string DefaultSeparators = "+|&,/\\";
            private static readonly Dictionary<Type, Dictionary<string, HashSet<string>>> AliasCache = new Dictionary<Type, Dictionary<string, HashSet<string>>>();

            protected sealed override object Convert(bool @internal, string value, ConfValueConverterState state) {
                if (null == value) throw new ArgumentNullException(nameof(value));
                if (null == state) throw new ArgumentNullException(nameof(state));
                var prop = state.Property.PropertyType;
                var type = Nullable.GetUnderlyingType(prop) ?? prop;
                var aliasCache = AliasCache;
                if (aliasCache.TryGetValue(type, out var alias) == false) {
                    aliasCache[type] = alias = type
                        .GetEnumAlias()
                        .ToDictionary(pair => pair.Key.Name, pair => pair.Value);
                }
                var separators = Separators;
                if (separators == null || separators.Length == 0) {
                    separators = DefaultSeparators;
                }
                foreach (var c in separators) {
                    if (value.Contains(c)) {
                        var items = value
                            .Split(c)
                            .Select(s => s?.Trim() ?? "")
                            .Where(s => s != "")
                            .Select(s => alias.FirstOrDefault(pair => pair.Value.Contains(s)).Key ?? s);
                        var parsableString = string.Join(",", items);
                        var parseResult = Enum.Parse(type, parsableString, ignoreCase: true);
                        return parseResult;
                    }
                }
                value = alias.FirstOrDefault(pair => pair.Value.Contains(value)).Key ?? value;
                return Enum.Parse(type, value, ignoreCase: true);
            }

            public string Separators { get; set; }
        }
    }
}
