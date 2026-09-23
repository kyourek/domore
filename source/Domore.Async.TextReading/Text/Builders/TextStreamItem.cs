namespace Domore.Text.Builders;

/// <summary>
/// An item read from <see cref="TextStreamBuilder.Read(System.Threading.CancellationToken)"/>.
/// </summary>
public sealed class TextStreamItem {
    internal TextStreamItem(string text) {
        Text = text;
        Clear = Text == null;
    }

    /// <summary>
    /// Gets whether all text read so far should be discarded, because the text that follows
    /// comes from another candidate encoding. If true, <see cref="Text"/> is null.
    /// </summary>
    public bool Clear { get; }
    /// <summary>
    /// Gets the next piece of decoded text, or null if <see cref="Clear"/> is true.
    /// </summary>
    public string Text { get; }
}
