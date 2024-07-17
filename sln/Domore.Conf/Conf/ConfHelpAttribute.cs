using System;

namespace Domore.Conf {
    /// <summary>
    /// Text used as help info in conf contexts for the decorated property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class ConfHelpAttribute : Attribute {
        internal string Format(string prefix) {
            return ConfAttribute.Format(prefix, Text);
        }

        /// <summary>
        /// Gets the help text.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Creates help text for a property.
        /// </summary>
        /// <param name="text">The help text.</param>
        public ConfHelpAttribute(string text) {
            Text = text;
        }
    }
}
