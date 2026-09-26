using Domore.IO;
using Domore.Text;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Domore.Windows.DispatcherTest;

namespace Domore.Windows.Controls;

[TestFixture]
public class TextReaderTest {
    private static readonly byte[] Latin1Cafe = [0x63, 0x61, 0x66, 0xE9];

    private readonly List<string> Files = [];

    private string CreateFile(string text, string directory = null) {
        var path = Path.Combine(directory ?? Path.GetTempPath(), $"{nameof(TextReaderTest)}_{Guid.NewGuid():N}.txt");
        if (text is not null) {
            File.WriteAllText(path, text);
        }
        Files.Add(path);
        return path;
    }

    [TearDown]
    public void TearDown() {
        foreach (var file in Files) {
            if (File.Exists(file)) {
                File.Delete(file);
            }
        }
        Files.Clear();
    }

    private static TextBox TextBox(TextReader subject) =>
        (TextBox)subject.Template.FindName("PART_TextBox", subject);

    private static Label Label(TextReader subject) =>
        (Label)subject.Template.FindName("PART_Label", subject);

    private static async Task<Window> Show(TextReader subject) {
        var window = new Window {
            Content = subject,
            Width = 400,
            Height = 300,
            Left = -20000,
            Top = -20000,
            ShowActivated = false,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.Manual
        };
        window.Show();
        await Until(() => subject.IsLoaded, "the control to load");
        return window;
    }

    private static async Task Settled(TextReader subject) {
        await Until(() => !subject.TextReaderLoading, "loading to finish");
    }

    private void Run(Func<TextReader, Task> test) {
        DispatcherTest.Run(async () => {
            var subject = new TextReader();
            var window = await Show(subject);
            await Settled(subject);
            try {
                await test(subject);
            }
            finally {
                window.Close();
            }
        });
    }

    [Test]
    public void Constructor_HasExpectedDefaults() {
        DispatcherTest.Run(() => {
            var subject = new TextReader();
            Assert.Multiple(() => {
                Assert.That(subject.TextReaderSource, Is.Null);
                Assert.That(subject.TextReaderEnabled, Is.True);
                Assert.That(subject.TextReaderSourceLengthMax, Is.Null);
                Assert.That(subject.TextReaderOptions, Is.Null);
                Assert.That(subject.TextReaderSuccess, Is.False);
                Assert.That(subject.TextReaderLoading, Is.False);
                Assert.That(subject.TextReaderEncoding, Is.Null);
                Assert.That(subject.Background, Is.EqualTo(SystemColors.WindowBrush));
                Assert.That(subject.HorizontalScrollBarVisibility, Is.EqualTo(ScrollBarVisibility.Auto));
                Assert.That(subject.VerticalScrollBarVisibility, Is.EqualTo(ScrollBarVisibility.Auto));
            });
            return Task.CompletedTask;
        });
    }

    [Test]
    public void TextReaderEncodingLabelStyle_HasDefaultStyle() {
        DispatcherTest.Run(() => {
            var style = new TextReader().TextReaderEncodingLabelStyle;
            Assert.That(style, Is.Not.Null);
            Assert.That(style.TargetType, Is.EqualTo(typeof(Label)));
            Assert.That(style.Setters, Has.Count.EqualTo(4));
            return Task.CompletedTask;
        });
    }

    [Test]
    public void Focusable_IsFalseSoOnlyTheTextBoxIsATabStop() {
        Run(subject => {
            Assert.That(subject.Focusable, Is.False);
            Assert.That(TextBox(subject).Focusable, Is.True);
            return Task.CompletedTask;
        });
    }

    [Test]
    public void IsTabStop_IsAppliedToTheTextBox() {
        Run(subject => {
            subject.IsTabStop = false;
            Assert.That(TextBox(subject).IsTabStop, Is.False);
            return Task.CompletedTask;
        });
    }

    [Test]
    public void TextBox_IsReadOnly() {
        Run(subject => {
            Assert.That(TextBox(subject).IsReadOnly, Is.True);
            return Task.CompletedTask;
        });
    }

    [Test]
    public void TextReaderSource_LoadsFilePath() {
        var path = CreateFile("hello from a path");
        Run(async subject => {
            subject.TextReaderSource = path;
            await Settled(subject);
            Assert.That(TextBox(subject).Text, Is.EqualTo("hello from a path"));
            Assert.That(subject.TextReaderSuccess, Is.True);
            Assert.That(subject.TextReaderEncoding, Is.Not.Null);
        });
    }

    [Test]
    public void TextReaderSource_ResolvesRelativePathAgainstApplicationDirectory() {
        var path = CreateFile("hello from a relative path", AppContext.BaseDirectory);
        Run(async subject => {
            subject.TextReaderSource = Path.GetFileName(path);
            await Settled(subject);
            Assert.That(TextBox(subject).Text, Is.EqualTo("hello from a relative path"));
            Assert.That(subject.TextReaderSuccess, Is.True);
        });
    }

    [Test]
    public void TextReaderSource_LoadsFileInfo() {
        var path = CreateFile("hello from a FileInfo");
        Run(async subject => {
            subject.TextReaderSource = new FileInfo(path);
            await Settled(subject);
            Assert.That(TextBox(subject).Text, Is.EqualTo("hello from a FileInfo"));
        });
    }

    [Test]
    public void TextReaderSource_LoadsFileUri() {
        var path = CreateFile("hello from a Uri");
        Run(async subject => {
            subject.TextReaderSource = new Uri(path);
            await Settled(subject);
            Assert.That(TextBox(subject).Text, Is.EqualTo("hello from a Uri"));
        });
    }

    [Test]
    public void TextReaderSource_LoadsStreamText() {
        Run(async subject => {
            subject.TextReaderSource = new TestStreamText("hello from a stream");
            await Settled(subject);
            Assert.That(TextBox(subject).Text, Is.EqualTo("hello from a stream"));
            Assert.That(subject.TextReaderSuccess, Is.True);
        });
    }

    [Test]
    public void TextReaderSource_LoadsSourceSetBeforeLoaded() {
        var path = CreateFile("set before loaded");
        DispatcherTest.Run(async () => {
            var subject = new TextReader { TextReaderSource = path };
            var window = await Show(subject);
            try {
                await Settled(subject);
                Assert.That(TextBox(subject).Text, Is.EqualTo("set before loaded"));
            }
            finally {
                window.Close();
            }
        });
    }

    [TestCase(42)]
    [TestCase("   ")]
    public void TextReaderSource_UnsupportedValueLoadsNothing(object value) {
        Run(async subject => {
            subject.TextReaderSource = value;
            await Settled(subject);
            Assert.That(TextBox(subject).Text, Is.Empty);
            Assert.That(subject.TextReaderSuccess, Is.False);
            Assert.That(subject.TextReaderEncoding, Is.Null);
        });
    }

    [Test]
    public void TextReaderSource_NonFileUriLoadsNothing() {
        Run(async subject => {
            subject.TextReaderSource = new Uri("https://example.com/file.txt");
            await Settled(subject);
            Assert.That(TextBox(subject).Text, Is.Empty);
            Assert.That(subject.TextReaderSuccess, Is.False);
        });
    }

    [Test]
    public void TextReaderSource_ChangeReplacesText() {
        Run(async subject => {
            subject.TextReaderSource = new TestStreamText("first");
            await Settled(subject);
            subject.TextReaderSource = new TestStreamText("second");
            await Settled(subject);
            Assert.That(TextBox(subject).Text, Is.EqualTo("second"));
        });
    }

    [Test]
    public void TextReaderSource_NullClearsText() {
        Run(async subject => {
            subject.TextReaderSource = new TestStreamText("some text");
            await Settled(subject);
            subject.TextReaderSource = null;
            await Settled(subject);
            Assert.That(TextBox(subject).Text, Is.Empty);
            Assert.That(subject.TextReaderSuccess, Is.False);
            Assert.That(subject.TextReaderEncoding, Is.Null);
        });
    }

    [Test]
    public void TextReaderSource_FailedDecodeClearsText() {
        Run(async subject => {
            subject.TextReaderSource = new TestStreamText("some text");
            await Settled(subject);
            subject.TextReaderSource = new TestStreamText(Latin1Cafe);
            await Settled(subject);
            Assert.That(TextBox(subject).Text, Is.Empty);
            Assert.That(subject.TextReaderSuccess, Is.False);
            Assert.That(subject.TextReaderEncoding, Is.Null);
        });
    }

    [Test]
    public void TextReaderSource_StreamExceptionLoadsNothing() {
        Run(async subject => {
            subject.TextReaderSource = new TestStreamText("x") { StreamException = new IOException() };
            await Settled(subject);
            Assert.That(TextBox(subject).Text, Is.Empty);
            Assert.That(subject.TextReaderSuccess, Is.False);
        });
    }

    [Test]
    public void TextReaderSource_ChangeCancelsPreviousLoad() {
        Run(async subject => {
            var slow = new TestStreamText("slow") { WaitUntilCanceled = true };
            subject.TextReaderSource = slow;
            Assert.That(subject.TextReaderLoading, Is.True);
            await Until(() => slow.ReadyStarted.IsCompleted, "the slow load to start");
            subject.TextReaderSource = new TestStreamText("fast");
            await Settled(subject);
            await Until(() => slow.ReadyCanceled.IsCompleted, "the slow load to be canceled");
            Assert.That(TextBox(subject).Text, Is.EqualTo("fast"));
            Assert.That(subject.TextReaderSuccess, Is.True);
        });
    }

    [Test]
    public void Reload_ReadsFileCreatedAfterFirstLoad() {
        var path = CreateFile(null);
        Run(async subject => {
            subject.TextReaderSource = path;
            await Settled(subject);
            Assert.That(subject.TextReaderSuccess, Is.False);
            File.WriteAllText(path, "created later");
            subject.Reload();
            await Settled(subject);
            Assert.That(TextBox(subject).Text, Is.EqualTo("created later"));
            Assert.That(subject.TextReaderSuccess, Is.True);
        });
    }

    [Test]
    public void Reload_ReadsChangedFile() {
        var path = CreateFile("before");
        Run(async subject => {
            subject.TextReaderSource = path;
            await Settled(subject);
            File.WriteAllText(path, "after");
            subject.Reload();
            await Settled(subject);
            Assert.That(TextBox(subject).Text, Is.EqualTo("after"));
        });
    }

    [Test]
    public void TextReaderEnabled_FalsePreventsLoading() {
        Run(async subject => {
            subject.TextReaderEnabled = false;
            var source = new TestStreamText("disabled");
            subject.TextReaderSource = source;
            await Settled(subject);
            Assert.That(TextBox(subject).Text, Is.Empty);
            Assert.That(subject.TextReaderSuccess, Is.False);
            Assert.That(source.StreamTextCalls, Is.Zero);
        });
    }

    [Test]
    public void TextReaderEnabled_TrueLoadsCurrentSource() {
        Run(async subject => {
            subject.TextReaderEnabled = false;
            subject.TextReaderSource = new TestStreamText("enabled later");
            await Settled(subject);
            subject.TextReaderEnabled = true;
            await Settled(subject);
            Assert.That(TextBox(subject).Text, Is.EqualTo("enabled later"));
        });
    }

    [Test]
    public void TextReaderEnabled_FalseClearsText() {
        Run(async subject => {
            subject.TextReaderSource = new TestStreamText("some text");
            await Settled(subject);
            subject.TextReaderEnabled = false;
            await Settled(subject);
            Assert.That(TextBox(subject).Text, Is.Empty);
            Assert.That(subject.TextReaderSuccess, Is.False);
        });
    }

    [Test]
    public void TextReaderSourceLengthMax_PreventsLoadingLargerSource() {
        var path = CreateFile("0123456789");
        Run(async subject => {
            subject.TextReaderSourceLengthMax = 5;
            subject.TextReaderSource = path;
            await Settled(subject);
            Assert.That(TextBox(subject).Text, Is.Empty);
            Assert.That(subject.TextReaderSuccess, Is.False);
        });
    }

    [Test]
    public void TextReaderSourceLengthMax_ChangeReloadsSource() {
        var path = CreateFile("0123456789");
        Run(async subject => {
            subject.TextReaderSourceLengthMax = 5;
            subject.TextReaderSource = path;
            await Settled(subject);
            subject.TextReaderSourceLengthMax = 10;
            await Settled(subject);
            Assert.That(TextBox(subject).Text, Is.EqualTo("0123456789"));
            Assert.That(subject.TextReaderSuccess, Is.True);
        });
    }

    [Test]
    public void TextReaderOptions_AreUsedToDecode() {
        Run(async subject => {
            subject.TextReaderSource = new TestStreamText(Latin1Cafe);
            await Settled(subject);
            Assert.That(subject.TextReaderSuccess, Is.False);
            subject.TextReaderOptions = new DecodedTextOptions { Encoding = ["iso-8859-1"] };
            await Settled(subject);
            Assert.That(TextBox(subject).Text, Is.EqualTo("café"));
            Assert.That(subject.TextReaderSuccess, Is.True);
        });
    }

    [Test]
    public void Label_IsCollapsedWithoutEncodingAndShowsEncodingAfterLoad() {
        Run(async subject => {
            Assert.That(Label(subject).Visibility, Is.EqualTo(Visibility.Collapsed));
            subject.TextReaderSource = new TestStreamText("hello");
            await Settled(subject);
            Assert.That(Label(subject).Visibility, Is.EqualTo(Visibility.Visible));
            Assert.That(Label(subject).Content, Is.EqualTo(subject.TextReaderEncoding));
        });
    }

    [Test]
    public void Unloaded_CancelsLoadAndResetsLoading() {
        DispatcherTest.Run(async () => {
            var subject = new TextReader();
            var window = await Show(subject);
            try {
                await Settled(subject);
                var slow = new TestStreamText("slow") { WaitUntilCanceled = true };
                subject.TextReaderSource = slow;
                Assert.That(subject.TextReaderLoading, Is.True);
                await Until(() => slow.ReadyStarted.IsCompleted, "the load to start");
                window.Content = null;
                await Until(() => slow.ReadyCanceled.IsCompleted, "the load to be canceled");
                await Settled(subject);
                Assert.That(subject.TextReaderSuccess, Is.False);
            }
            finally {
                window.Close();
            }
        });
    }

    [Test]
    public void Loaded_AgainReloadsSource() {
        var path = CreateFile("before unload");
        DispatcherTest.Run(async () => {
            var subject = new TextReader { TextReaderSource = path };
            var window = await Show(subject);
            try {
                await Settled(subject);
                window.Content = null;
                await Until(() => !subject.IsLoaded, "the control to unload");
                File.WriteAllText(path, "after reload");
                window.Content = subject;
                await Until(() => subject.IsLoaded, "the control to load again");
                await Settled(subject);
                Assert.That(TextBox(subject).Text, Is.EqualTo("after reload"));
            }
            finally {
                window.Close();
            }
        });
    }

    [Test]
    public void Events_AreRaisedForStatusChanges() {
        Run(async subject => {
            var raised = new List<RoutedEvent>();
            void Handler(object sender, RoutedEventArgs e) => raised.Add(e.RoutedEvent);
            subject.TextReaderSourceChanged += Handler;
            subject.TextReaderLoadingChanged += Handler;
            subject.TextReaderSuccessChanged += Handler;
            subject.TextReaderEncodingChanged += Handler;

            subject.TextReaderSource = new TestStreamText("hello");
            await Settled(subject);

            Assert.That(raised, Is.EqualTo(new[] {
                TextReader.TextReaderSourceChangedEvent,
                TextReader.TextReaderLoadingChangedEvent,
                TextReader.TextReaderSuccessChangedEvent,
                TextReader.TextReaderEncodingChangedEvent,
                TextReader.TextReaderLoadingChangedEvent
            }));
        });
    }

    [Test]
    public void Events_Bubble() {
        Run(async subject => {
            var raised = 0;
            var window = Window.GetWindow(subject);
            window.AddHandler(TextReader.TextReaderSourceChangedEvent, new RoutedEventHandler((s, e) => raised++));
            subject.TextReaderSource = new TestStreamText("hello");
            await Settled(subject);
            Assert.That(raised, Is.EqualTo(1));
        });
    }
}
