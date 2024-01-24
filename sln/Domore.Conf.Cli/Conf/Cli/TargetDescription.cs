using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Domore.Conf.Cli {
    internal sealed class TargetDescription {
        private static readonly Dictionary<Type, TargetDescription> Cache = [];

        private TargetDescription(Type targetType) {
            TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        }

        private string DisplayFactory() {
            var propertyDisplayDefault = DisplayDefault;
            var propertyDisplays = Properties
                .Where(p => p.DisplayAttribute.Include ?? propertyDisplayDefault)
                .Select(p => p.Display);
            var properties = string.Join(" ", propertyDisplays);
            return string.Join(" ", CommandName, properties);
        }

        public Type TargetType { get; }

        public IEnumerable<TargetPropertyDescription> Properties =>
            _Properties ?? (
            _Properties = TargetType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(propertyInfo => propertyInfo.CanRead && propertyInfo.CanWrite)
                .Select(propertyInfo => new TargetPropertyDescription(propertyInfo))
                .ToList());
        private IEnumerable<TargetPropertyDescription> _Properties;

        public string CommandName =>
            _CommandName ?? (
            _CommandName = TargetType.Name.ToLowerInvariant());
        private string _CommandName;

        public string Display =>
            _Display ?? (
            _Display = DisplayFactory());
        private string _Display;

        public bool DisplayDefault =>
            _DisplayDefault ?? (
            _DisplayDefault =
                Properties.Any(p => p.DisplayAttribute.Include == true) ? false :
                Properties.Any(p => p.DisplayAttribute.Include == false) ? true :
                true).Value;
        private bool? _DisplayDefault;

        public IEnumerable<TargetMethodValidation> Validations =>
            _Validations ?? (
            _Validations = TargetType
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
                .ToList());
        private IEnumerable<TargetMethodValidation> _Validations;

        public static TargetDescription Describe(Type targetType) {
            if (Cache.TryGetValue(targetType, out var targetDescription) == false) {
                Cache[targetType] = targetDescription = new TargetDescription(targetType);
            }
            return targetDescription;
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
                var key = token.Key?.Trim() ?? "";
                var val = token.Value?.Trim() ?? "";
                if (val == "" && key == "") {
                    continue;
                }
                if (val != "") {
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
