using Domore.Text;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Windows.Controls;

internal sealed class TextReaderTextBuilder : DecodedTextBuilder {
    public TextReader TextReader { get; }

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
