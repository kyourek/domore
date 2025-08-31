using System;
using System.Linq;

namespace Domore.Conf;

internal sealed class ConfTestHelper {
    private static void Main(string[] args) {
        Conf.Special = args.FirstOrDefault();
        var program = Conf.Configure(new ConfTestHelper(), key: "Program");
        Console.WriteLine(program.Greeting);
    }

    public string Greeting { get; set; }
}
