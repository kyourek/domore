using Domore.Conf.Logs;
using System;
using CONF = Domore.Conf.Conf;
using CONSOLE = System.Console;

namespace Domore {
    internal class Program {
        private static void Main(string[] args) {
            try {
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
