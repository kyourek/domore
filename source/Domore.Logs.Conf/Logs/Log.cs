using System.Threading;

namespace Domore.Logs;

public static class Log {
    public static class Conf {
        private static readonly
#if NET9_0_OR_GREATER
            Lock
#else
            object
#endif
            Locker = new();

        private static volatile LogConfFile File;

        public static bool Configured =>
            File != null;

        public static bool Configure(string path) {
            if (File == null) {
                lock (Locker) {
                    if (File is null) {
                        var candidate = new LogConfFile(path);
                        try {
                            candidate.Configure(watch: true);
                        }
                        catch {
                            candidate.Dispose();
                            throw;
                        }
                        File = candidate;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
