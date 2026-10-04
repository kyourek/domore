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
