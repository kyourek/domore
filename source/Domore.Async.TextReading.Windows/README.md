# Domore.Async.TextReading.Windows

`Domore.Async.TextReading.Windows` provides a WPF control for displaying text as it is decoded by
[Domore.Async.TextReading](../Domore.Async.TextReading/README.md). The control is read-only, reports the
detected encoding and load status, and cancels an in-progress decode when its source changes.

## Use the WPF control

Reference the `Domore.Async.TextReading.Windows` project or package, then declare the `domore` XAML
namespace:

```xml
<Window
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:domore="http://schemas.domore.local/winfx/2026/xaml/presentation">
  <domore:TextReader
      TextReaderSource="logs\app.log"
      TextReaderSourceLengthMax="52428800"
      TextWrapping="NoWrap"
      VerticalScrollBarVisibility="Auto"/>
</Window>
```

`TextReaderSource` accepts an `IStreamText`, a file path, a `FileInfo`, or a local file `Uri`. Relative
string paths are resolved against the application directory. For unsupported values, non-file URIs, and
invalid paths, null is returned as the source, so nothing is decoded. File metadata is read off the UI
thread; missing file paths are retained so `Reload()` can retry them later.

`TextReaderSourceLengthMax` limits the source size in bytes; `null` means no limit. Set
`TextReaderOptions` to customize encoding detection, or leave it `null` to use the decoder's defaults.
`TextReaderEnabled` controls whether decoding runs.

The read-only status properties `TextReaderLoading`, `TextReaderSuccess`, and `TextReaderEncoding` report
progress and the result. `TextReaderSourceChanged`, `TextReaderLoadingChanged`,
`TextReaderSuccessChanged`, and `TextReaderEncodingChanged` are available as routed events.

## Supported frameworks

.NET 6, .NET 8, and .NET 10 on Windows.
