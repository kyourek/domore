namespace Domore.Conf {
    internal interface IConfKeyPart : IConfToken {
        IConfCollection<IConfKeyIndex> Indices { get; }
    }
}
