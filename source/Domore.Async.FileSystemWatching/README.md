# Domore.Async.FileSystemWatching

React to file-system changes with async callbacks. Domore.Async.FileSystemWatching creates, configures, shares, restarts, and disposes `FileSystemWatcher` instances for you. You write the handler.

Install the package with `dotnet add package Domore.Async.FileSystemWatching`.

## Watch a directory

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

The callback receives the `FileSystemEventArgs` for each `Created`, `Changed`, `Deleted`, or `Renamed` event (for renames it's a `RenamedEventArgs`) and a cancellation token. Dispose the returned subscription to remove the callback. Omit the options to watch every file in the directory, not including subdirectories.

Watching starts in the background shortly after `Add` returns, so changes made immediately afterward may not be reported. If you need to know exactly when watching begins, use [`FileSystemEventProvider`](#stream-events) and its `ready` callback.

## Options

| Option | Default | Description |
|--------|---------|-------------|
| `FileFilter` | all files | A wildcard filter such as `*.log`. |
| `IncludeSubdirectories` | `false` | Also watch subdirectories. |
| `NotifyFilter` | all `NotifyFilters` | Which kinds of changes raise events. |
| `InternalBufferSize` | 65,536 | The size in bytes of the watcher's internal buffer. Larger buffers are less likely to overflow when many changes happen at once. |

`FileSystemEventOptions` is a record, so options with the same values are equal.

## How events are delivered

- **Watchers are shared.** Subscriptions to the same directory with equal options share a single `FileSystemWatcher`. Paths are compared by their full path, and on Windows the directory's case sensitivity is detected. When the last subscription is removed, the watcher is stopped after a short delay, so a quick unsubscribe and resubscribe reuses it.
- **Events arrive in order.** Each event is delivered to every subscription of a watcher at the same time, and the next event isn't delivered until every callback has finished.
- **Callbacks run where you subscribed.** If `Add` is called with a `SynchronizationContext` (for example, on a UI thread), callbacks run on that context. Otherwise, they run on the thread pool.
- **Failures are isolated.** An exception thrown by one callback doesn't affect other subscriptions or stop the watcher.

## Handle results and errors

`FileSystemEventTasks` reports outcomes through optional static handlers. Each receives a `FileSystemEventResult` with the `Subscription`, whether it was `Canceled`, and any `Exception`:

```csharp
FileSystemEventTasks.OnSubscriptionEventError = (result, token) => {
    Console.Error.WriteLine($"A file handler failed: {result.Exception.Message}");
    return Task.CompletedTask;
};

FileSystemEventTasks.OnManagerError = (result, token) => {
    Console.Error.WriteLine($"Could not watch the directory: {result.Exception.Message}");
    return Task.CompletedTask;
};

FileSystemEventTasks.OnUnhandledError = (exception, token) => {
    Console.Error.WriteLine($"The watcher failed: {exception.Message}");
    return Task.FromResult(true); // restart the watcher
};
```

| Handler | Called when |
|---------|-------------|
| `OnSubscriptionEventComplete` | A callback finishes handling an event. |
| `OnSubscriptionEventError` | A callback throws. |
| `OnSubscriptionEventCanceled` | A callback is canceled because its watcher stopped. |
| `OnManagerError` | Adding or removing a subscription fails, for example because the directory doesn't exist. |
| `OnManagerCanceled` | Adding or removing a subscription is canceled. |
| `OnUnhandledError` | The watcher itself fails, for example when its buffer overflows. Return `true` to restart the watcher after a short delay, or `false` to stop it. |

## Stream events

`FileSystemEventProvider` exposes a single watcher as an `IAsyncEnumerable<FileSystemEventArgs>`, for code that would rather `await foreach` than subscribe:

```csharp
using Domore.IO;

var provider = new FileSystemEventProvider(@"C:\data", new FileSystemEventOptions { FileFilter = "*.csv" });

await foreach (var e in provider.Events(
    ready: token => {
        Console.WriteLine("Watching.");
        return Task.CompletedTask;
    },
    token: cancellationToken)) {
    Console.WriteLine($"{e.ChangeType}: {e.Name}");
}
```

The optional `ready` callback runs once the watcher is running, so changes made from it or after it are reported. The watcher is disposed when the enumeration ends. That happens when the loop exits or the watcher fails. Canceling the token also ends it, by throwing an `OperationCanceledException`. Invalid paths or options throw a `FileSystemWatcherInitializationException`.

## Custom subscriptions

For more control, derive from `FileSystemEventSubscription` and manage subscriptions with your own `FileSystemEventManager`. Its handlers are the same as those on `FileSystemEventTasks`, but they're per instance:

```csharp
using Domore.IO;

public sealed class ReportImporter : FileSystemEventSubscription {
    protected override async Task Receive(FileSystemEventArgs e, CancellationToken token) {
        await ImportAsync(e.FullPath, token);
    }
}

var manager = new FileSystemEventManager {
    OnSubscriptionEventError = (result, token) => LogAsync(result.Exception)
};

var importer = new ReportImporter();
await manager.Add(importer, @"C:\reports", options: null, token);
// ...
await manager.Remove(importer, @"C:\reports", options: null, token);
```

## Supported frameworks

.NET Framework 4.6.2, .NET 6, .NET 8, and .NET 10.
