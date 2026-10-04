using Domore.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using DIRECTORY = System.IO.Directory;

namespace Domore.Logs.Service;

internal sealed class FileLog : ILogService, ILogQueueStatusProvider {
    private sealed class PendingLog {
        public readonly string Data;
        public readonly long Bytes;

        public PendingLog(string data) {
            Data = data;
            Bytes = 2L * data.Length;
        }
    }

    private readonly Queue<PendingLog> Queue = new();
    private readonly PathFormatter PathFormatter = new();
    private readonly
#if NET9_0_OR_GREATER
        Lock
#else
        object
#endif
        AdmissionLocker = new();

    private readonly
#if NET9_0_OR_GREATER
        Lock
#else
        object
#endif
        FileLocker = new();

    private Timer Timer;
    private long PendingMessageBytes;
    private long DroppedItemCount;
    private long DroppedMessageBytes;
    private bool TimerStarting;
    private bool AdmissionClosed;

    private FileInfo FileInfo {
        get => field ??= new(Path.Combine(DirectoryInfo.FullName, PathFormatter.Format(Name)));
        set;
    }

    private DirectoryInfo DirectoryInfo {
        get => field ??= new(
            PathFormatter.Format(
                PathFormatter.Expand(
                    Environment.ExpandEnvironmentVariables(Directory))));
        set;
    }

    private static long SaturatingAdd(long value, long increment) =>
        value > long.MaxValue - increment ? long.MaxValue : value + increment;

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
        var fileInfo = FileInfo;
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
        FileInfo = null;
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
            Timer timer;
            lock (AdmissionLocker) {
                timer = Timer;
                Timer = null;
                if (Complete) {
                    timer?.Dispose();
                    return;
                }
            }
            timer?.Dispose();
            lock (FileLocker) {
                for (; ; ) {
                    List<string> lines;
                    lock (AdmissionLocker) {
                        if (Complete || Queue.Count == 0) {
                            break;
                        }
                        var limit = LogCountLimit;
                        lines = new List<string>(capacity: limit);
                        while (lines.Count < limit && Queue.Count > 0) {
                            var pending = Queue.Dequeue();
                            PendingMessageBytes -= pending.Bytes;
                            lines.Add(pending.Data);
                        }
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

                var dueTime = ValidateFlushIntervalInMilliseconds(FlushInterval);
                lock (AdmissionLocker) {
                    if (Complete == false) {
                        Timer = new Timer(TimerCallback,
                                          state: null,
                                          dueTime: dueTime,
                                          period: Timeout.Infinite);
                        Started = true;
                    }
                }
            }
        }
        catch (Exception ex) {
            Logging.Notify(ex);
            lock (AdmissionLocker) {
                if (Complete == false) {
                    Started = false;
                    TimerStarting = false;
                }
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
        var dueTime = ValidateFlushIntervalInMilliseconds(FlushInterval);
        lock (AdmissionLocker) {
            try {
                if (Complete == false) {
                    Timer = new Timer(TimerCallback,
                                      state: null,
                                      dueTime: dueTime,
                                      period: Timeout.Infinite);
                    Started = true;
                }
            }
            finally {
                TimerStarting = false;
            }
        }
    }

    public string FileName {
        get => field ??= FileInfo.Name;
        private set;
    }

    public string FileNameWithoutExtension {
        get => field ??= Path.GetFileNameWithoutExtension(FileName);
        private set;
    }

    public string FileExtension {
        get => field ??= Path.GetExtension(FileName);
        private set;
    }

    public int IORetryLimit {
        get {
            lock (FileLocker) {
                return field;
            }
        }
        set {
            lock (FileLocker) {
                field = value;
            }
        }
    } = 5;

    public int IORetryDelay {
        get {
            lock (FileLocker) {
                return field;
            }
        }
        set {
            lock (FileLocker) {
                field = value;
            }
        }
    } = 10;

    public long FileSizeLimit {
        get {
            lock (FileLocker) {
                return field;
            }
        }
        set {
            lock (FileLocker) {
                field = value;
            }
        }
    } = 100000;

    public long TotalSizeLimit {
        get {
            lock (FileLocker) {
                return field;
            }
        }
        set {
            lock (FileLocker) {
                field = value;
            }
        }
    } = 100000000;

    public TimeSpan FileAgeLimit {
        get {
            lock (FileLocker) {
                return field;
            }
        }
        set {
            lock (FileLocker) {
                field = value;
            }
        }
    } = TimeSpan.FromDays(28);

    public TimeSpan FlushInterval {
        get {
            lock (FileLocker) {
                return field;
            }
        }
        set {
            ValidateFlushIntervalInMilliseconds(value, nameof(FlushInterval));
            lock (FileLocker) {
                field = value;
            }
        }
    } = TimeSpan.FromSeconds(2.5);

    public int LogCountLimit {
        get {
            lock (FileLocker) {
                return field;
            }
        }
        set {
            if (value <= 0) {
                throw new ArgumentOutOfRangeException(nameof(LogCountLimit), value,
                    "The log count limit must be greater than zero.");
            }
            lock (FileLocker) {
                field = value;
            }
        }
    } = 100;

    public int QueueItemLimit {
        get {
            lock (AdmissionLocker) {
                return field;
            }
        }
        set {
            if (value <= 0) {
                throw new ArgumentOutOfRangeException(nameof(QueueItemLimit), value,
                    "The queue item limit must be greater than zero.");
            }
            lock (AdmissionLocker) {
                field = value;
            }
        }
    } = 1024;

    public long QueueByteLimit {
        get {
            lock (AdmissionLocker) {
                return field;
            }
        }
        set {
            if (value <= 0) {
                throw new ArgumentOutOfRangeException(nameof(QueueByteLimit), value,
                    "The queue byte limit must be greater than zero.");
            }
            lock (AdmissionLocker) {
                field = value;
            }
        }
    } = 8L * 1024 * 1024;

    public LogQueueStatistics QueueStatus {
        get {
            lock (AdmissionLocker) {
                return new LogQueueStatistics(
                    QueueItemLimit,
                    QueueByteLimit,
                    Queue.Count,
                    PendingMessageBytes,
                    DroppedItemCount,
                    DroppedMessageBytes);
            }
        }
    }

    public string Directory {
        get;
        set {
            if (field != value) {
                lock (FileLocker) {
                    if (field != value) {
                        field = value;
                        DirectoryInfo = null;
                        FileInfo = null;
                        FileName = null;
                        FileNameWithoutExtension = null;
                        FileExtension = null;
                    }
                }
            }
        }
    }

    public string Name {
        get;
        set {
            if (field != value) {
                lock (FileLocker) {
                    if (field != value) {
                        field = value;
                        FileInfo = null;
                        FileName = null;
                        FileNameWithoutExtension = null;
                        FileExtension = null;
                    }
                }
            }
        }
    }

    public bool Started {
        get {
            lock (AdmissionLocker) {
                return field;
            }
        }
        private set {
            lock (AdmissionLocker) {
                field = value;
            }
        }
    }
    public bool Complete {
        get {
            lock (AdmissionLocker) {
                return field;
            }
        }
        private set {
            lock (AdmissionLocker) {
                field = value;
            }
        }
    }

    void ILogService.Complete() {
        Timer timer;
        lock (AdmissionLocker) {
            AdmissionClosed = true;
            Complete = true;
            timer = Timer;
            Timer = null;
        }
        timer?.Dispose();
        lock (FileLocker) {
            List<string> lines;
            lock (AdmissionLocker) {
                lines = new List<string>(Queue.Count);
                while (Queue.Count > 0) {
                    var pending = Queue.Dequeue();
                    PendingMessageBytes -= pending.Bytes;
                    lines.Add(pending.Data);
                }
            }
            if (lines.Count == 0) {
                return;
            }
            try {
                Flush(lines);
            }
            catch (Exception ex) {
                Logging.Notify(ex);
            }
        }
    }

    void ILogService.Log(string name, string data, LogSeverity severity) {
        var pending = new PendingLog(data ?? "");
        var startTimer = false;
        var dropped = false;
        lock (AdmissionLocker) {
            if (AdmissionClosed || Complete) {
                return;
            }
            if (Queue.Count >= QueueItemLimit ||
                pending.Bytes > QueueByteLimit ||
                PendingMessageBytes > QueueByteLimit - pending.Bytes) {
                DroppedItemCount = SaturatingAdd(DroppedItemCount, 1);
                DroppedMessageBytes = SaturatingAdd(DroppedMessageBytes, pending.Bytes);
                dropped = true;
            }
            else {
                Queue.Enqueue(pending);
                PendingMessageBytes += pending.Bytes;
                if (Started == false && TimerStarting == false) {
                    TimerStarting = true;
                    startTimer = true;
                }
            }
        }
        if (dropped) {
            LogQueueDiagnostics.ReportOverflow();
            return;
        }
        if (startTimer) {
            try {
                Start();
            }
            catch {
                lock (AdmissionLocker) {
                    TimerStarting = false;
                }
                throw;
            }
        }
    }
}
