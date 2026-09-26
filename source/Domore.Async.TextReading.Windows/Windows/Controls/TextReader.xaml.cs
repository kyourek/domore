using Domore.IO;
using Domore.Logs;
using Domore.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace Domore.Windows.Controls;

/// <summary>
/// A WPF control that asynchronously decodes and displays text from a stream or file.
/// </summary>
partial class TextReader {
    private static readonly ILog Log = Logging.For(typeof(TextReader));
    private static readonly Style TextReaderEncodingLabelStyleDefault = CreateTextReaderEncodingLabelStyleDefault();
    private static readonly StreamTextProvider Provider = new();

    private static Style CreateTextReaderEncodingLabelStyleDefault() {
        var
        style = new Style(typeof(Label));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        style.Setters.Add(new Setter(UIElement.FocusableProperty, false));
        style.Setters.Add(new Setter(
            Control.FontFamilyProperty,
            new Binding(nameof(FontFamily)) {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(TextReader), 1)
            }));
        style.Setters.Add(new Setter(
            Control.FontSizeProperty,
            new Binding(nameof(FontSize)) {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(TextReader), 1)
            }));
        return style;
    }

    static TextReader() {
        BackgroundProperty.OverrideMetadata(
            typeof(TextReader),
            new FrameworkPropertyMetadata(SystemColors.WindowBrush));
        // The inner text box takes focus, so the control itself is not a separate tab stop.
        FocusableProperty.OverrideMetadata(
            typeof(TextReader),
            new FrameworkPropertyMetadata(false));
    }

    private TextReaderWorker Worker;
    private CancellationTokenSource Cancellation;

    private static IStreamText ConvertSource(object value) {
        return Provider.GetStreamingText(value);
    }

    private static readonly DependencyPropertyKey TextReaderSuccessPropertyKey = DependencyProperty.RegisterReadOnly(
        name: nameof(TextReaderSuccess),
        propertyType: typeof(bool),
        ownerType: typeof(TextReader),
        typeMetadata: new PropertyMetadata(
            defaultValue: false,
            propertyChangedCallback: (s, e) => {
                if (s is TextReader self) {
                    self.OnTextReaderSuccessChanged(e);
                }
            }));

    private static readonly DependencyPropertyKey TextReaderLoadingPropertyKey = DependencyProperty.RegisterReadOnly(
        name: nameof(TextReaderLoading),
        propertyType: typeof(bool),
        ownerType: typeof(TextReader),
        typeMetadata: new PropertyMetadata(
            defaultValue: false,
            propertyChangedCallback: (s, e) => {
                if (s is TextReader self) {
                    self.OnTextReaderLoadingChanged(e);
                }
            }));

    private static readonly DependencyPropertyKey TextReaderEncodingPropertyKey = DependencyProperty.RegisterReadOnly(
        name: nameof(TextReaderEncoding),
        propertyType: typeof(string),
        ownerType: typeof(TextReader),
        typeMetadata: new PropertyMetadata(
            defaultValue: null,
            propertyChangedCallback: (s, e) => {
                if (s is TextReader self) {
                    self.OnTextReaderEncodingChanged(e);
                }
            }));

    private void This_Unloaded(object sender, RoutedEventArgs e) {
        Worker = null;
        Cancel(Cancellation);
    }

    private void This_Loaded(object sender, RoutedEventArgs e) {
        Worker = new TextReaderWorker {
            Enabled = TextReaderEnabled,
            Source = ConvertSource(TextReaderSource),
            SourceLengthMax = TextReaderSourceLengthMax,
            Options = TextReaderOptions
        };
        Refresh();
    }

    private bool IsCurrentRefresh(TextReaderWorker worker, CancellationTokenSource cancellation) {
        return ReferenceEquals(Worker, worker) &&
               ReferenceEquals(Cancellation, cancellation) &&
               !cancellation.IsCancellationRequested;
    }

    private static void Cancel(CancellationTokenSource cancellation) {
        if (cancellation is null) {
            return;
        }
        try {
            cancellation.Cancel();
        }
        catch (ObjectDisposedException ex) {
            if (Log.Debug()) {
                Log.Debug($"{nameof(cancellation)}[{nameof(ObjectDisposedException)}]", ex);
            }
        }
        catch (AggregateException ex) {
            if (Log.Warn()) {
                Log.Warn($"{nameof(cancellation)}[callback]", ex);
            }
        }
    }

    private async Task ClearTextForRefresh(CancellationToken cancellationToken) {
        try {
            await ClearText(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
        }
    }

    private async void Refresh() {
        Cancel(Cancellation);
        var worker = Worker;
        if (worker is null) {
            return;
        }
        using (var c = Cancellation = new CancellationTokenSource()) {
            try {
                TextReaderLoading = true;
                if (!IsCurrentRefresh(worker, c)) {
                    return;
                }
                TextReaderSuccess = false;
                if (!IsCurrentRefresh(worker, c)) {
                    return;
                }
                await ClearTextForRefresh(c.Token);
                if (!IsCurrentRefresh(worker, c)) {
                    return;
                }
                var decoded = await worker.Refresh(
                    builder: new TextReaderTextBuilder(this),
                    cancellationToken: c.Token);
                if (!IsCurrentRefresh(worker, c)) {
                    return;
                }
                if (decoded?.Success != true) {
                    await ClearTextForRefresh(c.Token);
                    if (!IsCurrentRefresh(worker, c)) {
                        return;
                    }
                }
                TextReaderSuccess = decoded?.Success == true;
                if (!IsCurrentRefresh(worker, c)) {
                    return;
                }
                TextReaderEncoding = decoded?.EncodingName;
            }
            finally {
                // Reset loading unless a newer refresh took over, including after cancellation by Unloaded.
                if (ReferenceEquals(Cancellation, c)) {
                    Cancellation = null;
                    TextReaderLoading = false;
                }
            }
        }
    }

    private void OnTextReaderSourceChanged(DependencyPropertyChangedEventArgs e) {
        var worker = Worker;
        if (worker is not null) {
            worker.Source = ConvertSource(e.NewValue);
        }
        var a = new RoutedEventArgs(TextReaderSourceChangedEvent);
        RaiseEvent(a);
        Refresh();
    }

    private void OnTextReaderEncodingChanged(DependencyPropertyChangedEventArgs e) {
        var a = new RoutedEventArgs(TextReaderEncodingChangedEvent);
        RaiseEvent(a);
    }

    private void OnTextReaderSuccessChanged(DependencyPropertyChangedEventArgs e) {
        var a = new RoutedEventArgs(TextReaderSuccessChangedEvent);
        RaiseEvent(a);
    }

    private void OnTextReaderLoadingChanged(DependencyPropertyChangedEventArgs e) {
        var a = new RoutedEventArgs(TextReaderLoadingChangedEvent);
        RaiseEvent(a);
    }

    private void OnTextReaderOptionsChanged(DependencyPropertyChangedEventArgs e) {
        var worker = Worker;
        if (worker is not null) {
            worker.Options = TextReaderOptions;
            Refresh();
        }
    }

    private void OnTextReaderEnabledChanged(DependencyPropertyChangedEventArgs e) {
        var worker = Worker;
        if (worker is not null) {
            worker.Enabled = TextReaderEnabled;
            Refresh();
        }
    }

    private void OnTextReaderSourceLengthMaxChanged(DependencyPropertyChangedEventArgs e) {
        var worker = Worker;
        if (worker is not null) {
            worker.SourceLengthMax = TextReaderSourceLengthMax;
            Refresh();
        }
    }

    internal async Task AddText(ReadOnlyMemory<char> memory, CancellationToken cancellationToken) {
        var s = new string(memory.Span);
        await Dispatcher.InvokeAsync(
            cancellationToken: cancellationToken,
            priority: DispatcherPriority.Background,
            callback: () => {
                var textBox = GetTemplateChild("PART_TextBox") as TextBox;
                if (textBox is not null) {
                    textBox.AppendText(s);
                }
            });
    }

    internal async Task ClearText(CancellationToken cancellationToken) {
        await Dispatcher.InvokeAsync(
            cancellationToken: cancellationToken,
            priority: DispatcherPriority.Background,
            callback: () => {
                var textBox = GetTemplateChild("PART_TextBox") as TextBox;
                if (textBox is not null) {
                    textBox.Clear();
                }
            });
    }

    /// <summary>
    /// Identifies the bubbling event raised when <see cref="TextReaderSource"/> changes.
    /// </summary>
    public static readonly RoutedEvent TextReaderSourceChangedEvent = EventManager.RegisterRoutedEvent(
        name: nameof(TextReaderSourceChanged),
        routingStrategy: RoutingStrategy.Bubble,
        handlerType: typeof(RoutedEventHandler),
        ownerType: typeof(TextReader));

    /// <summary>
    /// Identifies the bubbling event raised when <see cref="TextReaderSuccess"/> changes.
    /// </summary>
    public static readonly RoutedEvent TextReaderSuccessChangedEvent = EventManager.RegisterRoutedEvent(
        name: nameof(TextReaderSuccessChanged),
        routingStrategy: RoutingStrategy.Bubble,
        handlerType: typeof(RoutedEventHandler),
        ownerType: typeof(TextReader));

    /// <summary>
    /// Identifies the bubbling event raised when <see cref="TextReaderLoading"/> changes.
    /// </summary>
    public static readonly RoutedEvent TextReaderLoadingChangedEvent = EventManager.RegisterRoutedEvent(
        name: nameof(TextReaderLoadingChanged),
        routingStrategy: RoutingStrategy.Bubble,
        handlerType: typeof(RoutedEventHandler),
        ownerType: typeof(TextReader));

    /// <summary>
    /// Identifies the bubbling event raised when <see cref="TextReaderEncoding"/> changes.
    /// </summary>
    public static readonly RoutedEvent TextReaderEncodingChangedEvent = EventManager.RegisterRoutedEvent(
        name: nameof(TextReaderEncodingChanged),
        routingStrategy: RoutingStrategy.Bubble,
        handlerType: typeof(RoutedEventHandler),
        ownerType: typeof(TextReader));

    /// <summary>
    /// Identifies the <see cref="TextReaderSource"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty TextReaderSourceProperty = DependencyProperty.Register(
        name: nameof(TextReaderSource),
        propertyType: typeof(object),
        ownerType: typeof(TextReader),
        typeMetadata: new PropertyMetadata(
            defaultValue: null,
            propertyChangedCallback: (s, e) => {
                if (s is TextReader self) {
                    self.OnTextReaderSourceChanged(e);
                }
            }));

    /// <summary>
    /// Identifies the <see cref="TextReaderEnabled"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty TextReaderEnabledProperty = DependencyProperty.Register(
        name: nameof(TextReaderEnabled),
        propertyType: typeof(bool),
        ownerType: typeof(TextReader),
        typeMetadata: new PropertyMetadata(
            defaultValue: true,
            propertyChangedCallback: (s, e) => {
                if (s is TextReader self) {
                    self.OnTextReaderEnabledChanged(e);
                }
            }));

    /// <summary>
    /// Identifies the <see cref="TextReaderSourceLengthMax"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty TextReaderSourceLengthMaxProperty = DependencyProperty.Register(
        name: nameof(TextReaderSourceLengthMax),
        propertyType: typeof(long?),
        ownerType: typeof(TextReader),
        typeMetadata: new PropertyMetadata(
            defaultValue: default(long?),
            propertyChangedCallback: (s, e) => {
                if (s is TextReader self) {
                    self.OnTextReaderSourceLengthMaxChanged(e);
                }
            }));

    /// <summary>
    /// Identifies the <see cref="TextReaderOptions"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty TextReaderOptionsProperty = DependencyProperty.Register(
        name: nameof(TextReaderOptions),
        propertyType: typeof(DecodedTextOptions),
        ownerType: typeof(TextReader),
        typeMetadata: new PropertyMetadata(
            defaultValue: null,
            propertyChangedCallback: (s, e) => {
                if (s is TextReader self) {
                    self.OnTextReaderOptionsChanged(e);
                }
            }));

    /// <summary>
    /// Identifies the <see cref="TextReaderEncodingLabelStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty TextReaderEncodingLabelStyleProperty = DependencyProperty.Register(
        name: nameof(TextReaderEncodingLabelStyle),
        propertyType: typeof(Style),
        ownerType: typeof(TextReader),
        typeMetadata: new PropertyMetadata(defaultValue: TextReaderEncodingLabelStyleDefault));

    /// <summary>
    /// Identifies the <see cref="HorizontalScrollBarVisibility"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty =
        ScrollViewer.HorizontalScrollBarVisibilityProperty.AddOwner(
            typeof(TextReader),
            new FrameworkPropertyMetadata(ScrollBarVisibility.Auto));

    /// <summary>
    /// Identifies the <see cref="VerticalScrollBarVisibility"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
        ScrollViewer.VerticalScrollBarVisibilityProperty.AddOwner(
            typeof(TextReader),
            new FrameworkPropertyMetadata(ScrollBarVisibility.Auto));

    /// <summary>
    /// Identifies the <see cref="TextWrapping"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty TextWrappingProperty =
        TextBox.TextWrappingProperty.AddOwner(typeof(TextReader));

    /// <summary>
    /// Identifies the <see cref="SelectionTextBrush"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty SelectionTextBrushProperty =
        TextBoxBase.SelectionTextBrushProperty.AddOwner(typeof(TextReader));

    /// <summary>
    /// Identifies the read-only <see cref="TextReaderSuccess"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty TextReaderSuccessProperty =
        TextReaderSuccessPropertyKey.DependencyProperty;

    /// <summary>
    /// Identifies the read-only <see cref="TextReaderLoading"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty TextReaderLoadingProperty =
        TextReaderLoadingPropertyKey.DependencyProperty;

    /// <summary>
    /// Identifies the read-only <see cref="TextReaderEncoding"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty TextReaderEncodingProperty =
        TextReaderEncodingPropertyKey.DependencyProperty;

    /// <summary>
    /// Occurs when the source value changes.
    /// </summary>
    public event RoutedEventHandler TextReaderSourceChanged {
        add => AddHandler(TextReaderSourceChangedEvent, value);
        remove => RemoveHandler(TextReaderSourceChangedEvent, value);
    }

    /// <summary>
    /// Occurs when the success status changes.
    /// </summary>
    public event RoutedEventHandler TextReaderSuccessChanged {
        add => AddHandler(TextReaderSuccessChangedEvent, value);
        remove => RemoveHandler(TextReaderSuccessChangedEvent, value);
    }

    /// <summary>
    /// Occurs when the loading status changes.
    /// </summary>
    public event RoutedEventHandler TextReaderLoadingChanged {
        add => AddHandler(TextReaderLoadingChangedEvent, value);
        remove => RemoveHandler(TextReaderLoadingChangedEvent, value);
    }

    /// <summary>
    /// Occurs when the detected text encoding changes.
    /// </summary>
    public event RoutedEventHandler TextReaderEncodingChanged {
        add => AddHandler(TextReaderEncodingChangedEvent, value);
        remove => RemoveHandler(TextReaderEncodingChangedEvent, value);
    }

    /// <summary>
    /// Gets or sets the source to decode. Accepted values are an <see cref="IStreamText"/>, a file
    /// path, a <see cref="FileInfo"/>, or a local file <see cref="Uri"/>. Relative string paths are
    /// resolved against the application directory. For unsupported values, non-file URIs, and invalid
    /// paths, null is returned as the source, so nothing is decoded.
    /// </summary>
    public object TextReaderSource {
        get => GetValue(TextReaderSourceProperty);
        set => SetValue(TextReaderSourceProperty, value);
    }

    /// <summary>
    /// Reloads the current source. A retained file source is checked again, including if it did not
    /// exist during an earlier load.
    /// </summary>
    public void Reload() {
        Refresh();
    }

    /// <summary>
    /// Gets or sets whether the current source is decoded.
    /// </summary>
    public bool TextReaderEnabled {
        get => GetValue(TextReaderEnabledProperty) as bool? ?? true;
        set => SetValue(TextReaderEnabledProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum source length in bytes. A null value imposes no length limit.
    /// </summary>
    public long? TextReaderSourceLengthMax {
        get => GetValue(TextReaderSourceLengthMaxProperty) as long?;
        set => SetValue(TextReaderSourceLengthMaxProperty, value);
    }

    /// <summary>
    /// Gets or sets the options used to decode the current source. Null selects the default options.
    /// </summary>
    public DecodedTextOptions TextReaderOptions {
        get => GetValue(TextReaderOptionsProperty) as DecodedTextOptions;
        set => SetValue(TextReaderOptionsProperty, value);
    }

    /// <summary>
    /// Gets a value indicating whether the current source was decoded successfully.
    /// </summary>
    public bool TextReaderSuccess {
        get => GetValue(TextReaderSuccessProperty) as bool? ?? false;
        private set => SetValue(TextReaderSuccessPropertyKey, value);
    }

    /// <summary>
    /// Gets a value indicating whether the current source is being decoded.
    /// </summary>
    public bool TextReaderLoading {
        get => GetValue(TextReaderLoadingProperty) as bool? ?? false;
        private set => SetValue(TextReaderLoadingPropertyKey, value);
    }

    /// <summary>
    /// Gets the encoding detected for the current source, or null if no encoding is available.
    /// </summary>
    public string TextReaderEncoding {
        get => GetValue(TextReaderEncodingProperty) as string;
        private set => SetValue(TextReaderEncodingPropertyKey, value);
    }

    /// <summary>
    /// Gets or sets the style applied to the detected-encoding label. Setting this to null uses
    /// the normal <see cref="Label"/> style.
    /// </summary>
    public Style TextReaderEncodingLabelStyle {
        get => GetValue(TextReaderEncodingLabelStyleProperty) as Style;
        set => SetValue(TextReaderEncodingLabelStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the horizontal scroll bar visibility of the text display.
    /// </summary>
    public ScrollBarVisibility HorizontalScrollBarVisibility {
        get => (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty);
        set => SetValue(HorizontalScrollBarVisibilityProperty, value);
    }

    /// <summary>
    /// Gets or sets the vertical scroll bar visibility of the text display.
    /// </summary>
    public ScrollBarVisibility VerticalScrollBarVisibility {
        get => (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty);
        set => SetValue(VerticalScrollBarVisibilityProperty, value);
    }

    /// <summary>
    /// Gets or sets how text is wrapped in the text display.
    /// </summary>
    public TextWrapping TextWrapping {
        get => (TextWrapping)GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    /// <summary>
    /// Gets or sets the brush used to draw selected text.
    /// </summary>
    public Brush SelectionTextBrush {
        get => GetValue(SelectionTextBrushProperty) as Brush;
        set => SetValue(SelectionTextBrushProperty, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextReader"/> control.
    /// </summary>
    public TextReader() {
        Loaded += This_Loaded;
        Unloaded += This_Unloaded;
        InitializeComponent();
    }
}
