using System;

namespace Domore.Conf.Cli {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class CliExampleAttribute : Attribute {
        internal string Format(string invoke) {
            return string.Join(Environment.NewLine, "ex. " + invoke + " " + Command, ConfAttribute.Format("    ", Description));
        }

        public string Command { get; }
        public string Description { get; }

        public CliExampleAttribute(string command, string description) {
            Command = command;
            Description = description;
        }
    }
}
