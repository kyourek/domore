using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Domore.Conf;

class ConfSample {
    static void Main() {
        /*
         * The static `Configure` method populates an object
         * with values from the application's '.conf' file.
         * 
         * An empty `key` parameter causes all keys matching
         * a property name to be considered.
         */
        var movie = Conf.Configure(new Movie(), key: "");
        var title = movie.Title;
        Console.WriteLine(new string([.. Enumerable.Range(0, title.Length).Select(_ => '-')]));
        Console.WriteLine($"{title}");
        Console.WriteLine(new string([.. Enumerable.Range(0, title.Length).Select(_ => '-')]));
        Console.WriteLine();

        /*
         * Here, an instance of `Alien` is configured from
         * the application's '.conf' file. Since no key is
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
         * '.conf' file.
         */
        Console.WriteLine($"A: {alien.Greeting}, {visitor.HomePlanet}!");
        Console.WriteLine($"A: Welcome to {alien.HomePlanet}.");
        Console.WriteLine($"V: Thanks! I also toured {string.Join(", ", visitor.TourDestinations)}");
        Console.WriteLine($"V: on a {string.Join(", ", visitor.ShipModelsAndMakes.Select(pair => $"{pair.Value} {pair.Key}"))}");
        Console.WriteLine();

        /*
         * The show's over. Roll the credits.
         */
        var credits = Conf.Configure(new Credits());
        Console.WriteLine("Credits");
        Console.WriteLine("-------");
        foreach (var character in credits.Cast.Characters) {
            Console.WriteLine($"{character.Key,-24}{character.Value}");
        }
        Console.WriteLine();

        /*
         * Each source that was used during configuration
         * is displayed here.
         */
        Debug.WriteLine(nameof(Conf.Sources));
        Debug.WriteLine("-------");
        Debug.WriteLine(string.Join(Environment.NewLine, Conf.Sources));
    }

    class Movie {
        public string Title { get; set; }
    }

    class Alien {
        public string Greeting { get; set; }
        public string HomePlanet { get; set; }
    }

    class Visitor {
        public string HomePlanet { get; set; }
        public IList<string> TourDestinations { get; set; } = new List<string>();
        public IDictionary<string, string> ShipModelsAndMakes { get; set; } = new Dictionary<string, string>();
    }

    class Credits {
        public Cast Cast { get; set; }
    }

    class Cast {
        public Dictionary<string, string> Characters { get; set; }
    }
}
