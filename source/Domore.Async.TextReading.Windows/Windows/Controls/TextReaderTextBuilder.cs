using Domore.Text;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Windows.Controls;

/// <summary>
/// Sends incremental decoder output to a <see cref="TextReader"/> control.
/// </summary>
internal sealed class TextReaderTextBuilder : DecodedTextBuilder {
    /// <summary>
    /// Gets the control that receives decoded text.
    /// </summary>
    public TextReader TextReader { get; }

    /// <summary>
    /// Initializes a builder for the specified control.
    /// </summary>
    /// <param name="textReader">The control that receives decoded text.</param>
    /// <exception cref="ArgumentNullException"><paramref name="textReader"/> is null.</exception>
    public TextReaderTextBuilder(TextReader textReader) {
        TextReader = textReader ?? throw new ArgumentNullException(nameof(textReader));
    }

    protected sealed override Task Clear(CancellationToken cancellationToken) {
        return TextReader.ClearText(cancellationToken);
    }

    protected sealed override Task Add(ReadOnlyMemory<char> memory, CancellationToken cancellationToken) {
        return TextReader.AddText(memory, cancellationToken);
    }
}
