using Domore.Conf.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PATH = System.IO.Path;
using LOCK =
#if NET9_0_OR_GREATER
    System.Threading.Lock
#else
    System.Object
#endif
;

namespace Domore.Conf;

/// <summary>
/// A file with conf content that may populate an object upon file-system events.
/// </summary>
public sealed class ConfFile : IDisposable {
    private readonly LOCK WatcherLocker = new();
    private readonly LOCK ConfigureLocker = new();
    private readonly LOCK DelayLocker = new();
    private readonly DelayedState DelayedState = new() { Delay = 1000 };
    private readonly List<Action> PendingDelayCancellations = [];
    private volatile FileSystemWatcher Watcher;
    private string CanonicalName;
    private volatile bool Disposed;

    private void Watcher_Error(object sender, ErrorEventArgs e) {
        if (Disposed == false) {
            WatchError?.Invoke(this, e);
        }
    }

    private string Read() {
        try {
            return File.ReadAllText(Path);
        }
        catch (FileNotFoundException) {
            return "";
        }
        catch (DirectoryNotFoundException) {
            return "";
        }
    }

    internal void Watcher_Event(object sender, FileSystemEventArgs e) {
        if (e is null || (e.Name != Name && e.Name != CanonicalName)) {
            return;
        }
        Action[] previous;
        lock (DelayLocker) {
            if (Disposed) {
                return;
            }
            previous = [.. PendingDelayCancellations];
            PendingDelayCancellations.Add(DelayedState.Attempt(() => {
                try {
                    if (Disposed) {
                        return;
                    }
                    Configure();
                    if (Disposed == false) {
                        Configured?.Invoke(this, EventArgs.Empty);
                    }
                }
                catch (Exception ex) {
                    if (Disposed == false) {
                        ConfigureError?.Invoke(this, new ErrorEventArgs(ex));
                    }
                }
            }));
        }
        foreach (var cancel in previous) {
            cancel();
        }
        lock (DelayLocker) {
            foreach (var cancel in previous) {
                PendingDelayCancellations.Remove(cancel);
            }
        }
    }

    /// <summary>
    /// Gets or sets the delay, in milliseconds, between events.
    /// </summary>
    /// <remarks>
    /// The value must be nonnegative or <see cref="System.Threading.Timeout.Infinite"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is less than <see cref="System.Threading.Timeout.Infinite"/>.</exception>
    public int Delay {
        get => DelayedState.Delay;
        set => DelayedState.Delay = value;
    }

    /// <summary>
    /// Configures <see cref="Target"/> and optionally begins watching for changes.
    /// </summary>
    /// <param name="watch">True to watch for changes, otherwise false.</param>
    /// <exception cref="ObjectDisposedException">Thrown if this object is disposed.</exception>
    public void Configure(bool? watch = null) {
        lock (ConfigureLocker) {
            if (Disposed) {
                throw new ObjectDisposedException(nameof(ConfFile));
            }
            if (CanonicalName == null) {
                var dirInfo = new DirectoryInfo(Directory);
                if (dirInfo.Exists) {
                    var dirFiles = dirInfo.GetFiles();
                    var canonicalFile =
                        dirFiles.FirstOrDefault(file => file.Name == Name) ??
                        dirFiles.FirstOrDefault(file => file.Name.Equals(Name, StringComparison.OrdinalIgnoreCase));
                    CanonicalName = canonicalFile?.Name;
                }
            }
            var text = Read()?.Trim() ?? "";
            if (text != "") {
                var conf = new ConfContainer {
                    Source = text,
                    Special = Conf.Special,
                    SourceDirectory = Directory,
                    InitialSources = [Path],
                    ContentProvider = Conf.ContentProvider
                };
                conf.Configure(Target, key: Key);
            }
        }
        if (watch == true) {
            if (Watcher is null) {
                lock (WatcherLocker) {
                    if (Disposed) {
                        throw new ObjectDisposedException(nameof(ConfFile));
                    }
                    if (Watcher == null) {
                        Watcher = new FileSystemWatcher();
                        Watcher.Changed += Watcher_Event;
                        Watcher.Created += Watcher_Event;
                        Watcher.Deleted += Watcher_Event;
                        Watcher.Renamed += Watcher_Event;
                        Watcher.Error += Watcher_Error;
                        Watcher.Path = Directory;
                        Watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.LastWrite;
                        Watcher.IncludeSubdirectories = false;
                        Watcher.InternalBufferSize = 65536;
                        Watcher.EnableRaisingEvents = true;
                    }
                }
            }
        }
        if (watch == false) {
            if (Watcher is not null) {
                lock (WatcherLocker) {
                    if (Disposed) {
                        throw new ObjectDisposedException(nameof(ConfFile));
                    }
                    if (Watcher is not null) {
                        using (Watcher) {
                            Watcher.Changed -= Watcher_Event;
                            Watcher.Created -= Watcher_Event;
                            Watcher.Deleted -= Watcher_Event;
                            Watcher.Renamed -= Watcher_Event;
                            Watcher.Error -= Watcher_Error; ;
                            Watcher = null;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Raised when <see cref="Target"/> is populated.
    /// </summary>
    public event EventHandler Configured;

    /// <summary>
    /// Raised if a watch error occurs.
    /// </summary>
    public event ErrorEventHandler WatchError;

    /// <summary>
    /// Raised if an error occurs during population triggered by an event.
    /// </summary>
    public event ErrorEventHandler ConfigureError;

    /// <summary>
    /// Gets the path of the file.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the name of the file.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the directory of the file.
    /// </summary>
    public string Directory { get; }

    /// <summary>
    /// Gets the object to be populated.
    /// </summary>
    public object Target { get; }

    /// <summary>
    /// Gets the key used to populate the <see cref="Target"/>.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Creates a new instance of <see cref="ConfFile"/>.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    /// <param name="key">The key used to populate the <paramref name="target"/>.</param>
    /// <param name="target">The object to be populated.</param>
    public ConfFile(string path, string key, object target) {
        Target = target;
        Key = key;
        Path = path;
        Name = PATH.GetFileName(Path);
        Directory = PATH.GetDirectoryName(Path) switch {
            var dir when !string.IsNullOrEmpty(dir) => dir,
            _ => "."
        };
    }

    /// <summary>
    /// Disposes of resources used by the instance.
    /// </summary>
    public void Dispose() {
        Action[] cancellations;
        lock (DelayLocker) {
            Disposed = true;
            cancellations = PendingDelayCancellations.ToArray();
        }
        foreach (var cancel in cancellations) {
            cancel();
        }
        lock (DelayLocker) {
            PendingDelayCancellations.Clear();
        }
        lock (ConfigureLocker) {
            lock (WatcherLocker) {
                var
                watcher = Watcher;
                Watcher = null;
                if (watcher is not null) {
                    watcher.Changed -= Watcher_Event;
                    watcher.Created -= Watcher_Event;
                    watcher.Deleted -= Watcher_Event;
                    watcher.Renamed -= Watcher_Event;
                    watcher.Error -= Watcher_Error;
                    watcher.Dispose();
                }
            }
        }
    }
}
