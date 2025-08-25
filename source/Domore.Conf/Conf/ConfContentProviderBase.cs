using System.Collections.Generic;

namespace Domore.Conf;

internal abstract class ConfContentProviderBase : IConfContentProvider {
    public abstract ConfContent GetConfContent(object source, IEnumerable<object> sources, ConfContentProviderContext context);

    ConfContent IConfContentProvider.GetConfContent(object source) {
        return GetConfContent(source, null, null);
    }
}
