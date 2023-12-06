using Domore.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIRECTORY = System.IO.Directory;

namespace Domore.Logs.Service {
    internal sealed class FileLog : ILogService {
        private readonly object Locker = new();
        private readonly ConcurrentQueue<string> Queue = new();
        private readonly PathSpecial PathSpecial = new();

        private FileInfo FileInfo =>
            _FileInfo ?? (
            _FileInfo = new FileInfo(Path.Combine(DirectoryInfo.FullName, Name)));
        private FileInfo _FileInfo;

        private DirectoryInfo DirectoryInfo =>
            _DirectoryInfo ?? (
            _DirectoryInfo = new DirectoryInfo(PathSpecial.Expand(Environment.ExpandEnvironmentVariables(Directory))));
        private DirectoryInfo _DirectoryInfo;

        private string FileDateName() {
            var d = DateTime.UtcNow;
            return $"{Name}_{d.Year}{d.Month:00}{d.Day:00}-{d.Hour:00}{d.Minute:00}{d.Second:00}.{d.Millisecond:000}";
        }

        private DateTime? FileDate(string name) {
            if (name == null) {
                return null;
            }
            var prefix = $"{Name}_";
            if (prefix.Length >= name.Length) {
                return null;
            }
            var date = name.Substring(prefix.Length);
            if (date.Length != 19) {
                return null;
            }
            if (date[8] != '-') {
                return null;
            }
            if (date[15] != '.') {
                return null;
            }
            if (!int.TryParse(date.Substring(0, 4), out var year)) {
                return null;
            }
            if (!int.TryParse(date.Substring(4, 2), out var month)) {
                return null;
            }
            if (!int.TryParse(date.Substring(6, 2), out var day)) {
                return null;
            }
            if (!int.TryParse(date.Substring(9, 2), out var hour)) {
                return null;
            }
            if (!int.TryParse(date.Substring(11, 2), out var minute)) {
                return null;
            }
            if (!int.TryParse(date.Substring(13, 2), out var second)) {
                return null;
            }
            if (!int.TryParse(date.Substring(16, 3), out var millisecond)) {
                return null;
            }
            return new DateTime(year, month, day, hour, minute, second, millisecond, DateTimeKind.Utc);
        }

        private void Rotate() {
            var fileInfo = _FileInfo;
            if (fileInfo == null) {
                return;
            }
            fileInfo.Refresh();
            var size = fileInfo.Length;
            if (size < FileSizeLimit) {
                return;
            }
            var nextName = FileDateName();
            var nextPath = Path.Combine(DirectoryInfo.FullName, nextName);
            fileInfo.MoveTo(nextPath);
            _FileInfo = null;

            var now = DateTime.UtcNow;
            var fileSearchPattern = $"{Name}_*";
            var files = DirectoryInfo.GetFiles(fileSearchPattern, SearchOption.TopDirectoryOnly);
            var items = files
                .Select(file => new { File = file, Date = FileDate(file.Name) })
                .Where(item => item.Date.HasValue)
                .Select(item => new { item.File, Date = item.Date.Value, Age = now - item.Date.Value })
                .OrderByDescending(item => item.Age)
                .ToList();
            var itemsToDelete = items
                .Where(item => item.Age > FileAgeLimit)
                .ToList();
            foreach (var item in itemsToDelete) {
                item.File.Delete();
                items.Remove(item);
            }
            while (items.Count > 0 && items.Sum(item => item.File.Length) > TotalSizeLimit) {
                var oldest = items[0];
                oldest.File.Delete();
                items.Remove(oldest);
            }
        }

        private void Log(IEnumerable<string> lines) {
            void log() {
                if (DirectoryInfo.Exists == false) {
                    DIRECTORY.CreateDirectory(DirectoryInfo.FullName);
                    DirectoryInfo.Refresh();
                }
                if (FileInfo.Exists == false) {
                    using (FileInfo.Create()) {
                    }
                    FileInfo.Refresh();
                }
                File.AppendAllLines(FileInfo.FullName, lines);
            }
            for (var retry = 1; ; retry++) {
                try {
                    log();
                    break;
                }
                catch (IOException) {
                    var limit = IORetryLimit;
                    if (limit <= retry) {
                        throw;
                    }
                    var delay = IORetryDelay;
                    if (delay > 0) {
                        Thread.Sleep(delay);
                    }
                }
            }
        }

        private void Start() {
            if (Complete) {
                return;
            }
            var
            timer = default(Timer);
            timer = new Timer(state: null, dueTime: (int)FlushInterval.TotalMilliseconds, period: Timeout.Infinite, callback: _ => {
                using (timer) {
                    for (; ; ) {
                        lock (Locker) {
                            if (Complete) {
                                break;
                            }
                            if (Queue.Count == 0) {
                                break;
                            }
                            var limit = LogCountLimit;
                            var lines = new List<string>(capacity: limit);
                            for (; ; ) {
                                if (lines.Count >= limit) {
                                    break;
                                }
                                var dequeued = Queue.TryDequeue(out var line);
                                if (dequeued == false) {
                                    break;
                                }
                                lines.Add(line);
                            }
                            if (lines.Count > 0) {
                                try {
                                    Log(lines);
                                    Rotate();
                                }
                                catch (Exception ex) {
                                    Logging.Notify(ex);
                                }
                            }
                        }
                    }
                }
                Start();
            });
        }

        public int IORetryLimit { get; set; } = 5;
        public int IORetryDelay { get; set; } = 10;
        public int LogCountLimit { get; set; } = 100;
        public long FileSizeLimit { get; set; } = 100000;
        public long TotalSizeLimit { get; set; } = 100000000;
        public TimeSpan FileAgeLimit { get; set; } = TimeSpan.FromDays(28);
        public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(2.5);

        public string Directory {
            get => _Directory;
            set {
                if (_Directory != value) {
                    lock (Locker) {
                        _Directory = value;
                        _DirectoryInfo = null;
                        _FileInfo = null;
                    }
                }
            }
        }
        private string _Directory;

        public string Name {
            get => _Name;
            set {
                if (_Name != value) {
                    lock (Locker) {
                        _Name = value;
                        _FileInfo = null;
                    }
                }
            }
        }
        private string _Name;

        public bool Started { get; private set; }
        public bool Complete { get; private set; }

        void ILogService.Complete() {
            lock (Locker) {
                var lines = new List<string>(capacity: Queue.Count);
                try {
                    while (Queue.Count > 0) {
                        while (Queue.TryDequeue(out var line)) {
                            lines.Add(line);
                        }
                    }
                }
                finally {
                    Complete = true;
                }
                if (lines.Count > 0) {
                    try {
                        Log(lines);
                    }
                    catch (Exception ex) {
                        Logging.Notify(ex);
                    }
                }
            }
        }

        void ILogService.Log(string name, string data, LogSeverity severity) {
            if (Started == false) {
                lock (Locker) {
                    if (Started == false) {
                        Started = true;
                        Start();
                    }
                }
            }
            Queue.Enqueue(data);
        }
    }
}
