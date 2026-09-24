using Domore.Conf.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace Domore.Conf.Cli;

internal sealed class TargetPropertyDescription {
    private readonly Dictionary<int, string> Manuals = new(capacity: 1);

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

    private string ManualFactory(int propertyWidth) {
        var help = ConfHelpAttribute;
        var text = help.Format("");
        var empty = string.IsNullOrWhiteSpace(text);
        if (empty) {
            return "";
        }
        var name = "    " + DisplayName.PadRight(propertyWidth) + "    ";
        var space = new string(name.Select(_ => ' ').ToArray());
        var lines = text
            .Split('\n')
            .Select(line => line.Replace("\r", ""))
            .Select((line, i) => i == 0 ? $"{name}{line}" : $"{space}{line}");
        return string.Join(Environment.NewLine, lines);
    }

    public bool Required => _Required ??= (Attribute<CliRequiredAttribute>() != null);
    private bool? _Required;

    public int ArgumentOrder => _ArgumentOrder ??= (Attribute<CliArgumentAttribute>()?.Order ?? -1);
    private int? _ArgumentOrder;

    public string ArgumentName => field ??= (
        ArgumentOrder < 0
            ? ""
            : DisplayName);

    public bool ArgumentList => _ArgumentList ??= (Attribute<CliArgumentsAttribute>() != null);
    private bool? _ArgumentList;

    public bool ParameterSet => _ParameterSet ??= (Attribute<CliParametersAttribute>() != null);
    private bool? _ParameterSet;

    public string PropertyName => field ??= PropertyInfo.Name;

    public Type PropertyType => field ??= PropertyInfo.PropertyType;

    public ReadOnlyCollection<string> ConfNames => field ??=
        new ReadOnlyCollection<string>([.. PropertyInfo
            .GetCustomAttributes(typeof(ConfAttribute), inherit: true)
            .OfType<ConfAttribute>()
            .SelectMany(attribute => attribute.Names)]);

    public ReadOnlyCollection<string> AllNames => field ??=
        new ReadOnlyCollection<string>([PropertyName, .. ConfNames]);

    public CliDisplayAttribute DisplayAttribute => field ??=
        (Attribute<CliDisplayAttribute>() ?? new CliDisplayAttribute());

    public CliDisplayOverrideAttribute DisplayOverrideAttribute => field ??=
        (Attribute<CliDisplayOverrideAttribute>() ?? new CliDisplayOverrideAttribute());

    public ConfListItemsAttribute ConfListItemsAttribute => field ??=
        (Attribute<ConfListItemsAttribute>() ?? new ConfListItemsAttribute());

    public ConfHelpAttribute ConfHelpAttribute => field ??=
        (Attribute<ConfHelpAttribute>() ?? new ConfHelpAttribute(null));

    public string DisplayName => field ??= (ConfNames.FirstOrDefault() ?? PropertyName.ToLowerInvariant());
    public string DisplayKind => field ??= (TargetPropertyKind.For(this) ?? PropertyType.Name.ToLowerInvariant());
    public string Display => field ??= (DisplayOverrideAttribute.Display ?? DisplayFactory());

    public PropertyInfo PropertyInfo { get; }

    public TargetPropertyDescription(PropertyInfo propertyInfo) {
        PropertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
    }

    public string Manual(int propertyWidth) {
        lock (Manuals) {
            if (Manuals.TryGetValue(propertyWidth, out var manual) == false) {
                Manuals[propertyWidth] = manual = ManualFactory(propertyWidth);
            }
            return manual;
        }
    }
}
