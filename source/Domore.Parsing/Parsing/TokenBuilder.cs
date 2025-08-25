namespace Domore.Parsing;

public abstract class TokenBuilder : Token {
    public abstract Token Build(string s, ref int i);
}
