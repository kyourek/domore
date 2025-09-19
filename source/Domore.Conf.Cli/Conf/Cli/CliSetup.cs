using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Domore.Conf.Cli {
    public sealed class CliSetup {
        private readonly ReadOnlyCollection<Func<Type, string>> CommandNameCallbacks;
        private readonly ReadOnlyCollection<Func<Type, string>> CommandSpaceCallbacks;

        private CliSetup(IEnumerable<Func<Type, string>> commandNameCallbacks, IEnumerable<Func<Type, string>> commandSpaceCallbacks) {
            CommandNameCallbacks = commandNameCallbacks?.Where(c => c is not null)?.ToList()?.AsReadOnly() ?? new([]);
            CommandSpaceCallbacks = commandSpaceCallbacks?.Where(c => c is not null)?.ToList()?.AsReadOnly() ?? new([]);
        }

        internal string CommandName(Type type) {
            return CommandNameCallbacks
                .Reverse()
                .Select(c => c(type)?.Trim() ?? "")
                .Where(s => s != "")
                .FirstOrDefault();
        }

        internal string CommandSpace(Type type) {
            return CommandSpaceCallbacks
                .Reverse()
                .Select(c => c(type)?.Trim() ?? "")
                .Where(s => s != "")
                .FirstOrDefault();
        }

        public CliSetup() : this(null, null) {
        }

        public CliSetup WithCommandName(Func<Type, string> callback) {
            return new(
                CommandNameCallbacks.Concat([callback]),
                CommandSpaceCallbacks);
        }

        public CliSetup WithCommandSpace(Func<Type, string> callback) {
            return new(
                CommandNameCallbacks,
                CommandSpaceCallbacks.Concat([callback]));
        }
    }
}
