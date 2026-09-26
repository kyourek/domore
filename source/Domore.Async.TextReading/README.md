# Domore.Async.TextReading

Decode text files and streams asynchronously, even when you don't know the encoding. Domore.Async.TextReading reads through pooled buffers, detects byte-order marks, tries several candidate encodings at the same time, and hands you the text as a string, as lines, or as an async stream while it's still decoding.

Install the package with `dotnet add package Domore.Async.TextReading`.

## Decode a file

```csharp
using Domore.IO.Extensions;
using Domore.Text;
using Domore.Text.Builders;

var decoded = await new FileInfo("data.txt").DecodeText(
    new TextLineBuilder(onLine: line => Console.WriteLine(line)),
    options: null,
    cancellationToken);

Console.WriteLine(decoded.EncodingWebName); // utf-8
Console.WriteLine(decoded.Text());          // the whole text
```

With no options, the text is decoded as UTF-8. `DecodeText` returns a `DecodedText` with the full `Text()`, the `EncodingName` and `EncodingWebName` that were used, the `TextLength`, and flags such as `Success`. If the content can't be decoded with any candidate encoding, `DecodeText` returns `null`.

## Detect the encoding

List candidate encodings in order of preference. Each candidate decodes the content at the same time, and the first candidate in your list that decodes the content without errors wins:

```csharp
var options = new DecodedTextOptions {
    Encoding = { "utf-8", "iso-8859-1" }
};

using (options.Disposable()) {
    var decoded = await new FileInfo("legacy.txt").DecodeText(new TextStringBuilder(), options, cancellationToken);
    Console.WriteLine(decoded.EncodingWebName); // utf-8 if valid, otherwise iso-8859-1
}
```

- **Byte-order marks win.** A UTF-8, UTF-16, or UTF-32 byte-order mark is detected automatically and takes precedence over the candidate encoding.
- **Invalid bytes fail by default.** To accept invalid bytes and replace them instead, set a replacement string for that encoding: `EncodingFallback = { ["utf-8"] = "?" }`.
- **Any .NET encoding name works.** Names are resolved with `Encoding.GetEncoding`. On .NET, code pages such as `windows-1252` require registering `CodePagesEncodingProvider.Instance` first.

Options own the buffer pools used while decoding. When you pass options, dispose them with `options.Disposable()` (or `DisposableAsync()`) when you're done with them. When you pass `null`, the options are created and disposed for you.

## Consume the text

Pass a builder to receive text as it's decoded, or pass several builders at once as an `IEnumerable<DecodedTextBuilder>`:

| Builder | Result |
|---------|--------|
| `TextLineBuilder` | Calls `onLine` with each line, without its `\r\n` or `\n` ending. `onComplete` runs after the last line. |
| `TextStringBuilder` | Collects the text. Call `ToString()` for the result. |
| `TextStreamBuilder` | Exposes the text as an `IAsyncEnumerable<TextStreamItem>` through `Read`. |

```csharp
var lines = new List<string>();
var all = new TextStringBuilder();

await file.DecodeText(new DecodedTextBuilder[] { new TextLineBuilder(onLine: lines.Add), all }, null, cancellationToken);
```

Read a `TextStreamBuilder` while the decode is running:

```csharp
var stream = new TextStreamBuilder();
var decoding = file.DecodeText(stream, null, cancellationToken);

await foreach (var item in stream.Read(cancellationToken)) {
    if (item.Clear) {
        text.Clear();
    }
    else {
        text.Append(item.Text);
    }
}

await decoding;
```

While several candidate encodings are decoding, the text reported so far may come from a candidate that later fails. When that happens, the builder is cleared and the winning candidate's text is reported again from the start. `TextLineBuilder` calls `onClear`, and `TextStreamBuilder` yields an item whose `Clear` is `true`. With a single candidate encoding, this never happens.

To handle progress yourself, pass a `DecodedTextDelegate` instead of a builder. It receives a `DecodedText` snapshot each time more text is decoded. To write your own builder, derive from `DecodedTextBuilder` and override `Add` and `Clear`, and optionally `Complete` and `Fail`.

## Decode other sources

Implement `IStreamText` to decode something other than a file:

```csharp
using Domore.IO;

public sealed class BlobText : IStreamText {
    private readonly byte[] Bytes;

    public BlobText(byte[] bytes) {
        Bytes = bytes;
    }

    public long StreamLength => Bytes.Length;

    public Stream StreamText() => new MemoryStream(Bytes);

    public Task<IDisposable> StreamReady(CancellationToken cancellationToken) => Task.FromResult<IDisposable>(null);
}

var decoded = await new BlobText(bytes).DecodeText(new TextStringBuilder(), null, cancellationToken);
```

`StreamReady` runs before the stream is opened, so it can wait for a file lock or acquire a resource. Anything it returns is disposed when decoding finishes. The stream from `StreamText` is disposed as well.

## Tune buffers

`DecodedTextOptions.StreamBuffer` (bytes) and `TextBuffer` (characters) control buffer rental:

| Option | Default | Description |
|--------|---------|-------------|
| `Size` | 512 | The minimum length of each rented buffer. |
| `Shared` | `true` | Rent from `ArrayPool<T>.Shared`. If `false`, each decode rents from its own pool. |
| `Clear` | `false` | Clear buffers when they're returned, which is useful for sensitive content. |

## Supported frameworks

.NET 6, .NET 8, and .NET 10.

For WPF applications, use the [Domore.Async.TextReading.Windows companion project](../Domore.Async.TextReading.Windows/README.md), which provides a control for displaying text while it is decoded.
