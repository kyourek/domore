using Domore.Conf.Text;
using FILE = System.IO.File;

namespace Domore.Conf.IO {
    internal sealed class FileOrTextContentProvider : IConfContentProvider {
        private TextContentProvider Text =>
            _Text ?? (
            _Text = new TextContentProvider());
        private TextContentProvider _Text;

        private FileContentProvider File =>
            _File ?? (
            _File = new FileContentProvider());
        private FileContentProvider _File;

        public ConfContent GetConfContent(object contents) {
            var file = $"{contents}".Trim();
            if (FILE.Exists(file)) {
                return File.GetConfContent(file);
            }
            return Text.GetConfContent(contents, null);
        }
    }
}
