using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Domore.Logs.Service;

internal sealed class ConsoleLog : ILogService {
    private static readonly
#if NET9_0_OR_GREATER
        Lock
#else
        object
#endif
        ConsoleLocker = new();

    private readonly Func<ConsoleColor> GetForegroundColor;
    private readonly Action<ConsoleColor> SetForegroundColor;
    private readonly Func<ConsoleColor> GetBackgroundColor;
    private readonly Action<ConsoleColor> SetBackgroundColor;

    private static ConcurrentDictionary<LogSeverity, ConsoleColor> ForegroundDefault {
        get {
            var
            colors = new ConcurrentDictionary<LogSeverity, ConsoleColor>();
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
            var
            colors = new ConcurrentDictionary<LogSeverity, ConsoleColor>();
            colors[LogSeverity.Debug] = ConsoleColor.Black;
            colors[LogSeverity.Info] = ConsoleColor.Black;
            colors[LogSeverity.Warn] = ConsoleColor.Black;
            colors[LogSeverity.Error] = ConsoleColor.Black;
            colors[LogSeverity.Critical] = ConsoleColor.White;
            return colors;
        }
    }

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

    internal ConsoleLog(Func<ConsoleColor> getForegroundColor,
                        Action<ConsoleColor> setForegroundColor,
                        Func<ConsoleColor> getBackgroundColor,
                        Action<ConsoleColor> setBackgroundColor) {
        GetForegroundColor = getForegroundColor;
        SetForegroundColor = setForegroundColor;
        GetBackgroundColor = getBackgroundColor;
        SetBackgroundColor = setBackgroundColor;
    }

    public ConsoleLog() : this(getForegroundColor: () => Console.ForegroundColor,
                               setForegroundColor: color => Console.ForegroundColor = color,
                               getBackgroundColor: () => Console.BackgroundColor,
                               setBackgroundColor: color => Console.BackgroundColor = color) {
    }

    public ConcurrentDictionary<LogSeverity, ConsoleColor> Foreground {
        get {
            var value = Interlocked.CompareExchange(ref field, null, null);
            if (value != null) {
                return value;
            }
            var defaults = ForegroundDefault;
            return Interlocked.CompareExchange(ref field, defaults, null) ?? defaults;
        }
        set => Interlocked.Exchange(ref field, value);
    }

    public ConcurrentDictionary<LogSeverity, ConsoleColor> Background {
        get {
            var value = Interlocked.CompareExchange(ref field, null, null);
            if (value != null) {
                return value;
            }
            var defaults = BackgroundDefault;
            return Interlocked.CompareExchange(ref field, defaults, null) ?? defaults;
        }
        set => Interlocked.Exchange(ref field, value);
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
