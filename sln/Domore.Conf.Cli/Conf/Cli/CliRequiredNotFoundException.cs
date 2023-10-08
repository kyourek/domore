using System.Collections.Generic;

namespace Domore.Conf.Cli {
    internal sealed class CliRequiredNotFoundException : CliException {
        public IEnumerable<TargetPropertyDescription> NotFound { get; }

        public CliRequiredNotFoundException(IEnumerable<TargetPropertyDescription> notFound) {
            NotFound = notFound;
        }
    }
}
