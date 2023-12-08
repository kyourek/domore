using NUnit.Framework;
using System;
using System.IO;
using System.Threading;

namespace Domore.IO {
    public sealed class PathFormatterTest {
        private PathFormatter Subject {
            get => _Subject ?? (_Subject = new PathFormatter());
            set => _Subject = value;
        }
        private PathFormatter _Subject;

        [SetUp]
        public void SetUp() {
            Subject = null;
        }

        [TestCase("CommonApplicationData", Environment.SpecialFolder.CommonApplicationData)]
        [TestCase("commondesktopDIRECTORY", Environment.SpecialFolder.CommonDesktopDirectory)]
        [TestCase("personal", Environment.SpecialFolder.Personal)]
        [TestCase("MYDocuments", Environment.SpecialFolder.MyDocuments)]
        public void SpecialFolderNameIsExpandedToPath(string key, Environment.SpecialFolder specialFolder) {
            var actual = Subject.Expand($"{{{key}}}");
            var expected = Environment.GetFolderPath(specialFolder, Environment.SpecialFolderOption.DoNotVerify);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("system", Environment.SpecialFolder.System)]
        [TestCase("WINDOWS", Environment.SpecialFolder.Windows)]
        public void SpecialFolderNameIsExpandedInPath(string key, Environment.SpecialFolder specialFolder) {
            var actual = Subject.Expand(string.Join(Path.DirectorySeparatorChar.ToString(), $"{{{key}}}", "path", "to", "file.f"));
            var expected = Path.Combine(Environment.GetFolderPath(specialFolder, Environment.SpecialFolderOption.DoNotVerify), "path", "to", "file.f");
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("programfiles", Environment.SpecialFolder.ProgramFiles)]
        [TestCase("APPLICATIONDATA", Environment.SpecialFolder.ApplicationData)]
        public void SpecialFolderNameIsExpandedInPathWithoutLeadingSeparator(string key, Environment.SpecialFolder specialFolder) {
            var path = $"{{{key}}}" + string.Join(Path.DirectorySeparatorChar.ToString(), "path", "to", "file.f");
            var actual = Subject.Expand(path);
            var expected = Path.Combine(Environment.GetFolderPath(specialFolder, Environment.SpecialFolderOption.DoNotVerify), "path", "to", "file.f");
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("NotASpecialFolder")]
        public void NothingIsDoneIfSpecialFolderIsNotFound(string key) {
            var actual = Subject.Expand(string.Join(Path.DirectorySeparatorChar.ToString(), $"{{{key}}}", "path", "to", "file.f"));
            var expected = actual;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("StillNotASpecialFolder")]
        public void NothingIsDoneForNonExistentSpecialFolder(string key) {
            var actual = Subject.Expand($"{{{key}}}");
            var expected = actual;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("programfiles")]
        [TestCase("APPLICATIONDATA")]
        public void NothingIsDoneIfBracketIsNotClosed(string key) {
            var actual = Subject.Expand(string.Join(Path.DirectorySeparatorChar.ToString(), $"{{{key}", "path", "to", "file.f"));
            var expected = actual;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("programfiles")]
        [TestCase("APPLICATIONDATA")]
        public void NothingIsDoneIfBracketIsNotOpened(string key) {
            var actual = Subject.Expand(string.Join(Path.DirectorySeparatorChar.ToString(), $"{key}}}", "path", "to", "file.f"));
            var expected = actual;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void NullIsReturnedIfPathIsNull() {
            Assert.That(Subject.Expand(null), Is.Null);
        }

        [Test]
        public void EmptyIsReturnedIfPathIsEmpty() {
            Assert.That(Subject.Expand(""), Is.EqualTo(""));
        }

        [Test]
        public void OpenCloseBracketsAreReturned() {
            Assert.That(Subject.Expand("{}"), Is.EqualTo("{}"));
        }

        [Test]
        public void OpenBracketIsReturned() {
            Assert.That(Subject.Expand("{"), Is.EqualTo("{"));
        }

        [Test]
        public void CloseBracketIsReturned() {
            Assert.That(Subject.Expand("}"), Is.EqualTo("}"));
        }

        [Test]
        public void WhiteSpaceIsReturned() {
            Assert.That(Subject.Expand("    "), Is.EqualTo("    "));
        }

        [Test]
        public void TrailingSlashIsPreserved() {
            var actual = Subject.Expand("{ApplicationData}/");
            var expected = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify) + "/";
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void TrailingBackSlashIsPreserved() {
            if (Path.DirectorySeparatorChar == '\\') {
                var actual = Subject.Expand("{ApplicationData}\\");
                var expected = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify) + "\\";
                Assert.That(actual, Is.EqualTo(expected));
            }
        }

        private sealed class Tunnel : MarshalByRefObject {
            public string Format(string path) {
                return new PathFormatter().Format(path);
            }
        }

        [Test]
        public void AppDomainFriendlyNameIsReplacedInPath() {
#if NETFRAMEWORK
            var curDomain = AppDomain.CurrentDomain;
            var newDomain = AppDomain.CreateDomain(
                friendlyName: "What's up? I'm a new application domain",
                securityInfo: curDomain.Evidence,
                appBasePath: curDomain.BaseDirectory,
                appRelativeSearchPath: curDomain.RelativeSearchPath,
                shadowCopyFiles: false);
            try {
                var tunnel = (Tunnel)newDomain.CreateInstanceAndUnwrap(typeof(Tunnel).Assembly.FullName, typeof(Tunnel).FullName);
                var actual = tunnel.Format(@"Z:\path\{appdomain.Friendlyname}\file.txt");
                var expected = @"Z:\path\What's up_ I'm a new application domain\file.txt";
                Assert.That(actual, Is.EqualTo(expected));
            }
            finally {
                AppDomain.Unload(newDomain);
            }
#endif
        }

        [Test]
        public void AppDomainFriendlyNameIsReplacedInFileName() {
#if NETFRAMEWORK
            var curDomain = AppDomain.CurrentDomain;
            var newDomain = AppDomain.CreateDomain(
                friendlyName: "What's up? I'm a new application domain",
                securityInfo: curDomain.Evidence,
                appBasePath: curDomain.BaseDirectory,
                appRelativeSearchPath: curDomain.RelativeSearchPath,
                shadowCopyFiles: false);
            try {
                var tunnel = (Tunnel)newDomain.CreateInstanceAndUnwrap(typeof(Tunnel).Assembly.FullName, typeof(Tunnel).FullName);
                var actual = tunnel.Format(@"{appdomain.Friendlyname}.txt");
                var expected = @"What's up_ I'm a new application domain.txt";
                Assert.That(actual, Is.EqualTo(expected));
            }
            finally {
                AppDomain.Unload(newDomain);
            }
#endif
        }

        [Test]
        public void ThreadNameIsReplacedInPath() {
            var actual = default(string);
            var thread = new Thread(_ => {
                Thread.CurrentThread.Name = "::the THREAD::";
                actual = Subject.Format(@"z:\some path\to\thread {THREAD.NAME}\{thread.name}.file");
            });
            thread.Start();
            thread.Join();
            var expected = @"z:\some path\to\thread __the THREAD__\__the THREAD__.file";
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void ThreadNameIsReplacedAlongWithDirectorySeparatorChars() {
            var actual = default(string);
            var thread = new Thread(_ => {
                Thread.CurrentThread.Name = "::the THREAD::";
                actual = Subject.Format(@"z:\some path\to/thread {THREAD.NAME}/{thread.name}.file");
            });
            thread.Start();
            thread.Join();
            var expected = @"z:\some path\to\thread __the THREAD__\__the THREAD__.file";
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void ManagedThreadIdIsReplacedInPath() {
            var actual = default(string);
            var expected = default(string);
            var thread = new Thread(_ => {
                actual = Subject.Format(@"z:\some path\to\thread {THREAD.managedthreadid}\{thread.managedthreadID}.file");
                expected = @$"z:\some path\to\thread {Thread.CurrentThread.ManagedThreadId}\{Thread.CurrentThread.ManagedThreadId}.file";
            });
            thread.Start();
            thread.Join();
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void ManagedThreadIdIsReplacedInWorkingDirectoryPath() {
            var actual = default(string);
            var expected = default(string);
            var thread = new Thread(_ => {
                actual = Subject.Format(@"z:some path\to\thread {THREAD.managedthreadid}\{thread.managedthreadID}.file");
                expected = @$"z:some path\to\thread {Thread.CurrentThread.ManagedThreadId}\{Thread.CurrentThread.ManagedThreadId}.file";
            });
            thread.Start();
            thread.Join();
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void SameStringIsReturnedIfThereIsNothingToReplace() {
            var actual = Subject.Format(@"C:\path\to\file.txt");
            var expected = @"C:\path\to\file.txt";
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void NullReturnsEmptyString() {
            Assert.That(Subject.Format(null), Is.EqualTo(""));
        }

        [Test]
        public void EmptyStringIsReturned() {
            Assert.That(Subject.Format(""), Is.EqualTo(""));
        }
    }
}
