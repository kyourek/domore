using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Domore.Conf.Cli;

internal sealed class TargetDescription {
    private IEnumerable<TargetPropertyDescription> DisplayedProperties => field ??=
        Properties
            .Where(p => p.DisplayAttribute.Include ?? DisplayDefault)
            .OrderBy(p => p.ArgumentOrder > -1 ? p.ArgumentOrder : int.MaxValue)
            .ToList();

    private CliSetup Setup { get; }

    private TargetDescription(Type targetType, CliSetup setup) {
        TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        Setup = setup ?? throw new ArgumentNullException(nameof(setup));
    }

    private T Attribute<T>() {
        return TargetType
            .GetCustomAttributes(typeof(T), inherit: true)
            .OfType<T>()
            .FirstOrDefault();
    }

    public Type TargetType { get; }

    public ConfHelpAttribute ConfHelpAttribute => field ??=
        (Attribute<ConfHelpAttribute>() ?? new ConfHelpAttribute(null));

    public IEnumerable<TargetPropertyDescription> Properties => field ??=
        TargetType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(propertyInfo => propertyInfo.CanRead && propertyInfo.CanWrite)
            .Select(propertyInfo => new TargetPropertyDescription(propertyInfo))
            .ToList();

    public string Example => field ??= new Func<string>(() => {
        var examples = TargetType
            .GetCustomAttributes(typeof(CliExampleAttribute), inherit: true)
            .OfType<CliExampleAttribute>()
            .Select(attribute => attribute.Format(CommandInvoke));
        return string.Join(Environment.NewLine + Environment.NewLine, examples).Trim();
    })();

    public string CommandName => field ??= (Setup.CommandName(TargetType) ?? TargetType.Name.ToLowerInvariant());
    public string CommandSpace => field ??= (Setup.CommandSpace(TargetType) ?? "");

    public string CommandInvoke => field ??= new Func<string>(() => {
        var name = CommandName;
        var space = CommandSpace;
        var invoke = string.IsNullOrWhiteSpace(space) ? name : $"{space} {name}";
        return invoke;
    })();

    public bool DisplayDefault => _DisplayDefault ??= (
        Properties.Any(p => p.DisplayAttribute.Include == true) ? false :
        Properties.Any(p => p.DisplayAttribute.Include == false) ? true :
        true);
    private bool? _DisplayDefault;

    public string Display => field ??=
        string.Join(" ", new[] { CommandName }.Concat(DisplayedProperties.Select(p => p.Display)));

    public string Manual => field ??= new Func<string>(() => {
        var display = Display;
        var properties = DisplayedProperties;
        if (properties.Any() == false) {
            return display;
        }
        var help = ConfHelpAttribute.Format("    ");
        var propertyWidth = properties.Max(p => p?.DisplayName?.Length ?? 0);
        var propertyManuals = properties
            .Select(p => p.Manual(propertyWidth))
            .Where(manual => !string.IsNullOrWhiteSpace(manual));
        var manual = string.Join(Environment.NewLine + Environment.NewLine, new[] { display }
            .Concat(string.IsNullOrWhiteSpace(help) ? [] : [help])
            .Concat(propertyManuals)
            .Concat(string.IsNullOrWhiteSpace(Example) ? [] : [Example]));
        return manual;
    })();

    public IEnumerable<TargetMethodValidation> Validations => field ??=
        TargetType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(method => new {
                Method = method,
                Attribute = method
                    .GetCustomAttributes(typeof(CliValidationAttribute), inherit: true)
                    .OfType<CliValidationAttribute>()
                    .FirstOrDefault()
            })
            .Where(item => item.Attribute != null)
            .OrderBy(item => item.Attribute.Order)
            .Select(item => new TargetMethodValidation(item.Method, item.Attribute))
            .ToList();

    public IEnumerable<string> Conf(string cli) {
        var properties = Properties;
        var req = properties.Where(p => p.Required).ToList();
        var arg = properties
            .Where(p => p.ArgumentOrder >= 0)
            .OrderBy(p => p.ArgumentOrder)
            .ToList();
        var argi = 0;
        var args = properties.Where(p => p.ArgumentList).ToList();
        var sets = properties.Where(p => p.ParameterSet).ToList();
        void keyed(string k) {
            if (req.Count > 0) {
                req.RemoveAll(p => p.AllNames.Contains(k, StringComparer.OrdinalIgnoreCase));
            }
        }
        foreach (var token in Token.Parse(cli)) {
            var key = token.Key?.Trim();
            var val = token.Value?.Trim();
            if (string.IsNullOrEmpty(val) && string.IsNullOrEmpty(key)) {
                continue;
            }
            if (val != null) {
                foreach (var set in sets) {
                    var setNam = set.DisplayName;
                    var setKey = setNam + "[" + key + "]";
                    var setVal = setKey + " = " + val;
                    yield return setVal;
                }
                yield return key + " = " + val;
                keyed(key);
            }
            else {
                if (key.Equals(CommandName, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                var argTaken = false;
                if (arg.Count > 0) {
                    var argKey = arg[0].ArgumentName;
                    var argVal = key;
                    yield return argKey + " = " + argVal;
                    keyed(argKey);
                    arg.RemoveAt(0);
                    argTaken = true;
                }
                if (args.Count > 0) {
                    foreach (var list in args) {
                        var argNam = list.DisplayName;
                        var argKey = argNam + "[" + argi + "]";
                        var argVal = key;
                        yield return argKey + " = " + argVal;
                        keyed(argNam);
                    }
                    argi++;
                    argTaken = true;
                }
                if (argTaken == false) {
                    throw new CliArgumentNotFoundException(key);
                }
            }
        }
        if (req.Count > 0) {
            throw new CliRequiredNotFoundException(req);
        }
    }
    internal sealed class Cache {
        private readonly Dictionary<Type, TargetDescription> Agent = [];

        public CliSetup Setup { get; }

        public Cache(CliSetup setup) {
            Setup = setup;
        }

        public TargetDescription Describe(Type targetType) {
            lock (Agent) {
                if (Agent.TryGetValue(targetType, out var targetDescription) == false) {
                    Agent[targetType] = targetDescription = new TargetDescription(targetType, Setup);
                }
                return targetDescription;
            }
        }

        public void Clear() {
            lock (Agent) {
                Agent.Clear();
            }
        }
    }
}
