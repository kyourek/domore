namespace Domore.Conf.Cli {
    internal sealed class CliArgumentNotFoundException : CliException {
        public sealed override string Message =>
            $"Unexpected argument: {Argument}";

        public string Argument { get; }

        public CliArgumentNotFoundException(string argument) {
            Argument = argument;
        }
    }
}
