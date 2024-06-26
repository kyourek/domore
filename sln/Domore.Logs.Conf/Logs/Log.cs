﻿namespace Domore.Logs {
    public static class Log {
        public static class Conf {
            private static readonly object Locker = new();
            private static volatile LogConfFile File;

            public static bool Configured =>
                File != null;

            public static bool Configure(string path) {
                if (File == null) {
                    lock (Locker) {
                        if (File == null) {
                            File = new LogConfFile(path);
                            File.Configure(watch: true);
                            return true;
                        }
                    }
                }
                return false;
            }
        }
    }
}
