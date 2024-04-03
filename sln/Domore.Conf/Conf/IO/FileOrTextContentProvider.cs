using Domore.Conf.Text;
using System.Collections.Generic;
using FILE = System.IO.File;

namespace Domore.Conf.IO {
    internal sealed class FileOrTextContentProvider : ConfContentProviderBase {
        private TextContentProvider Text =>
            _Text ?? (
            _Text = new TextContentProvider());
        private TextContentProvider _Text;

        private FileContentProvider File =>
            _File ?? (
            _File = new FileContentProvider());
        private FileContentProvider _File;

        public sealed override ConfContent GetConfContent(object source, IEnumerable<object> sources, ConfContentProviderContext context) {
            var file = $"{source}".Trim();
            if (FILE.Exists(file)) {
                return File.GetConfContent(file, sources, context);
            }
            return Text.GetConfContent(source, sources, context);
        }
    }
}
