# Code review: `source\Domore.Async.TextReading.Windows`

Files reviewed:

- `Windows\Controls\TextReader.xaml`
- `Windows\Controls\TextReader.xaml.cs`
- `Windows\Controls\TextReaderTextBuilder.cs`
- `IO\TextReaderWorker.cs`
- `Properties\AssemblyInfo.cs`
- `Domore.Async.TextReading.Windows.csproj`

Findings are ordered by severity.

---

## High

### 1. A source set before `Loaded` is never read

`TextReader.xaml.cs`: `This_Loaded`, `OnTextReaderSourceChanged`, `Refresh`

`Worker` is created only in `This_Loaded`. When `TextReaderSource` is set before that, for example from a
XAML attribute, a binding that resolves during initialization, or in code before the control is shown,
`OnTextReaderSourceChanged` calls `Refresh()`, and `Refresh()` returns early because `Worker` is `null`.
`This_Loaded` then creates the worker with the current source but never calls `Refresh()`, so nothing is
loaded until the source changes again.

```xml
<domore:TextReader TextReaderSource="C:\log.txt"/>  <!-- never displays anything -->
```

The sample does not show this problem because its binding source `TextBox` starts empty.

**Fix:** Call `Refresh()` at the end of `This_Loaded`, or when the source is non-null.

### 2. Overlapping refreshes corrupt state and leave a load that cannot be canceled

`TextReader.xaml.cs`: `Refresh`

**Fixed:** Each refresh now receives its own `DecodedText` result, and only the refresh that still owns the
current worker and cancellation source can update UI state. Cancellation ownership is cleared in `finally`
only if it still belongs to that refresh. These checks also guard against refreshes triggered reentrantly by
property-change event handlers.

The worker no longer stores a shared `Decoded` result, so an older completion cannot report a newer
refresh's result.

---

## Medium

### 3. Old text stays visible when no new text is loaded

`TextReader.xaml.cs`: `Refresh`

**Fixed:** Each current refresh clears the `TextBox` before starting the worker, so early returns leave it
empty. If decoding completes without success, the refresh clears any partial output before reporting its
result. Both operations use the refresh cancellation token, so an obsolete refresh cannot clear output from
a newer one.

### 4. Changing `TextReaderEnabled`, `TextReaderOptions`, or `TextReaderSourceLengthMax` does nothing visible

**Fixed:** `OnTextReaderEnabledChanged`, `OnTextReaderOptionsChanged`, and
`OnTextReaderSourceLengthMaxChanged` now refresh the active worker after updating its settings. Enabling a
reader loads its current source; disabling it cancels the active load and clears the display; option and
length-limit changes are applied to the current source.

There is still no public way to reload the same source, for example after the file changes on disk; that
would be a separate API enhancement.

### 5. Unloading and loading again leaves partial text

**Fixed by #1 and #3:** `This_Loaded` now starts a refresh, and the refresh clears the existing text before
decoding again.

### 6. The text box can be edited, and it records undo history

`TextReader.xaml`: `PART_TextBox`

The control is a reader, but `PART_TextBox` was not read-only. Users could type into it while text was
being appended, potentially mixing their edits with the decoded text.

`IsUndoEnabled` was also `true`, so every `AppendText` call created an undo unit and used memory for large
files.

**Fixed:** `PART_TextBox` now sets `IsReadOnly="True"` to prevent edits and `IsUndoEnabled="False"` to avoid
retaining an undo history for appended content.

### 7. Adding text at `Render` priority can block input on large files

`TextReader.AddText` and `ClearText`

**Fixed:** Both methods now dispatch at `DispatcherPriority.Background`, allowing pending input and render
work to run first instead of queuing text updates ahead of input.

Very large files can still consume memory and require repeated `TextBox` layout; a source-size limit may
be useful for those cases.

---

## Low

### 8. `TemplateBinding` of `Background` and `SelectionTextBrush` overrides the `TextBox` defaults

**Fixed:** `TextReader.Background` now defaults to `SystemColors.WindowBrush`, matching the text box's
background. `SelectionTextBrush`, both scrollbar-visibility properties, and `TextWrapping` now use the
framework dependency properties via `AddOwner`. The scroll-bar properties retain their prior `Auto`
defaults.

### 9. Some common properties are not passed to the template

The template passes `BorderThickness` to `PART_TextBox`, but not `BorderBrush`, `Padding`, or `IsTabStop`.
Setting `BorderBrush` on `TextReader` has no effect.

### 10. The encoding label takes space when it is empty

The `Label` in row 1 is always shown. When `TextReaderEncoding` is `null`, the default `Label` padding still
reserves an empty row of about 10 px.

A `Label` also treats `_` in its content as an access-key marker, so an encoding name that contains an
underscore would be shown incorrectly.

**Fix:** Collapse the label when the encoding is `null`, and use a `TextBlock` or
`RecognizesAccessKey="False"` to avoid access-key handling.

### 11. Default label style is handled in two places

`OnApplyTemplate` and `OnTextReaderEncodingLabelStyleChanged` both look up
`TextReaderEncodingLabelStyleDefault` and apply it with `SetCurrentValue`. This works, but the same logic is
repeated. A `CoerceValueCallback`, or a `FallbackValue` or `TargetNullValue` on the template binding, would be
simpler. Setting the style back to `null` also cannot return the label to the plain `Label` style.

### 12. Limited source types and file I/O on the UI thread

`ConvertSource`

- Only `IStreamText` and `string` values are accepted. `FileInfo` and `Uri` values are ignored without any
  message.
- `fileInfo.Exists` accesses the file system on the UI thread, which can be slow for network paths.
- A relative path is resolved against the process's current directory.
- A file that does not exist when the source is set is not checked again, and no message is shown.

### 13. Exceptions are ignored without logging

The empty `catch { }` blocks around `cancellation.Cancel()` hide the `ObjectDisposedException` caused by #2.
They also hide exceptions thrown by user callbacks registered on the token. At least log them, as
`TextReaderWorker` does.

### 14. Project and assembly metadata

- `net6.0-windows` is no longer supported. The build warns that `System.IO.Hashing` 10.0.12 does not support
  it.
- `AssemblyInfo.cs` maps the `Domore.Windows` CLR namespace in `XmlnsDefinition`, but this assembly has no
  types in that namespace. The mapping is harmless but unused.
