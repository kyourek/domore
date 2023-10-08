namespace Domore.Conf {
    internal sealed class ConfPair : IConfPair {
        public string Content =>
            _Content ?? (
            _Content = $"{Key}={Value}");
        private string _Content;

        public IConfKey Key { get; }
        public IConfValue Value { get; }

        public ConfPair(IConfKey key, IConfValue value) {
            Key = key;
            Value = value;
        }
    }
}
