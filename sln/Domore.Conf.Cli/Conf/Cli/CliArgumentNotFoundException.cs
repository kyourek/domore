namespace Domore.Conf.Cli {
    internal sealed class CliArgumentNotFoundException : CliException {
        public string Argument { get; }

        public CliArgumentNotFoundException(string argument) {
            Argument = argument;
        }
    }
}
