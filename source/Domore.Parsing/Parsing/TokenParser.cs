namespace Domore.Parsing;

internal abstract class TokenParser<T> {
    public abstract T Parse(string s);
}
