using System;

namespace Domore.Conf.Cli {
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class CliValidationAttribute : Attribute {
        public int Order { get; }
        public string Message { get; }

        public CliValidationAttribute(int order, string message) {
            Order = order;
            Message = message;
        }

        public CliValidationAttribute(string message) : this(0, message) {
        }
    }
}
