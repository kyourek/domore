using System.Collections.Generic;
using System.Linq;

namespace Domore.Conf.Cli; 
internal sealed class CliRequiredNotFoundException : CliException {
    public sealed override string Message => _Message ??=
        $"Missing required: {string.Join(", ", NotFound.Select(p => p.DisplayName))}";
    private string _Message;

    public IEnumerable<TargetPropertyDescription> NotFound { get; }

    public CliRequiredNotFoundException(IEnumerable<TargetPropertyDescription> notFound) {
        NotFound = notFound;
    }
}
