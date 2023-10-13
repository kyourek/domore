using System;
using System.Collections.Generic;

namespace Domore.Logs.Service {
    internal sealed class ConsoleLog : ILogService {
        private static Dictionary<LogSeverity, ConsoleColor> ForegroundDefault => new Dictionary<LogSeverity, ConsoleColor> {
            { LogSeverity.Debug,    ConsoleColor.Cyan },
            { LogSeverity.Info,     ConsoleColor.Gray },
            { LogSeverity.Warn,     ConsoleColor.Yellow },
            { LogSeverity.Error,    ConsoleColor.Red },
            { LogSeverity.Critical, ConsoleColor.Black }
        };

        private static Dictionary<LogSeverity, ConsoleColor> BackgroundDefault => new Dictionary<LogSeverity, ConsoleColor> {
            { LogSeverity.Debug,    ConsoleColor.Black },
            { LogSeverity.Info,     ConsoleColor.Black },
            { LogSeverity.Warn,     ConsoleColor.Black },
            { LogSeverity.Error,    ConsoleColor.Black },
            { LogSeverity.Critical, ConsoleColor.White }
        };

        public Dictionary<LogSeverity, ConsoleColor> Foreground {
            get => _Foreground ?? (_Foreground = ForegroundDefault);
            set => _Foreground = value;
        }
        private Dictionary<LogSeverity, ConsoleColor> _Foreground;

        public Dictionary<LogSeverity, ConsoleColor> Background {
            get => _Background ?? (_Background = BackgroundDefault);
            set => _Background = value;
        }
        private Dictionary<LogSeverity, ConsoleColor> _Background;

        public void Log(string name, string data, LogSeverity severity) {
            var prevForeground = Console.ForegroundColor;
            var prevBackground = Console.BackgroundColor;
            try {
                Console.ForegroundColor = Foreground.TryGetValue(severity, out var foreground) ? foreground : prevForeground;
                Console.BackgroundColor = Background.TryGetValue(severity, out var background) ? background : prevBackground;
                Console.WriteLine(data);
            }
            finally {
                Console.ForegroundColor = prevForeground;
                Console.BackgroundColor = prevBackground;
            }
        }
    }
}
