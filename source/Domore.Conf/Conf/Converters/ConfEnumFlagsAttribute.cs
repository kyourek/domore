using System;

namespace Domore.Conf.Converters {
    /// <summary>
    /// Converts conf content to enum flags.
    /// </summary>
    public sealed class ConfEnumFlagsAttribute : ConfConverterAttribute {
        internal sealed override ConfValueConverter ConverterInstance =>
            _ConverterInstance ?? (
            _ConverterInstance = new ValueConverter { Separators = Separators });
        private ConfValueConverter _ConverterInstance;

        /// <summary>
        /// Gets or sets the string separators between flags.
        /// </summary>
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
            private readonly ConfEnumFlagsConverter Agent = new ConfEnumFlagsConverter();

            protected sealed override object Convert(bool @internal, string value, ConfValueConverterState state) {
                if (null == state) throw new ArgumentNullException(nameof(state));
                return Agent.Convert(value, state.Property.PropertyType);
            }

            public string Separators {
                get => Agent.Separators;
                set => Agent.Separators = value;
            }
        }
    }
}
