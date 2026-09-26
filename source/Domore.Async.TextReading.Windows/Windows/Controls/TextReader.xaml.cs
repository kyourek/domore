using Domore.IO;
using Domore.Text;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace Domore.Windows.Controls;

partial class TextReader {
    static TextReader() {
        BackgroundProperty.OverrideMetadata(
            typeof(TextReader),
            new FrameworkPropertyMetadata(SystemColors.WindowBrush));
    }

    private TextReaderWorker Worker;
    private CancellationTokenSource Cancellation;

    private static IStreamText ConvertSource(object value) {
        if (value is IStreamText streamText) {
            return streamText;
        }
        if (value is string s) {
            if (!string.IsNullOrEmpty(s)) {
                if (!Path.GetInvalidPathChars().Any(c => s.Contains(c))) {
                    var fileInfo = new FileInfo(s);
                    if (fileInfo.Exists) {
                        return new StreamTextSourceFile(fileInfo);
                    }
                }
            }
        }
        return null;
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
        var cancellation = Cancellation;
        if (cancellation != null) {
            try {
                cancellation.Cancel();
            }
            catch {
            }
        }
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

    private async Task ClearTextForRefresh(CancellationToken cancellationToken) {
        try {
            await ClearText(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
        }
    }

    private async void Refresh() {
        var cancellation = Cancellation;
        if (cancellation != null) {
            try {
                cancellation.Cancel();
            }
            catch {
            }
        }
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
                if (!IsCurrentRefresh(worker, c)) {
                    return;
                }
                TextReaderLoading = false;
            }
            finally {
                if (ReferenceEquals(Cancellation, c)) {
                    Cancellation = null;
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

    private void OnTextReaderEncodingLabelStyleChanged(DependencyPropertyChangedEventArgs e) {
        var newValue = e.NewValue as Style;
        if (newValue is null) {
            var defaultValue = TryFindResource("TextReaderEncodingLabelStyleDefault") as Style;
            if (defaultValue is not null) {
                SetCurrentValue(TextReaderEncodingLabelStyleProperty, defaultValue);
            }
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

    public override void OnApplyTemplate() {
        base.OnApplyTemplate();
        if (TextReaderEncodingLabelStyle is null) {
            var defaultValue = TryFindResource("TextReaderEncodingLabelStyleDefault") as Style;
            if (defaultValue is not null) {
                SetCurrentValue(TextReaderEncodingLabelStyleProperty, defaultValue);
            }
        }
    }

    public static readonly RoutedEvent TextReaderSourceChangedEvent = EventManager.RegisterRoutedEvent(
        name: nameof(TextReaderSourceChanged),
        routingStrategy: RoutingStrategy.Bubble,
        handlerType: typeof(RoutedEventHandler),
        ownerType: typeof(TextReader));

    public static readonly RoutedEvent TextReaderSuccessChangedEvent = EventManager.RegisterRoutedEvent(
        name: nameof(TextReaderSuccessChanged),
        routingStrategy: RoutingStrategy.Bubble,
        handlerType: typeof(RoutedEventHandler),
        ownerType: typeof(TextReader));

    public static readonly RoutedEvent TextReaderLoadingChangedEvent = EventManager.RegisterRoutedEvent(
        name: nameof(TextReaderLoadingChanged),
        routingStrategy: RoutingStrategy.Bubble,
        handlerType: typeof(RoutedEventHandler),
        ownerType: typeof(TextReader));

    public static readonly RoutedEvent TextReaderEncodingChangedEvent = EventManager.RegisterRoutedEvent(
        name: nameof(TextReaderEncodingChanged),
        routingStrategy: RoutingStrategy.Bubble,
        handlerType: typeof(RoutedEventHandler),
        ownerType: typeof(TextReader));

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

    public static readonly DependencyProperty TextReaderEncodingLabelStyleProperty = DependencyProperty.Register(
        name: nameof(TextReaderEncodingLabelStyle),
        propertyType: typeof(Style),
        ownerType: typeof(TextReader),
        typeMetadata: new PropertyMetadata(
            defaultValue: null,
            propertyChangedCallback: (s, e) => {
                if (s is TextReader self) {
                    self.OnTextReaderEncodingLabelStyleChanged(e);
                }
            }));

    public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty =
        ScrollViewer.HorizontalScrollBarVisibilityProperty.AddOwner(
            typeof(TextReader),
            new FrameworkPropertyMetadata(ScrollBarVisibility.Auto));

    public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
        ScrollViewer.VerticalScrollBarVisibilityProperty.AddOwner(
            typeof(TextReader),
            new FrameworkPropertyMetadata(ScrollBarVisibility.Auto));

    public static readonly DependencyProperty TextWrappingProperty =
        TextBox.TextWrappingProperty.AddOwner(typeof(TextReader));

    public static readonly DependencyProperty SelectionTextBrushProperty =
        TextBoxBase.SelectionTextBrushProperty.AddOwner(typeof(TextReader));

    public static readonly DependencyProperty TextReaderSuccessProperty =
        TextReaderSuccessPropertyKey.DependencyProperty;

    public static readonly DependencyProperty TextReaderLoadingProperty =
        TextReaderLoadingPropertyKey.DependencyProperty;

    public static readonly DependencyProperty TextReaderEncodingProperty =
        TextReaderEncodingPropertyKey.DependencyProperty;

    public event RoutedEventHandler TextReaderSourceChanged {
        add => AddHandler(TextReaderSourceChangedEvent, value);
        remove => RemoveHandler(TextReaderSourceChangedEvent, value);
    }

    public event RoutedEventHandler TextReaderSuccessChanged {
        add => AddHandler(TextReaderSuccessChangedEvent, value);
        remove => RemoveHandler(TextReaderSuccessChangedEvent, value);
    }

    public event RoutedEventHandler TextReaderLoadingChanged {
        add => AddHandler(TextReaderLoadingChangedEvent, value);
        remove => RemoveHandler(TextReaderLoadingChangedEvent, value);
    }

    public event RoutedEventHandler TextReaderEncodingChanged {
        add => AddHandler(TextReaderEncodingChangedEvent, value);
        remove => RemoveHandler(TextReaderEncodingChangedEvent, value);
    }

    public object TextReaderSource {
        get => GetValue(TextReaderSourceProperty);
        set => SetValue(TextReaderSourceProperty, value);
    }

    public bool TextReaderEnabled {
        get => GetValue(TextReaderEnabledProperty) as bool? ?? true;
        set => SetValue(TextReaderEnabledProperty, value);
    }

    public long? TextReaderSourceLengthMax {
        get => GetValue(TextReaderSourceLengthMaxProperty) as long?;
        set => SetValue(TextReaderSourceLengthMaxProperty, value);
    }

    public DecodedTextOptions TextReaderOptions {
        get => GetValue(TextReaderOptionsProperty) as DecodedTextOptions;
        set => SetValue(TextReaderOptionsProperty, value);
    }

    public bool TextReaderSuccess {
        get => GetValue(TextReaderSuccessProperty) as bool? ?? false;
        private set => SetValue(TextReaderSuccessPropertyKey, value);
    }

    public bool TextReaderLoading {
        get => GetValue(TextReaderLoadingProperty) as bool? ?? false;
        private set => SetValue(TextReaderLoadingPropertyKey, value);
    }

    public string TextReaderEncoding {
        get => GetValue(TextReaderEncodingProperty) as string;
        private set => SetValue(TextReaderEncodingPropertyKey, value);
    }

    public Style TextReaderEncodingLabelStyle {
        get => GetValue(TextReaderEncodingLabelStyleProperty) as Style;
        set => SetValue(TextReaderEncodingLabelStyleProperty, value);
    }

    public ScrollBarVisibility HorizontalScrollBarVisibility {
        get => (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty);
        set => SetValue(HorizontalScrollBarVisibilityProperty, value);
    }

    public ScrollBarVisibility VerticalScrollBarVisibility {
        get => (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty);
        set => SetValue(VerticalScrollBarVisibilityProperty, value);
    }

    public TextWrapping TextWrapping {
        get => (TextWrapping)GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    public Brush SelectionTextBrush {
        get => GetValue(SelectionTextBrushProperty) as Brush;
        set => SetValue(SelectionTextBrushProperty, value);
    }

    public TextReader() {
        Loaded += This_Loaded;
        Unloaded += This_Unloaded;
        InitializeComponent();
    }
}
