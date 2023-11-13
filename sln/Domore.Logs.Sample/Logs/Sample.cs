using Domore.Conf.Logs;
using System;
using CONF = Domore.Conf.Conf;

namespace Domore.Logs {
    internal sealed class Sample {
        private static void Main() {
            CONF.Contain(@"
                log[console].config.default.severity        = debug
                log[console].config.default.format          = {log} ({sev})
            ").ConfigureLogging();

            var log = Logging.For(typeof(Sample));
            if (log.Debug()) log.Debug($"Now it's {DateTime.Now}.");
            if (log.Info()) log.Info($"This is the logging sample.");
            if (log.Warn()) log.Warn($"Hey! Look out!");
            if (log.Error()) log.Error($"too late...");
            if (log.Critical()) log.Critical($"We've got to exit.");

            CONF.Contain(@"
                log[console].service.background[debug]      = cyan
                log[console].service.foreground[debug]      = black
                                                            
                log[console].service.background[info]       = gray
                log[console].service.foreground[info]       = black
                                                            
                log[console].service.background[warn]       = yellow
                log[console].service.foreground[warn]       = black

                log[console].service.background[error]      = red
                log[console].service.foreground[error]      = black

                log[console].service.background[critical]   = black
                log[console].service.foreground[critical]   = white
            ").ConfigureLogging();

            if (log.Debug()) log.Debug($"Now it's {DateTime.Now}.");
            if (log.Info()) log.Info($"This is the logging sample.");
            if (log.Warn()) log.Warn($"Hey! Look out!");
            if (log.Error()) log.Error($"too late...");
            if (log.Critical()) log.Critical($"We've got to exit.");

            log.Critical("Changing default format and minimum severity...");

            CONF.Contain(@"
                log[console].config.default.severity        = info
                log[console].config.default.format          = {dat} {tim} [{sev}]
            ").ConfigureLogging();

            log.Debug($"This won't show up.");
            log.Info($"But this will.");

            Logging.Complete();
        }
    }
}
