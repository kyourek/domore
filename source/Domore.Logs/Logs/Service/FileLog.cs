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
    private readonly ConcurrentQueue<string> Queue = new();
    private readonly PathFormatter PathFormatter = new();
    private readonly
#if NET9_0_OR_GREATER
        Lock
#else
        object
#endif
        Locker = new();

    private Timer Timer;
    private int _IORetryLimit = 5;
    private int _IORetryDelay = 10;
    private long _FileSizeLimit = 100000;
    private long _TotalSizeLimit = 100000000;
    private TimeSpan _FileAgeLimit = TimeSpan.FromDays(28);

    public string FileName => _FileName ??=
        FileInfo.Name;
    private string _FileName;

    public string FileNameWithoutExtension => _FileNameWithoutExtension ??=
        Path.GetFileNameWithoutExtension(FileName);
    private string _FileNameWithoutExtension;

    public string FileExtension => _FileExtension ??=
        Path.GetExtension(FileName);
    private string _FileExtension;

    private FileInfo FileInfo => _FileInfo ??=
        new(Path.Combine(DirectoryInfo.FullName, PathFormatter.Format(Name)));
    private FileInfo _FileInfo;

    private DirectoryInfo DirectoryInfo => _DirectoryInfo ??= new(
        PathFormatter.Format(
            PathFormatter.Expand(
                Environment.ExpandEnvironmentVariables(Directory))));
    private DirectoryInfo _DirectoryInfo;

    private string FileDateName() {
        var now = DateTimeOffset.Now;
        var offsetMinutes = (int)now.Offset.TotalMinutes;
        var absoluteOffsetMinutes = Math.Abs(offsetMinutes);
        var offset = $"{(offsetMinutes < 0 ? "-" : "+")}{absoluteOffsetMinutes / 60:00}{absoluteOffsetMinutes % 60:00}";
        var date = now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
        return $"{FileNameWithoutExtension}_{date}{offset}{FileExtension}";
    }

    private DateTime? FileDate(string name) {
        if (name == null) {
            return null;
        }
        var prefix = $"{FileNameWithoutExtension}_";
        if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
            !name.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase) ||
            prefix.Length + FileExtension.Length >= name.Length) {
            return null;
        }
        var date = name.Substring(prefix.Length, name.Length - prefix.Length - FileExtension.Length);
        var hasOffset = date.Length == 24 && (date[19] == '+' || date[19] == '-');
        if (date.Length != 19 && hasOffset == false) {
            return null;
        }
        if (!DateTime.TryParseExact(
            date.Substring(0, 19),
            "yyyyMMdd-HHmmss-fff",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var wallTime)) {
            return null;
        }
        if (hasOffset == false) {
            return DateTime.SpecifyKind(wallTime, DateTimeKind.Local);
        }
        if (!int.TryParse(date.Substring(20, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var offsetHour) ||
            !int.TryParse(date.Substring(22, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var offsetMinute) ||
            offsetHour > 14 || offsetMinute > 59 || (offsetHour == 14 && offsetMinute != 0)) {
            return null;
        }
        var offsetSign = date[19] == '-' ? -1 : 1;
        var offsetValue = TimeSpan.FromMinutes(offsetSign * (offsetHour * 60 + offsetMinute));
        try {
            return new DateTimeOffset(wallTime, offsetValue).UtcDateTime;
        }
        catch (ArgumentException) {
            return null;
        }
    }

    private void Rotate() {
        var fileInfo = _FileInfo;
        if (fileInfo == null) {
            return;
        }
        var directoryInfo = fileInfo.Directory;
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
        var nextPath = Path.Combine(directoryInfo.FullName, nextName);
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
        var files = directoryInfo.GetFiles(fileSearchPattern, SearchOption.TopDirectoryOnly);
        var items = files
            .Select(file => new { File = file, Date = FileDate(file.Name) })
            .Where(item => item.Date.HasValue)
            .Select(item => new {
                item.File,
                Date = item.Date.Value,
                Age = now - item.Date.Value.ToUniversalTime(),
                Length = item.File.Length
            })
            .OrderByDescending(item => item.Age)
            .ToList();
        var itemsToDelete = items
            .Where(item => item.Age > FileAgeLimit)
            .ToList();
        var deleteFailures = new HashSet<FileInfo>();
        bool TryDelete(FileInfo file) {
            try {
                file.Delete();
                return true;
            }
            catch (Exception ex) {
                deleteFailures.Add(file);
                try {
                    Logging.Notify(ex);
                }
                catch (Exception) {
                }
                return false;
            }
        }
        foreach (var item in itemsToDelete) {
            if (TryDelete(item.File)) {
                items.Remove(item);
            }
        }
        var totalSize = items.Sum(item => item.Length);
        while (totalSize > TotalSizeLimit) {
            var oldest = items.FirstOrDefault(item => deleteFailures.Contains(item.File) == false);
            if (oldest == null) {
                break;
            }
            if (TryDelete(oldest.File)) {
                items.Remove(oldest);
                totalSize -= oldest.Length;
            }
        }
    }

    private void Log(IEnumerable<string> lines) {
        var fileInfo = FileInfo;
        var directoryInfo = fileInfo.Directory;
        void log() {
            if (directoryInfo.Exists == false) {
                DIRECTORY.CreateDirectory(directoryInfo.FullName);
                directoryInfo.Refresh();
            }
            File.AppendAllLines(fileInfo.FullName, lines);
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
            directoryInfo.Refresh();
            fileInfo.Refresh();
        }
    }

    private void Flush(IEnumerable<string> lines) {
        Log(lines);
        Rotate();
    }

    private void TimerCallback(object _) {
        try {
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
                        if (limit <= 0) {
                            throw new InvalidOperationException(
                                $"{nameof(LogCountLimit)} must be greater than zero.");
                        }
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
                                Flush(lines);
                            }
                            catch (Exception ex) {
                                Logging.Notify(ex);
                            }
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
                        try {
                            Start();
                        }
                        catch (Exception ex) {
                            Started = false;
                            Logging.Notify(ex);
                        }
                    }
                }
            }
            catch (Exception ex) {
                Logging.Notify(ex);
            }
        }
    }

    private static int ValidateFlushIntervalInMilliseconds(TimeSpan interval, string paramName = null) {
        var maxTicks = (long)int.MaxValue * TimeSpan.TicksPerMillisecond;
        var invalid = interval.Ticks < TimeSpan.TicksPerMillisecond ||
                      interval.Ticks % TimeSpan.TicksPerMillisecond != 0 ||
                      interval.Ticks > maxTicks;
        if (invalid) {
            if (paramName is not null) {
                throw new ArgumentOutOfRangeException(paramName, interval,
                    $"The flush interval must be a whole number of milliseconds between 1 and {int.MaxValue}.");
            }
            else {
                throw new InvalidOperationException("The value is invalid.");
            }
        }
        return (int)interval.TotalMilliseconds;
    }

    private void Start() {
        if (Complete) {
            return;
        }
        Timer = new(TimerCallback,
                    state: null,
                    dueTime: ValidateFlushIntervalInMilliseconds(FlushInterval),
                    period: Timeout.Infinite);
    }

    public int IORetryLimit {
        get {
            lock (Locker) {
                return _IORetryLimit;
            }
        }
        set {
            lock (Locker) {
                _IORetryLimit = value;
            }
        }
    }

    public int IORetryDelay {
        get {
            lock (Locker) {
                return _IORetryDelay;
            }
        }
        set {
            lock (Locker) {
                _IORetryDelay = value;
            }
        }
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
            ValidateFlushIntervalInMilliseconds(value, nameof(FlushInterval));
            lock (Locker) {
                _FlushInterval = value;
            }
        }
    }
    private TimeSpan _FlushInterval = TimeSpan.FromSeconds(2.5);

    public int LogCountLimit {
        get {
            lock (Locker) {
                return field;
            }
        }
        set {
            if (value <= 0) {
                throw new ArgumentOutOfRangeException(nameof(LogCountLimit), value,
                    "The log count limit must be greater than zero.");
            }
            lock (Locker) {
                field = value;
            }
        }
    } = 100;

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
                    Flush(lines);
                }
                catch (Exception ex) {
                    Logging.Notify(ex);
                }
            }
        }
    }

    void ILogService.Log(string name, string data, LogSeverity severity) {
        lock (Locker) {
            if (Complete) {
                return;
            }
            Queue.Enqueue(data);
            if (Started == false) {
                Start();
                Started = true;
            }
        }
    }
}
