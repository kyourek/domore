# Domore.Processing

Launch a process and asynchronously collect its standard output and standard error as lines.

Install the package with `dotnet add package Domore.Processing`.

## Run a process

`ProcessAgent` configures the executable and starts it. The returned task completes after the process exits and its redirected output has been read. Capture the `IProcessProxy` in the callback to access the output:

```csharp
using Domore.Diagnostics;
using System;
using System.Threading;
using System.Threading.Tasks;

static async Task RunAsync(CancellationToken cancellationToken) {
    IProcessProxy proxy = null;
    var agent = new ProcessAgent {
        FileName = "dotnet",
        Arguments = "--info",
        OnProxyCreated = created => proxy = created
    };

    await agent.Start(cancellationToken);

    foreach (var item in proxy.Stream.LineItems) {
        var writer = item.Kind == ProcessOutputKind.StandardError
            ? Console.Error
            : Console.Out;
        writer.WriteLine(item.Line);
    }
}
```

`LineItems` contains completed lines from both output streams. Each item identifies its source with `ProcessOutputKind`. The collection is read-only and observable; read it after `Start` completes or observe its collection-change notifications while the process is running.

## Configuration

Set `ProcessAgent` properties before calling `Start`:

| Property | Purpose |
|----------|---------|
| `FileName` | Executable path or name. |
| `Arguments` | Arguments passed to the executable. |
| `WorkingDirectory` | Process working directory. |
| `Environment` | Environment variables to add or override. |
| `UserName`, `Domain`, `Password`, `PasswordInClearText`, `LoadUserProfile` | Optional credentials and profile settings, where supported by the platform. |
| `Verb` | Process-start verb, where supported. |
| `OnProxyCreated` | Callback invoked synchronously with the proxy before process startup; `null` omits it. |
| `OnErrorCaught` | Handler for exceptions encountered while closing standard input or retrieving exit metadata. Exceptions from startup and output reading are not sent to it. |
| `StartInfoFixer` | Optional `IProcessStartInfoFixer` that can modify start information and retry after a process-start failure. |
| `SynchronizeWithCurrentContext` | Whether output collection changes are posted to the `SynchronizationContext` current when `Start` is called. Defaults to `true`. |
| `StreamBufferSize` | Size, in characters, of each buffer used to read standard output and standard error. Zero (the default) uses 4096 characters; negative values cause `Start` to throw `InvalidOperationException` synchronously before the process starts. |

When a synchronization context is available, the default `SynchronizeWithCurrentContext` setting lets UI-bound observers receive collection changes on that context. Without a current context, changes are dispatched on the thread pool. Set `SynchronizeWithCurrentContext` to `false` to dispatch collection changes on the thread pool even when a context is available. Only `LineItems` changes are marshalled; `CurrentItem` and `Line` property-change notifications are raised on the output-reading thread. Prefer awaiting `Start` rather than synchronously blocking on it.

The process's standard input is closed immediately after it starts, so processes that read input receive end-of-file.

Set `OnErrorCaught` to handle exceptions encountered while closing standard input or retrieving the process exit code and exit time. If it is `null`, those exceptions fault the returned task. An exception is considered handled when the callback returns normally; exceptions from process startup and output reading are not sent to it.

The `OnProxyCreated` callback runs synchronously before process startup; set it to `null` to omit it. The proxy's `Stream` is initialized after the process starts. Cancel the supplied token to request termination of the process and cancel the returned task.

## Supported frameworks

.NET Framework 4.0 and 4.6.2, .NET Standard 2.0, .NET Core 3.1, and .NET 6, .NET 8, and .NET 10.
