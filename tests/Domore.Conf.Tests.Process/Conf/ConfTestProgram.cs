using System;
using System.Linq;

namespace Domore.Conf;

internal sealed class ConfTestProgram {
    private static void Main(string[] args) {
        Conf.Special = args.FirstOrDefault();
        var program = Conf.Configure(new ConfTestProgram());
        Console.WriteLine(program.Greeting);
    }

    public string Greeting { get; set; }
}
