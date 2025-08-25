using Domore.Conf.Text;
using System.Collections.Generic;
using System.IO;

namespace Domore.Conf.IO {
    internal sealed class FileContentProvider : ConfContentProviderBase {
        private TextContentProvider Text => field ??= new();

        public sealed override ConfContent GetConfContent(object source, IEnumerable<object> sources, ConfContentProviderContext context) {
            var path = $"{source}";
            var text = File.ReadAllText(path);
            return Text.GetConfContent(text, [path], context);
        }
    }
}
