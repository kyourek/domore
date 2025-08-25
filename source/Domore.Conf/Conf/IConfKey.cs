namespace Domore.Conf;

internal interface IConfKey : IConfToken {
    IConfCollection<IConfKeyPart> Parts { get; }
    IConfKey Skip();
}
