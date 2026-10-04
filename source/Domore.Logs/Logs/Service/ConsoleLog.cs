using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Domore.Logs.Service; 
internal sealed class ConsoleLog : ILogService {
    private static ConcurrentDictionary<LogSeverity, ConsoleColor> ForegroundDefault => new(
        new Dictionary<LogSeverity, ConsoleColor> {
            { LogSeverity.Debug,    ConsoleColor.Cyan },
            { LogSeverity.Info,     ConsoleColor.Gray },
            { LogSeverity.Warn,     ConsoleColor.Yellow },
            { LogSeverity.Error,    ConsoleColor.Red },
            { LogSeverity.Critical, ConsoleColor.Black }
        });

    private static ConcurrentDictionary<LogSeverity, ConsoleColor> BackgroundDefault => new(
        new Dictionary<LogSeverity, ConsoleColor> {
            { LogSeverity.Debug,    ConsoleColor.Black },
            { LogSeverity.Info,     ConsoleColor.Black },
            { LogSeverity.Warn,     ConsoleColor.Black },
            { LogSeverity.Error,    ConsoleColor.Black },
            { LogSeverity.Critical, ConsoleColor.White }
        });

    public ConcurrentDictionary<LogSeverity, ConsoleColor> Foreground {
        get {
            var foreground = Interlocked.CompareExchange(ref _Foreground, null, null);
            if (foreground != null) {
                return foreground;
            }
            foreground = ForegroundDefault;
            return Interlocked.CompareExchange(ref _Foreground, foreground, null) ?? foreground;
        }
        set => Interlocked.Exchange(ref _Foreground, value);
    }
    private ConcurrentDictionary<LogSeverity, ConsoleColor> _Foreground;

    public ConcurrentDictionary<LogSeverity, ConsoleColor> Background {
        get {
            var background = Interlocked.CompareExchange(ref _Background, null, null);
            if (background != null) {
                return background;
            }
            background = BackgroundDefault;
            return Interlocked.CompareExchange(ref _Background, background, null) ?? background;
        }
        set => Interlocked.Exchange(ref _Background, value);
    }
    private ConcurrentDictionary<LogSeverity, ConsoleColor> _Background;

    void ILogService.Log(string name, string data, LogSeverity severity) {
        var prevForeground = Console.ForegroundColor;
        var prevBackground = Console.BackgroundColor;
        var foregroundColors = Foreground;
        var backgroundColors = Background;
        try {
            Console.ForegroundColor = foregroundColors.TryGetValue(severity, out var foreground)
                ? foreground
                : prevForeground;
            Console.BackgroundColor = backgroundColors.TryGetValue(severity, out var background)
                ? background
                : prevBackground;
            Console.WriteLine(data);
        }
        finally {
            Console.ForegroundColor = prevForeground;
            Console.BackgroundColor = prevBackground;
        }
    }

    void ILogService.Complete() {
    }
}
