# Domore.Conf

Configure .NET objects from readable `key = value` text or conf files. Domore.Conf populates existing objects, supports nested properties and indexed collections, and can serialize objects back to conf text.

Install the package with `dotnet add package Domore.Conf`.

## Configure an object

```csharp
using System;
using Domore.Conf.Extensions;

var settings = new AppSettings().ConfFrom(@"
AppSettings.Host = example.com
AppSettings.Port = 8080
");

Console.WriteLine(settings.Host); // example.com
Console.WriteLine(settings.Port); // 8080

var text = settings.ConfText();

public sealed class AppSettings {
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 80;
}
```

By default, keys begin with the target type's name (`AppSettings` in this example). Pass `key: ""` to `ConfFrom` to use unprefixed keys instead:

```csharp
var settings = new AppSettings().ConfFrom("Port = 8080", key: "");
```

Keys and property names are matched without regard to case. Values are converted using the invariant culture. Properties not mentioned in the text retain their existing values; an empty value such as `Port =` or `Host =` does not overwrite a property.

## Read a file

Use `Conf.Contain` to load a file path (or conf text) into a container:

```csharp
using Domore.Conf;

var settings = Conf.Contain("settings.conf").Configure(new AppSettings());
```

Alternatively, set `Conf.Source` and call `Conf.Configure` for the shared static container. When no source is set, Domore.Conf looks for a `.conf` file next to the entry application and can copy a `.conf.default` file into place if one exists.

To combine files, add an include directive:

```text
@conf.include = defaults.conf
AppSettings.Port = 8080
```

Relative includes resolve from the containing file's directory. Later settings override earlier included settings. The default directive prefix is `@conf`; set `Conf.Special` or pass a different prefix to `Conf.Contain(source, special)` to change it.

## Nested values and collections

For types with corresponding public properties, separate nested properties with dots and use brackets for list or dictionary entries:

```text
AppSettings.Database.Host = db.example.com
AppSettings.Servers[0] = primary
AppSettings.Labels[region] = west
```

For a comma-separated value that replaces a list, annotate the property with `[ConfListItems]` from `Domore.Conf.Converters`:

```csharp
using System.Collections.Generic;
using Domore.Conf;
using Domore.Conf.Converters;

public sealed class AppSettings {
    [ConfListItems]
    public List<string> Servers { get; set; } = new List<string>();
}
```

```text
AppSettings.Servers = primary, secondary
```

Use `[Conf("alias")]` to accept another name for a property, or `[Conf(ignore: true)]` to exclude it from both population and serialization.

## Watch a file

`ConfFile` can populate an object once and optionally watch the file for changes:

```csharp
using Domore.Conf;

using var file = new ConfFile("settings.conf", key: null, target: settings);
file.Configure(watch: true);
```

The default watch delay is 1,000 ms; set `file.Delay` to change it. Subscribe to `Configured`, `ConfigureError`, or `WatchError` to handle watch events. Dispose the `ConfFile` when it is no longer needed.
