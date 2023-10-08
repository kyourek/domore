using System;
using CONSOLE = System.Console;

namespace Domore {
    using Conf;
    using CONF = Conf.Conf;

    public static class Console {
        public static void Release(string[] args) {
            try {
                CONF.Container.ContentsProvider = new AppSettingsProvider();
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
