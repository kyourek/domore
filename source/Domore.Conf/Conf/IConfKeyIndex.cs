namespace Domore.Conf;

internal interface IConfKeyIndex : IConfToken {
    IConfCollection<IConfKeyIndexPart> Parts { get; }
}
