using Domore.Logs;
using System;

namespace Domore.Conf.Logs; 
public static class LogConfContainer {
    /// <summary>
    /// Configures logging from a conf container.
    /// </summary>
    /// <remarks>
    /// The built-in container applies exact <c>log[service].type</c> pairs before
    /// service settings. Other container implementations keep their supplied pair order.
    /// </remarks>
    public static void ConfigureLogging(this IConfContainer confContainer) {
        if (null == confContainer) throw new ArgumentNullException(nameof(confContainer));
        Logging.Configure(target => LogConfPopulation.Configure(confContainer, target));
    }
}

internal static class LogConfPopulation {
    internal static void Configure(IConf conf, object target) {
        if (conf is null) throw new ArgumentNullException(nameof(conf));
        if (conf is ConfContainer container) {
            container.Configure(target, key: "", first: IsServiceType);
        }
        else {
            conf.Configure(target, key: "");
        }
    }

    private static bool IsServiceType(IConfPair pair) {
        var parts = pair?.Key?.Parts;
        if (parts is null || parts.Count != 2) {
            return false;
        }
        var service = parts[0];
        var type = parts[1];
        return service.Content.Equals("log", StringComparison.OrdinalIgnoreCase) &&
               service.Indices.Count == 1 &&
               type.Content.Equals("type", StringComparison.OrdinalIgnoreCase) &&
               type.Indices.Count == 0;
    }
}
