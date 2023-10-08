using Domore.Conf.Converters;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace Domore.Conf.Cli {
    internal sealed class TargetPropertyDescription {
        private T Attribute<T>() {
            return PropertyInfo
                .GetCustomAttributes(typeof(T), inherit: true)
                .OfType<T>()
                .FirstOrDefault();
        }

        private string DisplayFactory() {
            var required = Required;
            var propertyType = PropertyType;
            var argumentList = ArgumentList;
            if (argumentList) {
                var arg = '<' + DisplayName + '>';
                return required
                    ? arg
                    : ('[' + arg + ']');
            }
            var argumentName = ArgumentName;
            if (argumentName != "" && (propertyType == typeof(string) || propertyType == typeof(object))) {
                var arg = '<' + argumentName + '>';
                return required
                    ? arg
                    : ('[' + arg + ']');
            }
            var key = argumentName;
            if (key == "") {
                key = DisplayName + '=';
            }
            var val = '<' + DisplayKind + '>';
            var pair = key + val;
            return required
                ? pair
                : ('[' + pair + ']');
        }

        public bool Required =>
            _Required ?? (
            _Required = Attribute<CliRequiredAttribute>() != null).Value;
        private bool? _Required;

        public int ArgumentOrder =>
            _ArgumentOrder ?? (
            _ArgumentOrder = Attribute<CliArgumentAttribute>()?.Order ?? -1).Value;
        private int? _ArgumentOrder;

        public string ArgumentName =>
            _ArgumentName ?? (
            _ArgumentName = ArgumentOrder < 0
                ? ""
                : DisplayName);
        private string _ArgumentName;

        public bool ArgumentList =>
            _ArgumentList ?? (
            _ArgumentList = Attribute<CliArgumentsAttribute>() != null).Value;
        private bool? _ArgumentList;

        public bool ParameterSet =>
            _ParameterSet ?? (
            _ParameterSet = Attribute<CliParametersAttribute>() != null).Value;
        private bool? _ParameterSet;

        public string PropertyName =>
            _PropertyName ?? (
            _PropertyName = PropertyInfo.Name);
        private string _PropertyName;

        public Type PropertyType =>
            _PropertyType ?? (
            _PropertyType = PropertyInfo.PropertyType);
        private Type _PropertyType;

        public ReadOnlyCollection<string> ConfNames =>
            _ConfNames ?? (
            _ConfNames = new ReadOnlyCollection<string>(PropertyInfo
                .GetCustomAttributes(typeof(ConfAttribute), inherit: true)
                .OfType<ConfAttribute>()
                .SelectMany(attribute => attribute.Names)
                .ToList()));
        private ReadOnlyCollection<string> _ConfNames;

        public ReadOnlyCollection<string> AllNames =>
            _AllNames ?? (
            _AllNames = new ReadOnlyCollection<string>(new[] { PropertyName }
                .Concat(ConfNames)
                .ToList()));
        private ReadOnlyCollection<string> _AllNames;

        public CliDisplayAttribute DisplayAttribute =>
            _DisplayAttribute ?? (
            _DisplayAttribute = Attribute<CliDisplayAttribute>() ?? new CliDisplayAttribute());
        private CliDisplayAttribute _DisplayAttribute;

        public CliDisplayOverrideAttribute DisplayOverrideAttribute =>
            _DisplayOverrideAttribute ?? (
            _DisplayOverrideAttribute = Attribute<CliDisplayOverrideAttribute>() ?? new CliDisplayOverrideAttribute());
        private CliDisplayOverrideAttribute _DisplayOverrideAttribute;

        public ConfListItemsAttribute ConfListItemsAttribute =>
            _ConfListItemsAttribute ?? (
            _ConfListItemsAttribute = Attribute<ConfListItemsAttribute>() ?? new ConfListItemsAttribute());
        private ConfListItemsAttribute _ConfListItemsAttribute;

        public string DisplayName =>
            _DisplayName ?? (
            _DisplayName = ConfNames.FirstOrDefault() ?? PropertyName.ToLowerInvariant());
        private string _DisplayName;

        public string DisplayKind =>
            _DisplayKind ?? (
            _DisplayKind = TargetPropertyKind.For(this) ?? PropertyType.Name.ToLowerInvariant());
        private string _DisplayKind;

        public string Display =>
            _Display ?? (
            _Display = DisplayOverrideAttribute.Display ?? DisplayFactory());
        private string _Display;

        public PropertyInfo PropertyInfo { get; }

        public TargetPropertyDescription(PropertyInfo propertyInfo) {
            PropertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
        }
    }
}
