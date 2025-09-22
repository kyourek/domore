namespace Domore.Conf.Text.Parsing.Tokens;

internal abstract class MultiLineValueBuilder : ValueBuilder {
    protected MultiLineValueBuilder(KeyBuilder key) : base(key) {
    }

    protected Token Complete() {
        if (String.Length > 0) {
            if (Sep == '\n') {
                if (String[String.Length - 1] == '\r') {
                    String.Remove(String.Length - 1, 1);
                }
            }
            var whitespace = true;
            for (var i = 0; i < String.Length; i++) {
                var ws = whitespace = whitespace && char.IsWhiteSpace(String[i]);
                if (ws == false) {
                    break;
                }
            }
            if (whitespace) {
                String.Clear();
            }
        }
        if (String.Length > 0) {
            return new Complete(Key, this);
        }
        else {
            return new KeyBuilder(Sep);
        }
    }
}
