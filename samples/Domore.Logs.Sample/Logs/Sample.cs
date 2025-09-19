using Domore.Conf.Logs;
using System;
using System.Collections.Generic;

namespace Domore.Logs; 
using Conf = Conf.Conf;

internal class Sample {
    /*
     * Typically, an instance of `ILog` is created for each type
     * in code as a static member. The type may be added to logs
     * to differentiate the source of each message.
     */
    private static readonly ILog Log = Logging.For(typeof(Sample));

    private static void Main() {
        /*
         * Reference `Domore.Logs.Conf` to get the extension method
         * `ConfigureLogging` on the `IConfContainer` type. That
         * method allows the configuration of logs at runtime with
         * the exact same strings found in a `.conf` file.
         */
        Conf.Contain(@"
                log[console].config.default.severity        = debug
                log[console].config.default.format          = {log} ({sev})
            ").ConfigureLogging();

        /*
         * It's a good practice to use the logging methods without
         * arguments to check whether or not any logging would
         * actually take place at that respective severity. It's a
         * performance consideration, as this check is much less
         * expensive than the call with arguments, particularly
         * if the arguments are formatted strings.
         */
        if (Log.Debug()) Log.Debug($"Now it's {DateTime.Now}.");
        if (Log.Info()) Log.Info($"This is the logging sample.");
        if (Log.Warn()) Log.Warn($"Hey! Look out!");
        if (Log.Error()) Log.Error($"too late...");
        if (Log.Critical()) Log.Critical($"We've got to exit.");

        /*
         * Logging can be reconfigured at any time.
         */
        Conf.Contain(@"
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

        /*
         * It's not strictly necessary to check whether or not
         * logging will occur before doing logging. In code
         * where performance isn't critical, this may be more
         * legible.
         */
        Log.Debug($"Now it's {DateTime.Now}.");
        Log.Info($"This is the logging sample.");
        Log.Warn($"Hey! Look out!");
        Log.Error($"too late...");
        Log.Critical($"We've got to exit.");

        Log.Critical("Changing default format and minimum severity...");

        /*
         * Here, the severity threshold is raised to `Info`,
         * and the date and time are added to log messages. 
         */
        Conf.Contain(@"
                log[console].config.default.severity        = info
                log[console].config.default.format          = {dat} {tim} [{sev}]
            ").ConfigureLogging();

        Log.Debug($"This won't show up.");
        Log.Info($"But this will.");

        /*
         * The static method `ConfigureLogging` of class `LogConf`
         * can be called as a shortcut to log configuration.
         */
        LogConf.ConfigureLogging("log[console].service.background[info] = black; log[console].service.foreground[info] = gray");

        /*
         * Custom formats may be specified for types. That
         * format will be used whenever an instance of the
         * type is passed as a parameter to one of the logging
         * methods. To specify a custom format for a type,
         * call `Logging.Format`.
         */
        Logging.Format(typeof(XY), obj => [$"{((XY)obj).X},{((XY)obj).Y}"]);
        Log.Info("These XY coordinates have been formatted by a callback.", new XY { X = 1, Y = 2 }, new XY { X = 2, Y = 3 });

        /*
         * Custom log handlers implement `ILogService`.
         * They're used by specifying the assembly qualified
         * name of the type in conf. 
         */
        LogConf.ConfigureLogging($@"
                log[queue].type = {typeof(CustomLogQueue).AssemblyQualifiedName}
                log[queue].config.default.severity = info
            ");
        Log.Info("This message will be handled by the custom log service:", typeof(CustomLogQueue).AssemblyQualifiedName);

        /*
         * Call `Logging.Complete` before the program exits
         * to guarantee the pending log queue is emptied.
         */
        Logging.Complete();
    }

    private class CustomLogQueue : ILogService {
        public static Queue<string> Queue { get; } = new();

        public void Log(string name, string data, LogSeverity severity) {
            Queue.Enqueue(data);
        }

        public void Complete() {
        }
    }

    private class XY {
        public int X { get; set; }
        public int Y { get; set; }
    }
}
