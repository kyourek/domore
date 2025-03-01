﻿using Domore.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DIRECTORY = System.IO.Directory;

namespace Domore.Logs.Service {
    internal sealed class FileLog : ILogService {
        private readonly object Locker = new();
        private readonly ConcurrentQueue<string> Queue = new();
        private readonly PathFormatter PathFormatter = new();

        private Timer Timer;

        public string FileName => _FileName ??= FileInfo.Name;
        private string _FileName;

        public string FileNameWithoutExtension => _FileNameWithoutExtension ??= Path.GetFileNameWithoutExtension(FileName);
        private string _FileNameWithoutExtension;

        public string FileExtension => _FileExtension ??= Path.GetExtension(FileName);
        private string _FileExtension;

        private FileInfo FileInfo => _FileInfo ??= new(
            Path.Combine(DirectoryInfo.FullName, PathFormatter.Format(Name)));
        private FileInfo _FileInfo;

        private DirectoryInfo DirectoryInfo => _DirectoryInfo ??= new(
            PathFormatter.Format(
                PathFormatter.Expand(
                    Environment.ExpandEnvironmentVariables(Directory))));
        private DirectoryInfo _DirectoryInfo;

        private string FileDateName() {
            var dt = DateTime.Now;
            return $"{FileNameWithoutExtension}_{dt.Year}{dt.Month:00}{dt.Day:00}-{dt.Hour:00}{dt.Minute:00}{dt.Second:00}-{dt.Millisecond:000}{FileExtension}";
        }

        private DateTime? FileDate(string name) {
            if (name == null) {
                return null;
            }
            var prefix = $"{FileNameWithoutExtension}_";
            if (prefix.Length + FileExtension.Length >= name.Length) {
                return null;
            }
            var date = name.Substring(prefix.Length).Substring(0, name.Length - prefix.Length - FileExtension.Length);
            if (date.Length != 19) {
                return null;
            }
            if (date[8] != '-') {
                return null;
            }
            if (date[15] != '-') {
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
            var exists = fileInfo.Exists;
            if (exists == false) {
                return;
            }
            var size = fileInfo.Length;
            if (size < FileSizeLimit) {
                return;
            }
            var nextName = FileDateName();
            var nextPath = Path.Combine(DirectoryInfo.FullName, nextName);
            try {
                fileInfo.MoveTo(nextPath);
            }
            catch {
                if (File.Exists(nextPath)) {
                    return;
                }
                throw;
            }
            _FileInfo = null;
            var now = DateTime.UtcNow;
            var fileSearchPattern = $"{FileNameWithoutExtension}_*{FileExtension}";
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
                }
                var delay = IORetryDelay;
                if (delay > 0) {
                    Thread.Sleep(delay);
                }
                DirectoryInfo.Refresh();
                FileInfo.Refresh();
            }
        }

        private void TimerCallback(object _) {
            using (Timer) {
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
        }

        private void Start() {
            if (Complete) {
                return;
            }
            Timer = new(TimerCallback, state: null, dueTime: (int)FlushInterval.TotalMilliseconds, period: Timeout.Infinite);
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
                        if (_Directory != value) {
                            _Directory = value;
                            _DirectoryInfo = null;
                            _FileInfo = null;
                            _FileName = null;
                            _FileNameWithoutExtension = null;
                            _FileExtension = null;
                        }
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
                        if (_Name != value) {
                            _Name = value;
                            _FileInfo = null;
                            _FileName = null;
                            _FileNameWithoutExtension = null;
                            _FileExtension = null;
                        }
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
