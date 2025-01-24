using System;
using System.Linq;

namespace Domore.Conf.Test.Process {
    internal sealed class Program {
        private static void Main(string[] args) {
            Conf.Special = args.FirstOrDefault();
            var program = Conf.Configure(new Program());
            Console.WriteLine(program.Greeting);
        }

        public string Greeting { get; set; }
    }
}
