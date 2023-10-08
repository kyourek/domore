namespace Domore.Conf.Extensions {
    using Text;

    public static class ConfObject {
        private static TextSourceProvider SourceProvider =>
            _SourceProvider ?? (
            _SourceProvider = new TextSourceProvider());
        private static TextSourceProvider _SourceProvider;

        private static TextContentProvider ContentProvider =>
            _ContentProvider ?? (
            _ContentProvider = new TextContentProvider());
        private static TextContentProvider _ContentProvider;

        public static string ConfText(this object obj, string key = null, bool? multiline = null) {
            return SourceProvider.GetConfSource(obj, key, multiline);
        }

        public static T ConfFrom<T>(this T obj, string text, string key = null) {
            return new ConfContainer { Source = text, ContentProvider = ContentProvider }.Configure(obj, key);
        }
    }
}
