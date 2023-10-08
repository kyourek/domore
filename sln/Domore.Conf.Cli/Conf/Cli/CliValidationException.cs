using System;

namespace Domore.Conf.Cli {
    public class CliValidationException : CliException {
        public CliValidationException(string message) : base(message) {
        }

        public CliValidationException(string message, Exception innerException) : base(message, innerException) {
        }
    }
}
