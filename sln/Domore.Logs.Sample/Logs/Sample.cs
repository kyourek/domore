using Domore.Conf.Logs;
using System;
using CONF = Domore.Conf.Conf;

namespace Domore.Logs {
    internal sealed class Sample {
        private static void Main() {
            CONF.Contain(@"
                log[console].config.default.severity = debug
                log[console].config.default.format   = {log} ({sev})
            ").ConfigureLogging();

            var log = Logging.For(typeof(Sample));
            if (log.Debug()) log.Debug($"Now it's {DateTime.Now}.");
            if (log.Info()) log.Info($"This is the logging sample.");
            if (log.Warn()) log.Warn($"Hey! Look out!");
            if (log.Error()) log.Error($"too late...");
            if (log.Critical()) log.Critical($"We've got to exit.");

            Logging.Complete();
        }
    }
}
