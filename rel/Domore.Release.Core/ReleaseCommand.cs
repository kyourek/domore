using Domore.Conf;
using System;
using System.Collections.Generic;
using System.Linq;
using CONF = Domore.Conf.Conf;

namespace Domore {
    public sealed class ReleaseCommand {
        private readonly IConfContainer Conf;

        public object Content { get; }

        public ReleaseCommand(object content) {
            Conf = CONF.Contain(Content = content);
        }

        public T Configure<T>(T obj, string key = null) =>
            Conf.Configure(obj, key);

        public static ReleaseCommand From(IDictionary<string, string> content) {
            if (null == content) throw new ArgumentNullException(nameof(content));
            return new ReleaseCommand(string.Join(Environment.NewLine, content.Select(pair => $"{pair.Key} = {pair.Value}")));
        }

        public static ReleaseCommand From(IEnumerable<string> content) {
            if (null == content) throw new ArgumentNullException(nameof(content));
            return From(content
                .Where(arg => arg.StartsWith("-"))
                .Where(arg => arg.Contains("="))
                .Select(arg => arg.Split(new[] { '=' }, 2))
                .ToDictionary(
                    pair => pair[0].TrimStart('-'),
                    pair => pair.Length > 1 ? pair[1] : null,
                    StringComparer.OrdinalIgnoreCase));
        }
    }
}
