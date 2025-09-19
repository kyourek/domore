using System;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Conf;

internal sealed class ConfSample {
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
