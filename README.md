# Do more in .NET

**Domore** is a family of small, focused .NET libraries for the everyday jobs in an app: configuration, command lines, logging, change notification, async caching, file watching, and text decoding. Each package does one thing well, has almost no dependencies, and targets everything from .NET Framework 4.0 through .NET 10 (with a couple of async packages requiring newer runtimes), so you can use it in a brand-new service or a decade-old desktop app.

All packages are MIT licensed and ship with SourceLink and symbol packages, so you can step straight into the source while debugging.

## Packages

| Package | What it does |
|---------|--------------|
| [Domore.Conf](#domoreconf) | Populate plain objects from forgiving `key = value` text or `.conf` files. |
| [Domore.Conf.Cli](#domoreconfcli) | Turn command lines into typed objects, with usage and help text generated for you. |
| [Domore.Conf.ConfigurationManager](#domoreconfconfigurationmanager) | Use `app.config` `<appSettings>` as a Domore.Conf source. |
| [Domore.Logs](#domorelogs) | Lightweight, opinionated logging to the console, debug output, trace, or files. |
| [Domore.Logs.Conf](#domorelogsconf) | Configure Domore.Logs with conf text, and reconfigure it live when a file changes. |
| [Domore.Notification](#domorenotification) | A tiny base class for `INotifyPropertyChanged`, `INotifyPropertyChanging`, and `INotifyDataErrorInfo`. |
| [Domore.Async.TaskCaching](#domoreasynctaskcaching) | Run an async operation once and cache the result, with thread-safe refresh. |
| [Domore.Async.FileSystemWatching](#domoreasyncfilesystemwatching) | Async callbacks for file-system events, with no `FileSystemWatcher` bookkeeping. |
| [Domore.Async.TextReading](#domoreasynctextreading) | Decode files and streams asynchronously with pooled buffers and encoding detection. |
| [Domore.Indexing](#domoreindexing) | Get-or-create collections keyed by forgiving, normalized strings. |
| [Domore.Builds.SemanticVersioning](#domorebuildssemanticversioning) | Parse, compare, and bump semantic versions. |

Install any of them with `dotnet add package <name>`.

---

## Domore.Conf

Configure .NET objects from readable text. There's no schema, no binding setup, and no ceremony: write `key = value` lines, and Domore.Conf fills in your existing objects, including nested properties, lists, and dictionaries. It can also write objects back out as conf text.

```csharp
using Domore.Conf.Extensions;

var settings = new AppSettings().ConfFrom(@"
    AppSettings.Host             = example.com
    AppSettings.Port             = 8080
    AppSettings.Database.Host    = db.example.com
    AppSettings.Servers[0]       = primary
    AppSettings.Labels[region]   = west
");

var text = settings.ConfText(); // ...and back to conf text
```

- **Forgiving by design.** Keys ignore case and whitespace (`Home planet` matches `HomePlanet`), and lines that aren't settings are ignored, so comments are optional.
- **Files that compose.** Load a file with `Conf.Contain("settings.conf")`, split settings across files with `@conf.include`, and let later settings override earlier ones.
- **Hot reload.** `ConfFile` can watch a file and reconfigure your object whenever the file changes.
- **Zero-config default.** With no source set, Domore.Conf finds the `.conf` file next to your app and can seed it from a `.conf.default` file.

📖 [Full Domore.Conf documentation](source/Domore.Conf/README.md)

## Domore.Conf.Cli

Describe a command as a class, and let Domore.Conf.Cli parse the command line, convert the values, enforce required arguments, run your validations, and generate usage and manual text from the same type.

```csharp
using Domore.Conf;
using Domore.Conf.Cli;

[ConfHelp("Copies a file.")]
public sealed class Copy {
    [CliArgument(0), CliRequired, ConfHelp("The file to copy.")]
    public string Source { get; set; }

    [CliArgument(1), CliRequired, ConfHelp("Where to copy the file.")]
    public string Destination { get; set; }

    [ConfHelp("Replace the destination if it exists.")]
    public bool Overwrite { get; set; }
}

var cli = new CliProvider(new CliSetup());
var copy = cli.Configure(new Copy(), "copy report.txt backup/ overwrite=true");

Console.WriteLine(cli.Display(new Copy()));
// copy <source> <destination> [overwrite=<true/false>]
```

Positional arguments, argument lists, `name=value` parameters, examples, and clear, typed exceptions (`Missing required: source, destination`) all come built in.

📖 [Full Domore.Conf.Cli documentation](source/Domore.Conf.Cli/README.md)

## Domore.Conf.ConfigurationManager

Already have settings in `app.config`? Point Domore.Conf at `<appSettings>` and keep the same configuration code:

```csharp
using Domore.Conf;

Conf.ContentProvider = new AppSettingsProvider();
var settings = Conf.Configure(new AppSettings());
```

## Domore.Logs

A lightweight, simple, and very opinionated logging library. Create a log per type, and write to the console, debug output, trace, or files. Logging runs on a background queue, so it stays out of your hot paths.

```csharp
using Domore.Logs;

class Sample {
    private static readonly ILog Log = Logging.For(typeof(Sample));

    static void Main() {
        if (Log.Debug()) Log.Debug($"Now it's {DateTime.Now}."); // cheap check before formatting
        Log.Info("This is the logging sample.");
        Log.Warn("Hey! Look out!");

        Logging.Complete(); // flush pending entries before exit
    }
}
```

Severity thresholds, per-log formats, console colors, custom formatters for your own types, and custom `ILogService` handlers are all configurable at runtime.

## Domore.Logs.Conf

Configure logging with the same conf text as everything else, and change it while the app is running:

```csharp
using Domore.Conf;
using Domore.Conf.Logs;

Conf.Contain(@"
    log[console].config.default.severity = info
    log[console].config.default.format   = {dat} {tim} [{sev}]
    log[console].service.background[warn] = yellow
").ConfigureLogging();
```

Or call `Log.Conf.Configure("logging.conf")` once at startup to load a file and watch it, so you can raise or lower verbosity in production without a restart.

## Domore.Notification

Implement `INotifyPropertyChanged` in one line per property. `Change` only raises events when the value actually changes, gets the property name for you from `[CallerMemberName]`, and notifies dependent properties along with it.

```csharp
using Domore.Notification;

public sealed class Person : Notifier {
    public string FirstName {
        get => _FirstName;
        set => Change(ref _FirstName, value, nameof(FirstName), nameof(FullName));
    }
    private string _FirstName;

    public int Age {
        get => _Age;
        set => Change(ref _Age, value);
    }
    private int _Age;

    public string FullName => $"{FirstName}".Trim();
}
```

It also raises `PropertyChanging`, has overloads for the built-in value types that avoid boxing, can suppress events during batch updates or veto changes, and offers `Notifier.WithErrorInfo` for `INotifyDataErrorInfo` validation.

📖 [Full Domore.Notification documentation](source/Domore.Notification/README.md)

## Domore.Async.TaskCaching

Run an expensive async operation once, and share the result with every caller. Concurrent callers share the in-flight task, and faults and cancellations aren't cached, so the next call simply tries again.

```csharp
using Domore.Threading.Tasks;

var settings = new TaskCache<Settings>(token => LoadSettingsAsync(token));

var a = await settings.Ready(token); // loads
var b = await settings.Ready(token); // cached
```

Need to reload? `TaskCache<T>.WithRefresh` adds `Refresh` and `Refreshed`, and a load still in flight from before the refresh never overwrites the newer result.

## Domore.Async.FileSystemWatching

React to file changes with an async callback. There's no `FileSystemWatcher` to create, configure, keep alive, or dispose correctly:

```csharp
using Domore.IO;

using var subscription = FileSystemEventTasks.Add(
    @"C:\logs",
    new FileSystemEventOptions { FileFilter = "*.log", IncludeSubdirectories = true },
    async (e, token) => {
        Console.WriteLine($"{e.ChangeType}: {e.Name}");
        await ProcessAsync(e.FullPath, token);
    });
```

Disposing the subscription removes the callback, and errors and cancellations are reported through optional handlers instead of disappearing. Subscriptions to the same directory share a single watcher, and a watcher that fails can restart itself. Targets .NET Framework 4.6.2 and .NET 6 or later.

📖 [Full Domore.Async.FileSystemWatching documentation](source/Domore.Async.FileSystemWatching/README.md)

## Domore.Async.TextReading

Decode text files and streams asynchronously, with pooled buffers and encoding detection. List candidate encodings, and the first one in your list that decodes the content cleanly wins, which is handy for files that might be UTF-8 or might be legacy Latin-1. Byte-order marks are detected automatically.

```csharp
using Domore.IO.Extensions;
using Domore.Text;
using Domore.Text.Builders;

var options = new DecodedTextOptions { Encoding = { "utf-8", "iso-8859-1" } };
using (options.Disposable()) {
    var decoded = await new FileInfo("data.txt").DecodeText(
        new TextLineBuilder(onLine: line => Console.WriteLine(line)),
        options,
        token);

    Console.WriteLine(decoded.EncodingWebName); // e.g. iso-8859-1
}
```

Stream lines as they're decoded with `TextLineBuilder`, consume them as an `IAsyncEnumerable` with `TextStreamBuilder`, or collect the whole text with `TextStringBuilder`. Targets .NET 6 and later.

📖 [Full Domore.Async.TextReading documentation](source/Domore.Async.TextReading/README.md)

## Domore.Indexing

Collections that create items on first access, keyed by forgiving strings. Keys ignore case and whitespace, so `"Hello World"`, `"helloworld"`, and `"HELLO world"` all return the same item.

```csharp
using Domore.Collections.ObjectModel;

public sealed class Tag : NormallyIndexedItem<Tag> {
    public string Name => Index;
}

var tags = new Tag.Source();
var tag = tags["Hello World"];                // created on first access
var same = tags["helloworld"];                // same instance
```

Pass a `syncRoot` to make the collection thread-safe, subscribe to `ItemCreated` to initialize new items, and derive from `IndexedItemSource<TIndex, TItem>` for your own key types.

## Domore.Builds.SemanticVersioning

Parse, compare, and bump [SemVer 2.0](https://semver.org) versions, with arbitrarily large version numbers, and produce the strings MSBuild and NuGet expect:

```csharp
using Domore.Builds;
using Domore.Builds.Extensions;

var version = SemVer.Parse("1.4.2");
var next = version.Bump(minor: true);          // 1.5.0
var newer = next > version;                    // true

var packageVersion = SemVer.Parse("2.0.0-rc.1+build.7").PackageVersion(); // 2.0.0-rc.1
```

---

## License

[MIT](LICENSE) © Ken Yourek
