# Domore.Conf.Cli

Parse command lines into .NET objects with attributes. Domore.Conf.Cli is built on [Domore.Conf](https://www.nuget.org/packages/Domore.Conf), so values are converted the same forgiving way, and it can generate usage and manual text from the same type.

Install the package with `dotnet add package Domore.Conf.Cli`.

## Define a command

```csharp
using Domore.Conf;
using Domore.Conf.Cli;

[ConfHelp("Copies a file.")]
[CliExample("report.txt backup/ overwrite=true", "Copies report.txt into backup/, replacing any existing file.")]
public sealed class Copy {
    [CliArgument(0), CliRequired, ConfHelp("The file to copy.")]
    public string Source { get; set; }

    [CliArgument(1), CliRequired, ConfHelp("Where to copy the file.")]
    public string Destination { get; set; }

    [ConfHelp("Replace the destination if it exists.")]
    public bool Overwrite { get; set; }

    [ConfHelp("Number of times to retry.")]
    public int Retries { get; set; }

    [CliValidation("Retries cannot be negative.")]
    public bool ValidateRetries() => Retries >= 0;
}
```

## Parse a command line

```csharp
var cli = new CliProvider(new CliSetup());

var copy = cli.Configure(new Copy(), "copy report.txt backup/ overwrite=true retries=3");

Console.WriteLine(copy.Source);      // report.txt
Console.WriteLine(copy.Destination); // backup/
Console.WriteLine(copy.Overwrite);   // True
Console.WriteLine(copy.Retries);     // 3
```

The command name (the type name in lowercase by default) may appear in the line and is skipped. Tokens are separated by whitespace; wrap a token in single or double quotes to include spaces or `=`. Named values are written `name=value`, and names match properties (or `[Conf]` aliases) without regard to case.

## Display usage and manuals

```csharp
Console.WriteLine(cli.Display(new Copy()));
// copy <source> <destination> [overwrite=<true/false>] [retries=<int>]

Console.WriteLine(cli.Manual(new Copy()));
```

```text
copy <source> <destination> [overwrite=<true/false>] [retries=<int>]

    Copies a file.

    source         The file to copy.

    destination    Where to copy the file.

    overwrite      Replace the destination if it exists.

    retries        Number of times to retry.

ex. copy report.txt backup/ overwrite=true
    Copies report.txt into backup/, replacing any existing file.
```

Both methods also accept a collection of targets to display several commands at once.

## Attributes

| Attribute | Target | Purpose |
|-----------|--------|---------|
| `[CliArgument(order)]` | Property | Fills the property from an unnamed, positional argument. |
| `[CliArguments]` | List property | Collects every remaining unnamed argument. |
| `[CliParameters]` | Dictionary property | Also collects each `name=value` pair into the dictionary. |
| `[CliRequired]` | Property | Fails parsing when the property is not provided. |
| `[CliValidation(message)]` | Method | Runs a parameterless method after parsing; returning `false` fails with `message`. Use `order` to sequence several validations. |
| `[CliDisplay(include)]` | Property, enum field | Includes or excludes an item from usage text. |
| `[CliDisplayOverride(display)]` | Property, enum field | Replaces the generated usage text for an item. |
| `[CliExample(command, description)]` | Class | Adds an example to the manual. |
| `[ConfHelp(text)]` | Class, property | Adds help text to the manual (from Domore.Conf). |

```csharp
using System.Collections.Generic;
using Domore.Conf.Cli;

public sealed class Tag {
    [CliArguments]
    public List<string> Files { get; set; } = new List<string>();

    [CliParameters]
    public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();
}

var tag = cli.Configure(new Tag(), "tag a.txt b.txt color=red");
// tag.Files    == ["a.txt", "b.txt"]
// tag.Settings == { ["color"] = "red" }
```

## Errors

Failures throw a subclass of `CliException`:

| Exception | Thrown when |
|-----------|-------------|
| `CliRequiredNotFoundException` | A `[CliRequired]` property is missing, e.g. `Missing required: source, destination`. |
| `CliArgumentNotFoundException` | An unnamed argument has nowhere to go, e.g. `Unexpected argument: c`. |
| `CliConversionException` | A value cannot be converted to the property type. |
| `CliValidationException` | A `[CliValidation]` method returns `false` or throws. |

## Customize command names

`CliSetup` is immutable; each `With...` method returns a new setup. Callbacks receive the target type, and the most recently added callback that returns a non-empty value wins.

```csharp
var cli = new CliProvider(new CliSetup()
    .WithCommandName(type => type.Name.ToUpperInvariant())
    .WithCommandSpace(type => "myapp"));

Console.WriteLine(cli.Display(new Copy()));
// COPY <source> <destination> [overwrite=<true/false>] [retries=<int>]
```

The command space prefixes the command name in manual examples (`ex. myapp COPY ...`).
