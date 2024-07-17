using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Domore.Conf.Cli {
    internal sealed class TargetDescription {
        private readonly Dictionary<CliDisplayOptions, string> DisplayCache = new();
        private readonly Dictionary<CliDisplayOptions, string> ManualCache = new();

        private static readonly Dictionary<Type, TargetDescription> Cache = [];

        private IEnumerable<TargetPropertyDescription> DisplayedProperties => _DisplayedProperties ??=
            Properties
                .Where(p => p.DisplayAttribute.Include ?? DisplayDefault)
                .OrderBy(p => p.ArgumentOrder > -1 ? p.ArgumentOrder : int.MaxValue)
                .ToList();
        private IEnumerable<TargetPropertyDescription> _DisplayedProperties;

        private TargetDescription(Type targetType) {
            TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        }

        public Type TargetType { get; }

        public IEnumerable<TargetPropertyDescription> Properties => _Properties ??=
            TargetType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(propertyInfo => propertyInfo.CanRead && propertyInfo.CanWrite)
                .Select(propertyInfo => new TargetPropertyDescription(propertyInfo))
                .ToList();
        private IEnumerable<TargetPropertyDescription> _Properties;

        public string Example => _Example ??= new Func<string>(() => {
            var examples = TargetType
                .GetCustomAttributes(typeof(CliExampleAttribute), inherit: true)
                .OfType<CliExampleAttribute>()
                .Select(attribute => attribute.Format(CommandInvoke));
            return string.Join(Environment.NewLine, examples).Trim();
        })();
        private string _Example;

        public string CommandName => _CommandName ??= (Cli.Setup().CommandName(TargetType) ?? TargetType.Name.ToLowerInvariant());
        private string _CommandName;

        public string CommandSpace => _CommandSpace ??= (Cli.Setup().CommandSpace(TargetType) ?? "");
        private string _CommandSpace;

        public string CommandInvoke => _CommandInvoke ??= new Func<string>(() => {
            var name = CommandName;
            var space = CommandSpace;
            var invoke = string.IsNullOrWhiteSpace(space) ? name : $"{space} {name}";
            return invoke;
        })();
        private string _CommandInvoke;

        public bool DisplayDefault => _DisplayDefault ??= (
            Properties.Any(p => p.DisplayAttribute.Include == true) ? false :
            Properties.Any(p => p.DisplayAttribute.Include == false) ? true :
            true);
        private bool? _DisplayDefault;

        public IEnumerable<TargetMethodValidation> Validations => _Validations ??=
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
        private IEnumerable<TargetMethodValidation> _Validations;

        public static TargetDescription Describe(Type targetType) {
            lock (Cache) {
                if (Cache.TryGetValue(targetType, out var targetDescription) == false) {
                    Cache[targetType] = targetDescription = new TargetDescription(targetType);
                }
                return targetDescription;
            }
        }

        public static void Clear() {
            lock (Cache) {
                Cache.Clear();
            }
        }

        public string Display(CliDisplayOptions options) {
            lock (DisplayCache) {
                if (DisplayCache.TryGetValue(options, out var value) == false) {
                    DisplayCache[options] = value = new Func<string>(() => {
                        var propertyDisplays = DisplayedProperties.Select(p => p.Display);
                        var propertyDisplay = string.Join(" ", propertyDisplays);
                        if (options.HasFlag(CliDisplayOptions.SkipCommandName)) {
                            return propertyDisplay;
                        }
                        return string.Join(" ", CommandName, propertyDisplay);
                    })();
                }
                return value;
            }
        }

        public string Manual(CliDisplayOptions options) {
            lock (ManualCache) {
                if (ManualCache.TryGetValue(options, out var value) == false) {
                    ManualCache[options] = value = new Func<string>(() => {
                        var display = Display(options);
                        var properties = DisplayedProperties;
                        if (properties.Any() == false) {
                            return display;
                        }
                        var propertyWidth = properties.Max(p => p?.DisplayName?.Length ?? 0);
                        var propertyManuals = properties
                            .Select(p => p.Manual(propertyWidth))
                            .Where(manual => !string.IsNullOrWhiteSpace(manual));
                        var manual = string.Join(Environment.NewLine, new[] { display }
                            .Concat(propertyManuals)
                            .Concat(string.IsNullOrWhiteSpace(Example) ? [] : [Example]));
                        return manual;
                    })();
                }
                return value;
            }
        }

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
    }
}
