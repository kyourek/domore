using System;
using System.Linq;

namespace Domore.Conf {
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class ConfHelpAttribute : Attribute {
        internal string Format(string prefix) {
            var text = Text;
            if (text == null) {
                return null;
            }
            var lines = text
                .Replace("\r", "")
                .TrimStart('\n')
                .TrimEnd()
                .Split('\n');
            var nonWhite = lines.Where(line => !string.IsNullOrWhiteSpace(line));
            var whiteSpace = nonWhite.Any()
                ? nonWhite.Min(line => line.TakeWhile(c => char.IsWhiteSpace(c)).Count())
                : 0;
            var trimmed = lines
                .Select(line => string.IsNullOrWhiteSpace(line)
                    ? line
                    : line.Substring(whiteSpace, line.Length - whiteSpace));
            var help = string.Join(Environment.NewLine,
                trimmed.Select(line => string.IsNullOrWhiteSpace(line)
                    ? $"{prefix}"
                    : $"{prefix}{line}"));
            return help;
        }

        public string Text { get; }

        public ConfHelpAttribute(string text) {
            Text = text;
        }
    }
}
