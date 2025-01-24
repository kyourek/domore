using System;

namespace Domore.Conf.Cli {
    public sealed class CliSetup {
        private Func<Type, string> CommandNameCallback;
        private Func<Type, string> CommandSpaceCallback;

        internal string CommandName(Type type) {
            var value = CommandNameCallback?.Invoke(type);
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        internal string CommandSpace(Type type) {
            var value = CommandSpaceCallback?.Invoke(type);
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        public CliSetup CommandName(Func<Type, string> callback) {
            CommandNameCallback = callback;
            return this;
        }

        public CliSetup CommandSpace(Func<Type, string> callback) {
            CommandSpaceCallback = callback;
            return this;
        }
    }
}
