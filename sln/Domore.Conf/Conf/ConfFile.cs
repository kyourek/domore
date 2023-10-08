using Domore.Conf.Threading;
using System;
using System.IO;
using System.Linq;
using PATH = System.IO.Path;

namespace Domore.Conf {
    public sealed class ConfFile : IDisposable {
        private readonly object WatcherLocker = new object();
        private readonly object ConfigureLocker = new object();
        private readonly DelayedState DelayedState = new DelayedState { Delay = 1000 };
        private FileSystemWatcher Watcher;
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

        public int Delay {
            get => DelayedState.Delay;
            set => DelayedState.Delay = value;
        }

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

        public event EventHandler Configured;
        public event ErrorEventHandler WatchError;
        public event ErrorEventHandler ConfigureError;

        public string Path { get; }
        public string Name { get; }
        public string Directory { get; }
        public object Target { get; }
        public string Key { get; }

        public ConfFile(string path, string key, object target) {
            Target = target;
            Key = key;
            Path = path;
            Name = PATH.GetFileName(Path);
            Directory = PATH.GetDirectoryName(Path);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ConfFile() {
            Dispose(false);
        }
    }
}
