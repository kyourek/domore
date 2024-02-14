using System;

namespace Domore.Conf.Test.Process {
    internal sealed class Program {
        private static void Main(string[] args) {
            var program = Conf.Configure(new Program());
            Console.WriteLine(program.Greeting);
        }

        public string Greeting { get; set; }
    }
}
