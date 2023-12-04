Do more in .NET

# Domore.Conf

Configure POCO objects with simple, forgiving strings from configuration files or anywhere in a program.

The following sample uses an application `.conf` file that looks like this:

    # This is the application's .conf file. Settings in these
    # files are largely case- and whitespace-insensitive. Hash
    # signs may precede comments, but they aren't strictly
    # necessary, as any line that does not parse to a known
    # configuration setting will be ignored.
    
    This alien will welcome our visitor
    alien.Greeting      = Hello
    Alien.Home planet   = Jupiter
    
    Our visitor is on a tour through the solar system.
    Visitor.homeplanet                              = Earth
    visitor.tour destinations[0]                    = Mercury
    visitor.tour destinations[1]                    = Venus
    visitor.tour destinations[2]                    = Mars
    visitor.ship models and makes[Tie Fighter]      = Imperial
    visitor.ship models and makes[Star Destroyer]   = Imperial
    visitor.ship models and makes[X-wing]           = Rebel
# 
    internal class Sample {
        private static void Main() {
            /*
             * Here, an instance of `Alien` is configured from
             * the application's `.conf` file. Since no key is
             * provided, the name of the type (Alien) is used
             * as the key.
             */
            var alien = Conf.Configure(new Alien());
    
            /*
             * The same thing is done for an instance of `Visitor`.
             */
            var visitor = Conf.Configure(new Visitor());
    
            /*
             * After configuration, properties of each instance
             * will match the values specified in the application's
             * `.conf` file.
             */
            Console.WriteLine($"A: {alien.Greeting}, {visitor.HomePlanet}!");
            Console.WriteLine($"A: Welcome to {alien.HomePlanet}.");
            Console.WriteLine($"V: Thanks! I also toured {string.Join(", ", visitor.TourDestinations)}");
            Console.WriteLine($"V: on a {string.Join(", ", visitor.ShipModelsAndMakes.Select(pair => $"{pair.Value} {pair.Key}"))}");
            Console.WriteLine();
    
            /*
             * Each source that was used during configuration
             * is displayed here.
             */
            Console.WriteLine(nameof(Conf.Sources));
            Console.WriteLine("-------");
            Console.WriteLine(string.Join(Environment.NewLine, Conf.Sources));
        }
    
        private class Alien {
            public string Greeting { get; set; }
            public string HomePlanet { get; set; }
        }
    
        private class Visitor {
            public string HomePlanet { get; set; }
            public IList<string> TourDestinations { get; set; } = new List<string>();
            public IDictionary<string, string> ShipModelsAndMakes { get; set; } = new Dictionary<string, string>();
        }
    }

# Domore.Logs

A lightweight, simple, and very opinionated logging library.

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
             * Call `Logging.Complete` before the program exits
             * to guarantee the pending log queue is emptied.
             */
            Logging.Complete();
        }
    }