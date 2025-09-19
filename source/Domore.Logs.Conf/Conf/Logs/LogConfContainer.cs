using Domore.Logs;
using System;

namespace Domore.Conf.Logs; 
public static class LogConfContainer {
    public static void ConfigureLogging(this IConfContainer confContainer) {
        if (null == confContainer) throw new ArgumentNullException(nameof(confContainer));
        confContainer.Configure(Logging.Config, key: "");
    }
}
