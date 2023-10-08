using System;

namespace Domore.Conf {
    public class ConfException : Exception {
        public ConfException(string message, Exception innerException) : base(message, innerException) {
        }
    }
}
