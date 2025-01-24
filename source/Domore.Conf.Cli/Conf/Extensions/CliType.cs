using Domore.Conf.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Domore.Conf.Extensions {
    internal static class CliType {
        public static Dictionary<MemberInfo, string> GetEnumDisplay(this Type type) {
            var alias = ConfType.GetEnumAlias(type);
            var display = ConfType.GetEnumAttributes<CliDisplayAttribute>(type).ToList();
            var displayDefault =
                display.Any(d => d.Value?.Include == true) ? false :
                display.Any(d => d.Value?.Include == false) ? true :
                true;
            return display
                .Where(d => d.Value?.Include ?? displayDefault)
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Key
                        .GetCustomAttributes(typeof(CliDisplayOverrideAttribute), inherit: true)
                        .OfType<CliDisplayOverrideAttribute>()
                        .FirstOrDefault()?.Display ?? alias[pair.Key].OrderBy(a => a.Length).First());
        }
    }
}
