using System;

namespace Domore.Logs;
public static class Log {
    public static class Conf {
        static Conf() {
            Logging.Completed += Logging_Completed;
        }

        private static readonly object Locker = new();
        private static volatile LogConfFile File;

        private static void Logging_Completed(object sender, EventArgs e) {
            LogConfFile file;
            lock (Locker) {
                file = File;
                File = null;
            }
            try {
                file?.Dispose();
            }
            catch (Exception ex) {
                Logging.Notify(ex);
            }
        }

        /// <summary>
        /// Gets whether a configuration file is currently applied.
        /// </summary>
        /// <remarks>Returns <see langword="false"/> after <see cref="Logging.Complete()"/>.</remarks>
        public static bool Configured =>
            File != null;

        /// <summary>
        /// Configures logging from a file and enables hot reload.
        /// </summary>
        /// <param name="path">The path to the configuration file.</param>
        /// <returns>
        /// <see langword="true"/> if configured; otherwise, <see langword="false"/> if already configured.
        /// </returns>
        /// <remarks>
        /// A failed configuration is disposed and its exception is rethrown. Calling
        /// <see cref="Logging.Complete()"/> disposes the configuration and allows it to be configured again.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="path"/> is <see langword="null"/>.
        /// </exception>
        public static bool Configure(string path) {
            if (path == null) {
                throw new ArgumentNullException(nameof(path));
            }
            lock (Locker) {
                if (File != null) {
                    return false;
                }

                var file = new LogConfFile(path);
                try {
                    file.Configure(watch: true);
                }
                catch {
                    try {
                        file.Dispose();
                    }
                    catch (Exception ex) {
                        Logging.Notify(ex);
                    }
                    throw;
                }

                File = file;
                return true;
            }
        }
    }
}
