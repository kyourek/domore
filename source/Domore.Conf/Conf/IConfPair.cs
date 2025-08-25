namespace Domore.Conf;

internal interface IConfPair : IConfToken {
    IConfKey Key { get; }
    IConfValue Value { get; }
}
