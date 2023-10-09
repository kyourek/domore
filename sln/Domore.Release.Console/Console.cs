using Domore.Conf;
using Domore.Conf.Logs;
using System;
using CONF = Domore.Conf.Conf;
using CONSOLE = System.Console;

namespace Domore {
    public static class Console {
        public static void Release(string[] args) {
            try {
                CONF.ContentProvider = new AppSettingsProvider();
                CONF.Container.ConfigureLogging();

                var command = ReleaseCommand.From(args);
                new Release(command);
            }
            catch (Exception ex) {
                CONSOLE.WriteLine(ex);
            }
        }
    }
}
