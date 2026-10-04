using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Domore.Logs.Service; 
internal sealed class ConsoleLog : ILogService {
    private static readonly object ConsoleLocker = new();

    private readonly Func<ConsoleColor> GetForegroundColor;
    private readonly Action<ConsoleColor> SetForegroundColor;
    private readonly Func<ConsoleColor> GetBackgroundColor;
    private readonly Action<ConsoleColor> SetBackgroundColor;

    public ConsoleLog() : this(
        () => Console.ForegroundColor,
        color => Console.ForegroundColor = color,
        () => Console.BackgroundColor,
        color => Console.BackgroundColor = color) {
    }

    internal ConsoleLog(
        Func<ConsoleColor> getForegroundColor,
        Action<ConsoleColor> setForegroundColor,
        Func<ConsoleColor> getBackgroundColor,
        Action<ConsoleColor> setBackgroundColor) {
        GetForegroundColor = getForegroundColor;
        SetForegroundColor = setForegroundColor;
        GetBackgroundColor = getBackgroundColor;
        SetBackgroundColor = setBackgroundColor;
    }

    private static ConcurrentDictionary<LogSeverity, ConsoleColor> ForegroundDefault {
        get {
            var colors = new ConcurrentDictionary<LogSeverity, ConsoleColor>();
            colors[LogSeverity.Debug] = ConsoleColor.Cyan;
            colors[LogSeverity.Info] = ConsoleColor.Gray;
            colors[LogSeverity.Warn] = ConsoleColor.Yellow;
            colors[LogSeverity.Error] = ConsoleColor.Red;
            colors[LogSeverity.Critical] = ConsoleColor.Black;
            return colors;
        }
    }

    private static ConcurrentDictionary<LogSeverity, ConsoleColor> BackgroundDefault {
        get {
            var colors = new ConcurrentDictionary<LogSeverity, ConsoleColor>();
            colors[LogSeverity.Debug] = ConsoleColor.Black;
            colors[LogSeverity.Info] = ConsoleColor.Black;
            colors[LogSeverity.Warn] = ConsoleColor.Black;
            colors[LogSeverity.Error] = ConsoleColor.Black;
            colors[LogSeverity.Critical] = ConsoleColor.White;
            return colors;
        }
    }

    public ConcurrentDictionary<LogSeverity, ConsoleColor> Foreground {
        get {
            var value = Interlocked.CompareExchange(ref _Foreground, null, null);
            if (value != null) {
                return value;
            }
            var defaults = ForegroundDefault;
            return Interlocked.CompareExchange(ref _Foreground, defaults, null) ?? defaults;
        }
        set => Interlocked.Exchange(ref _Foreground, value);
    }
    private ConcurrentDictionary<LogSeverity, ConsoleColor> _Foreground;

    public ConcurrentDictionary<LogSeverity, ConsoleColor> Background {
        get {
            var value = Interlocked.CompareExchange(ref _Background, null, null);
            if (value != null) {
                return value;
            }
            var defaults = BackgroundDefault;
            return Interlocked.CompareExchange(ref _Background, defaults, null) ?? defaults;
        }
        set => Interlocked.Exchange(ref _Background, value);
    }
    private ConcurrentDictionary<LogSeverity, ConsoleColor> _Background;

    private static ConsoleColor? TryGetColor(Func<ConsoleColor> getColor) {
        try {
            return getColor();
        }
        catch (Exception) {
            return null;
        }
    }

    private static void TrySetColor(Action<ConsoleColor> setColor, ConsoleColor color) {
        try {
            setColor(color);
        }
        catch (Exception) {
        }
    }

    void ILogService.Log(string name, string data, LogSeverity severity) {
        lock (ConsoleLocker) {
            var prevForeground = TryGetColor(GetForegroundColor);
            var prevBackground = TryGetColor(GetBackgroundColor);
            try {
                if (prevForeground.HasValue && Foreground.TryGetValue(severity, out var foreground)) {
                    TrySetColor(SetForegroundColor, foreground);
                }
                if (prevBackground.HasValue && Background.TryGetValue(severity, out var background)) {
                    TrySetColor(SetBackgroundColor, background);
                }
                Console.WriteLine(data);
            }
            finally {
                if (prevForeground.HasValue) {
                    TrySetColor(SetForegroundColor, prevForeground.Value);
                }
                if (prevBackground.HasValue) {
                    TrySetColor(SetBackgroundColor, prevBackground.Value);
                }
            }
        }
    }

    void ILogService.Complete() {
    }
}
