using System;

namespace Domore.Conf.Cli {
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class CliDisplayOverrideAttribute : Attribute {
        internal CliDisplayOverrideAttribute() {
        }

        public string Display { get; }

        public CliDisplayOverrideAttribute(string display) {
            Display = display;
        }
    }
}
