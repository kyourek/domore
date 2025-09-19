using Domore.Conf.Threading;
using System;
using System.IO;
using System.Linq;
using PATH = System.IO.Path;

namespace Domore.Conf;

/// <summary>
/// A file with conf content that may populate an object upon file-system events.
/// </summary>
public sealed class ConfFile : IDisposable {
    private readonly object WatcherLocker = new();
    private readonly object ConfigureLocker = new();
    private readonly DelayedState DelayedState = new() { Delay = 1000 };
    private volatile FileSystemWatcher Watcher;
    private Action CancelDelay;
    private string CanonicalName;
    private bool Disposed;

    private void Dispose(bool disposing) {
        if (disposing) {
            lock (ConfigureLocker) {
                lock (WatcherLocker) {
                    using (Watcher) {
                        Disposed = true;
                    }
                }
            }
        }
    }

    private void Watcher_Event(object sender, FileSystemEventArgs e) {
        if (e != null) {
            if (e.Name == Name || e.Name == CanonicalName) {
                CancelDelay?.Invoke();
                CancelDelay = DelayedState.Attempt(() => {
                    try {
                        Configure();
                        Configured?.Invoke(this, EventArgs.Empty);
                    }
                    catch (Exception ex) {
                        ConfigureError?.Invoke(this, new ErrorEventArgs(ex));
                    }
                });
            }
        }
    }

    private void Watcher_Error(object sender, ErrorEventArgs e) {
        WatchError?.Invoke(this, e);
    }

    private string Read() {
        try {
            return File.ReadAllText(Path);
        }
        catch (FileNotFoundException) {
            return "";
        }
    }

    /// <summary>
    /// Gets or sets the delay, in milliseconds, between events.
    /// </summary>
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
            var text = Read();
            var conf = Conf.Contain(text);
            conf.Configure(Target, key: Key);
        }
        if (watch == true) {
            if (Watcher == null) {
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
            if (Watcher != null) {
                lock (WatcherLocker) {
                    if (Disposed) {
                        throw new ObjectDisposedException(nameof(ConfFile));
                    }
                    if (Watcher != null) {
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
        Directory = PATH.GetDirectoryName(Path);
    }

    /// <summary>
    /// Disposes of resources used by the instance.
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Destructor of <see cref="ConfFile"/>.
    /// </summary>
    ~ConfFile() {
        Dispose(false);
    }
}
