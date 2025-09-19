using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace Domore.Conf;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ConfPopulatedCallbackAttribute : Attribute {
    private static readonly ConcurrentDictionary<Type, ReadOnlyCollection<DecoratedMethod>> Cache = [];

    internal static IEnumerable<DecoratedMethod> For(Type type) {
        return Cache.GetOrAdd(type, type => {
            static IEnumerable<Type> hierarchy(Type type) {
                for (var baseType = type; baseType is not null; baseType = baseType.BaseType) {
                    yield return baseType;
                }
            }
            return hierarchy(type)
                .Reverse()
                .SelectMany(type => type
                    .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(method =>
                        method.ReflectedType == method.DeclaringType &&
                        method
                            .GetCustomAttributes(inherit: false)
                            .OfType<ConfPopulatedCallbackAttribute>()
                            .Any()))
                .Select(method => new DecoratedMethod(method))
                .ToList()
                .AsReadOnly();
        });
    }

    internal sealed class DecoratedMethod {
        private Action<object, IConf, IEnumerable<IConfPair>> Action => _Action ??=
            GetAction(MethodInfo);
        private Action<object, IConf, IEnumerable<IConfPair>> _Action;

        private static Action<object, IConf, IEnumerable<IConfPair>> GetAction(MethodInfo method) {
            if (method is null) {
                throw new ArgumentNullException(nameof(method));
            }
            var parameters = method.GetParameters();
            if (parameters.Length == 0) {
                return (target, _, _) => {
                    method.Invoke(target, null);
                };
            }
            if (parameters.Length == 1) {
                var p = parameters[0];
                var pType = p.ParameterType;
                if (pType.IsAssignableFrom(typeof(IConf))) {
                    return (target, conf, _) => {
                        method.Invoke(target, [conf]);
                    };
                }
            }
            throw new ConfPopulatedCallbackSignatureInvalidException();
        }

        public MethodInfo MethodInfo { get; }

        public DecoratedMethod(MethodInfo methodInfo) {
            MethodInfo = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));
        }

        public void Call(object target, IConf conf, IEnumerable<IConfPair> pairs) {
            Action(target, conf, pairs);
        }
    }
}
