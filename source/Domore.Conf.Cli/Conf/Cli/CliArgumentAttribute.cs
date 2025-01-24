using System;

namespace Domore.Conf.Cli {
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class CliArgumentAttribute : Attribute {
        public int Order { get; }

        public CliArgumentAttribute(int order) {
            Order = order;
        }

        public CliArgumentAttribute() : this(0) {
        }
    }
}
