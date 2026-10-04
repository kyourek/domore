using Domore.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using DIRECTORY = System.IO.Directory;

namespace Domore.Logs.Service; 
internal sealed class FileLog : ILogService {
    private readonly object Locker = new();
    private readonly ConcurrentQueue<string> Queue = new();
    private readonly PathFormatter PathFormatter = new();

    private Timer Timer;
    private int _IORetryLimit = 5;
    private int _IORetryDelay = 10;
    private int _LogCountLimit = 100;
    private long _FileSizeLimit = 100000;
    private long _TotalSizeLimit = 100000000;
    private TimeSpan _FileAgeLimit = TimeSpan.FromDays(28);
    private TimeSpan _FlushInterval = TimeSpan.FromSeconds(2.5);

    public string FileName => _FileName ??= FileInfo.Name;
    private string _FileName;

    public string FileNameWithoutExtension => _FileNameWithoutExtension ??= Path.GetFileNameWithoutExtension(FileName);
    private string _FileNameWithoutExtension;

    public string FileExtension => _FileExtension ??= Path.GetExtension(FileName);
    private string _FileExtension;

    private FileInfo FileInfo => _FileInfo ??= new(
        ResolvedPath);
    private FileInfo _FileInfo;

    private string ResolvedPath {
        get {
            var resolvedPath = _ResolvedPath;
            if (resolvedPath != null) {
                return resolvedPath;
            }
            lock (Locker) {
                resolvedPath = _ResolvedPath;
                if (resolvedPath == null) {
                    resolvedPath = new FileInfo(
                        Path.Combine(DirectoryInfo.FullName, PathFormatter.Format(Name))).FullName;
                    _ResolvedPath = resolvedPath;
                }
                return resolvedPath;
            }
        }
    }
    private volatile string _ResolvedPath;

    private DirectoryInfo DirectoryInfo => _DirectoryInfo ??= new(
        PathFormatter.Format(
            PathFormatter.Expand(
                Environment.ExpandEnvironmentVariables(Directory))));
    private DirectoryInfo _DirectoryInfo;

    private string FileDateName() {
        var dt = DateTime.UtcNow;
        var date = dt.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
        return $"{FileNameWithoutExtension}_{date}{FileExtension}";
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
        if (DateTime.TryParseExact(
            date,
            "yyyyMMdd-HHmmss-fff",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed) == false) {
            return null;
        }
        return parsed;
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
        var fileAgeLimit = FileAgeLimit;
        var totalSizeLimit = TotalSizeLimit;
        var fileSearchPattern = $"{FileNameWithoutExtension}_*{FileExtension}";
        var files = DirectoryInfo.GetFiles(fileSearchPattern, SearchOption.TopDirectoryOnly);
        var items = files
            .Select(file => new { File = file, Date = FileDate(file.Name) })
            .Where(item => item.Date.HasValue)
            .Select(item => new { item.File, Date = item.Date.Value, Age = now - item.Date.Value })
            .OrderByDescending(item => item.Age)
            .ToList();
        var itemsToDelete = items
            .Where(item => item.Age > fileAgeLimit)
            .ToList();
        var totalSize = 0L;
        foreach (var item in items) {
            try {
                totalSize += item.File.Length;
            }
            catch (Exception ex) {
                Logging.Notify(ex);
            }
        }
        foreach (var item in itemsToDelete) {
            try {
                var itemSize = item.File.Length;
                item.File.Delete();
                totalSize -= itemSize;
            }
            catch (Exception ex) {
                Logging.Notify(ex);
            }
        }
        items = items
            .Where(item => item.Age <= fileAgeLimit)
            .ToList();
        for (var i = 0; i < items.Count && totalSize > totalSizeLimit; i++) {
            var oldest = items[i];
            try {
                var oldestSize = oldest.File.Length;
                oldest.File.Delete();
                totalSize -= oldestSize;
            }
            catch (Exception ex) {
                Logging.Notify(ex);
            }
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
        try {
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
        catch (Exception ex) {
            Logging.Notify(ex);
        }
        finally {
            try {
                lock (Locker) {
                    if (Complete == false) {
                        if (Timer.Change((int)FlushInterval.TotalMilliseconds, Timeout.Infinite) == false) {
                            throw new ObjectDisposedException(nameof(Timer));
                        }
                    }
                }
            }
            catch (Exception ex) {
                Logging.Notify(ex);
            }
        }
    }

    private bool Start() {
        if (Complete) {
            return false;
        }
        if (Timer != null) {
            return true;
        }
        var timer = new Timer(TimerCallback, state: null, dueTime: Timeout.Infinite, period: Timeout.Infinite);
        Timer = timer;
        try {
            if (timer.Change((int)FlushInterval.TotalMilliseconds, Timeout.Infinite) == false) {
                throw new ObjectDisposedException(nameof(Timer));
            }
        }
        catch {
            Timer = null;
            timer.Dispose();
            throw;
        }
        return true;
    }

    public int IORetryLimit {
        get => _IORetryLimit;
        set => _IORetryLimit = Math.Max(1, value);
    }
    public int IORetryDelay {
        get => _IORetryDelay;
        set => _IORetryDelay = Math.Max(0, value);
    }
    public int LogCountLimit {
        get => _LogCountLimit;
        set => _LogCountLimit = Math.Max(1, value);
    }
    public long FileSizeLimit {
        get {
            lock (Locker) {
                return _FileSizeLimit;
            }
        }
        set {
            lock (Locker) {
                _FileSizeLimit = value;
            }
        }
    }
    public long TotalSizeLimit {
        get {
            lock (Locker) {
                return _TotalSizeLimit;
            }
        }
        set {
            lock (Locker) {
                _TotalSizeLimit = value;
            }
        }
    }
    public TimeSpan FileAgeLimit {
        get {
            lock (Locker) {
                return _FileAgeLimit;
            }
        }
        set {
            lock (Locker) {
                _FileAgeLimit = value;
            }
        }
    }
    public TimeSpan FlushInterval {
        get {
            lock (Locker) {
                return _FlushInterval;
            }
        }
        set {
            lock (Locker) {
                if (value < TimeSpan.FromMilliseconds(1)) {
                    _FlushInterval = TimeSpan.FromMilliseconds(1);
                }
                else if (value > TimeSpan.FromMilliseconds(int.MaxValue)) {
                    _FlushInterval = TimeSpan.FromMilliseconds(int.MaxValue);
                }
                else {
                    _FlushInterval = value;
                }
            }
        }
    }

    public string Directory {
        get => _Directory;
        set {
            if (_Directory != value) {
                lock (Locker) {
                    if (_Directory != value) {
                        _Directory = value;
                        _DirectoryInfo = null;
                        _FileInfo = null;
                        _ResolvedPath = null;
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
                        _ResolvedPath = null;
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
                Timer?.Dispose();
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
        try {
            _ = ResolvedPath;
            Queue.Enqueue(data);
            if (Started == false) {
                lock (Locker) {
                    if (Started == false) {
                        Started = Start();
                    }
                }
            }
        }
        catch (Exception ex) {
            Logging.Notify(ex);
        }
    }
}
