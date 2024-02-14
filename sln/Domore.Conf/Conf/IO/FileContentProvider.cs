using Domore.Conf.Text;
using System.IO;

namespace Domore.Conf.IO {
    internal sealed class FileContentProvider : IConfContentProvider {
        private TextContentProvider Text =>
            _Text ?? (
            _Text = new TextContentProvider());
        private TextContentProvider _Text;

        public ConfContent GetConfContent(object source) {
            var path = $"{source}";
            var text = File.ReadAllText(path);
            return Text.GetConfContent(text, new[] { path });
        }
    }
}
