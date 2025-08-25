namespace Domore.Parsing;

public abstract class Token : IToken {
    protected abstract string Create();

    public string Content => field ??= Create();

    public override string ToString() {
        return Content;
    }
}
