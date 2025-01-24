using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Domore {
    using Conventions;
    using Logs;

    public abstract class ReleaseAction {
        protected ILog Log =>
            _Log ?? (
            _Log = Logging.For(GetType()));
        private ILog _Log;

        protected string Process(string fileName, params string[] arguments) {
            var outp = "";
            var args = string.Join(" ", arguments);
            Log.Info($"{fileName} {args}");

            if (ProcessPath.TryGetValue(fileName, out string processPath)) {
                fileName = processPath;
                Log.Info($"Using {fileName}");
            }

            void errorDataReceived(object sender, DataReceivedEventArgs e) {
                var data = e?.Data;
                if (data != null) {
                    Log.Warn(data);
                }
            }

            void outputDataReceived(object sender, DataReceivedEventArgs e) {
                var data = e?.Data;
                if (data != null) {
                    outp += data;
                    Log.Info(data);
                }
            }

            using (var process = new Process()) {
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = args;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.WorkingDirectory = Directory.Exists(CodeBase.Path) ? CodeBase.Path : "";
                process.ErrorDataReceived += errorDataReceived;
                process.OutputDataReceived += outputDataReceived;
                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.WaitForExit();

                var exitCode = process.ExitCode;
                if (exitCode != 0) throw new Exception("Process error (exit code '" + exitCode + "')");
            }

            return outp;
        }

        public Solution Solution { get; set; }
        public CodeBase CodeBase { get; set; }

        public IDictionary<string, string> ProcessPath {
            get => _ProcessPath ?? (_ProcessPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
            set => _ProcessPath = value;
        }
        private IDictionary<string, string> _ProcessPath;

        public abstract void Work();
    }
}
