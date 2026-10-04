using System;
using System.IO;
using Domore.Logs;
using CONF = Domore.Conf.Conf;

namespace Domore.Logs.ExitHelper;
internal static class Program {
    private static int Main(string[] args) {
        if (args.Length != 2) {
            return 1;
        }

        var outputFile = Path.GetFullPath(args[0]);
        var directory = Path.GetDirectoryName(outputFile);
        CONF.Contain($@"
                log[exit].type = file
                log[exit].service.directory = {directory}
                log[exit].service.name = {Path.GetFileName(outputFile)}
                log[exit].service.flush interval = 01:00:00
                log[exit].config.default.severity = info
                log[exit].config.default.format = {{log}}
            ")
            .Configure(Logging.Config, key: "");

        Logging.For(typeof(Program)).Info(args[1]);
        return 0;
    }
}
