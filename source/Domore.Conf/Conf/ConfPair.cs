using System;
using System.Linq;

namespace Domore.Conf;

internal sealed class ConfPair : IConfPair {
    public string Content => field ??= ToString(this);

    public IConfKey Key { get; }
    public IConfValue Value { get; }

    public ConfPair(IConfKey key, IConfValue value) {
        Key = key;
        Value = value;
    }

    public static string ToString(IConfPair pair) {
        if (null == pair) throw new ArgumentNullException(nameof(pair));
        var key = $"{pair.Key}";
        var keyPad = default(string);
        var keyPadding = new Func<string>(() => keyPad ??= new string(Enumerable.Range(0, key.Length + 1).Select(_ => ' ').ToArray()));
        var value = $"{pair.Value}";
        if (value.IndexOf('\n') > -1) {
            value = string.Join(Environment.NewLine,
                "{",
                string.Join(Environment.NewLine, value
                    .Split('\n')
                    .Select(line => line.Trim())
                    .Select(line => keyPadding() + line)),
                keyPadding() + "}");
        }
        return $"{key}={value}";
    }
}
