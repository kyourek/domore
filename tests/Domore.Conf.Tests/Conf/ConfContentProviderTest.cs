﻿using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace Domore.Conf {
    [TestFixture]
    internal sealed class ConfContentProviderTest {
        private string ProcessPath => _ProcessPath ??= GetProcessPath();
        private string _ProcessPath;

        private string ProcessDir => _ProcessDir ??= Path.GetDirectoryName(ProcessPath);
        private string _ProcessDir;

        private string ConfPath => _ConfPath ??= Path.ChangeExtension(ProcessPath, ".conf");
        private string _ConfPath;

        private string ConfDefaultPath => _ConfDefaultPath ??= ConfPath + ".default";
        private string _ConfDefaultPath;

        private string GetProcessPath() {
            var thisAssemblyPath = Assembly.GetExecutingAssembly().Location;
            var thisAssemblyFile = new FileInfo(thisAssemblyPath);
            var runtime = thisAssemblyFile.Directory.Name;
            var processProject = Path.Combine(thisAssemblyFile.Directory.Parent.Parent.FullName, "Domore.Conf.Tests.Process");
            var processPath = Path.Combine(processProject, runtime, "Domore.Conf.Tests.Process.exe");
            return processPath;
        }

        private string RunProcess(params string[] args) {
            var error = new StringBuilder();
            var output = new StringBuilder();
            using (var process = new Process()) {
                process.StartInfo.Arguments = string.Join(" ", args);
                process.StartInfo.FileName = ProcessPath;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.WorkingDirectory = ProcessDir;
                process.Start();
                process.ErrorDataReceived += (s, e) => { error.AppendLine(e?.Data); };
                process.OutputDataReceived += (s, e) => { output.AppendLine(e?.Data); };
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.WaitForExit();
            }
            Console.WriteLine(error);
            return output.ToString();
        }

        [SetUp]
        public void SetUp() {
            File.Delete(ConfPath);
            File.Delete(ConfDefaultPath);
        }

        [TestCase("Hello, World!")]
        [TestCase("Goodbye, Earth...")]
        public void ConfDefaultIsUsedIfConfIsNotFound(string greeting) {
            File.WriteAllText(ConfDefaultPath, $"Program.Greeting = {greeting}");
            var output = RunProcess();
            Assert.That(output.Trim(), Is.EqualTo(greeting));
        }

        [TestCase("Hello, World!")]
        [TestCase("Goodbye, Earth...")]
        public void ConfDefaultIsCopiedToConfFile(string greeting) {
            File.WriteAllText(ConfDefaultPath, $"Program.Greeting = {greeting}");
            RunProcess();
            var expected = File.ReadAllText(ConfDefaultPath);
            var actual = File.ReadAllText(ConfPath);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("Hello, World!")]
        [TestCase("Goodbye, Earth...")]
        public void ConfIsUsedAheadOfConfDefault(string greeting) {
            File.WriteAllText(ConfDefaultPath, $"Program.Greeting = x");
            File.WriteAllText(ConfPath, $"Program.Greeting = {greeting}");
            var output = RunProcess();
            Assert.That(output.Trim(), Is.EqualTo(greeting));
        }

        [TestCase("Hello, World!", "$")]
        [TestCase("Hello, World!", "@")]
        [TestCase("Hello, World!", "@@")]
        [TestCase("Goodbye, Earth...", "$")]
        [TestCase("Goodbye, Earth...", "@")]
        [TestCase("Goodbye, Earth...", "@@")]
        public void AdditionalConfFileIsIncluded(string greeting, string special) {
            var tempPath = Path.GetTempFileName();
            try {
                File.WriteAllText(tempPath, $"Program.Greeting = {greeting}");
                File.WriteAllText(ConfPath, $"{special}.include = {tempPath}");
                var output = RunProcess(special);
                Assert.That(output.Trim(), Is.EqualTo(greeting));
            }
            finally {
                File.Delete(tempPath);
            }
        }
    }
}
