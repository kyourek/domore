using System;
using System.Collections.Generic;

namespace Domore {
    using Conventions;
    using ReleaseActions;
    using CONF = Conf.Conf;

    public sealed class Release {
        public ReleaseCommand Command { get; }
        public string Repository { get; set; }

        public IEnumerable<ReleaseAction> Actions =>
            _Actions ?? (
            _Actions = new ReleaseAction[] {
                new Clone(),
                new Bump(),
                new Restore(),
                new Build(),
                new Pack(),
                new Tag(),
                new Push(),
                new Clean() });
        private IEnumerable<ReleaseAction> _Actions;

        public Release(ReleaseCommand command) {
            Command = command ?? throw new ArgumentNullException(nameof(command));

            T config<T>(T obj) {
                CONF.Configure(obj, "");
                CONF.Configure(obj);

                Command.Configure(obj, "");
                Command.Configure(obj);

                return obj;
            }

            var info = config(this);
            var codeBase = new CodeBase(info.Repository);

            foreach (var action in Actions) {
                config(action);
                action.CodeBase = codeBase;
                action.Solution = codeBase.Solution;
                action.Work();
            }
        }

        public Release(object command) : this(new ReleaseCommand(command)) {
        }
    }
}
