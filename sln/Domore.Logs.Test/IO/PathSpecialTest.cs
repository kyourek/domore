using NUnit.Framework;
using System;
using System.IO;

namespace Domore.IO {
    public sealed class PathSpecialTest {
        private PathSpecial Subject {
            get => _Subject ?? (_Subject = new PathSpecial());
            set => _Subject = value;
        }
        private PathSpecial _Subject;

        [SetUp]
        public void SetUp() {
            Subject = null;
        }

        [TestCase("CommonApplicationData", Environment.SpecialFolder.CommonApplicationData)]
        [TestCase("commondesktopDIRECTORY", Environment.SpecialFolder.CommonDesktopDirectory)]
        [TestCase("personal", Environment.SpecialFolder.Personal)]
        [TestCase("MYDocuments", Environment.SpecialFolder.MyDocuments)]
        public void SpecialFolderNameIsExpandedToPath(string key, Environment.SpecialFolder specialFolder) {
            var actual = Subject.Expand($"<{key}>");
            var expected = Environment.GetFolderPath(specialFolder, Environment.SpecialFolderOption.DoNotVerify);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("system", Environment.SpecialFolder.System)]
        [TestCase("WINDOWS", Environment.SpecialFolder.Windows)]
        public void SpecialFolderNameIsExpandedInPath(string key, Environment.SpecialFolder specialFolder) {
            var actual = Subject.Expand(string.Join(Path.DirectorySeparatorChar.ToString(), $"<{key}>", "path", "to", "file.f"));
            var expected = Path.Combine(Environment.GetFolderPath(specialFolder, Environment.SpecialFolderOption.DoNotVerify), "path", "to", "file.f");
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("programfiles", Environment.SpecialFolder.ProgramFiles)]
        [TestCase("APPLICATIONDATA", Environment.SpecialFolder.ApplicationData)]
        public void SpecialFolderNameIsExpandedInPathWithoutLeadingSeparator(string key, Environment.SpecialFolder specialFolder) {
            var path = $"<{key}>" + string.Join(Path.DirectorySeparatorChar.ToString(), "path", "to", "file.f");
            var actual = Subject.Expand(path);
            var expected = Path.Combine(Environment.GetFolderPath(specialFolder, Environment.SpecialFolderOption.DoNotVerify), "path", "to", "file.f");
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("NotASpecialFolder")]
        public void NothingIsDoneIfSpecialFolderIsNotFound(string key) {
            var actual = Subject.Expand(string.Join(Path.DirectorySeparatorChar.ToString(), $"<{key}>", "path", "to", "file.f"));
            var expected = actual;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("StillNotASpecialFolder")]
        public void NothingIsDoneForNonExistentSpecialFolder(string key) {
            var actual = Subject.Expand($"<{key}>");
            var expected = actual;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("programfiles")]
        [TestCase("APPLICATIONDATA")]
        public void NothingIsDoneIfBracketIsNotClosed(string key) {
            var actual = Subject.Expand(string.Join(Path.DirectorySeparatorChar.ToString(), $"<{key}", "path", "to", "file.f"));
            var expected = actual;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("programfiles")]
        [TestCase("APPLICATIONDATA")]
        public void NothingIsDoneIfBracketIsNotOpened(string key) {
            var actual = Subject.Expand(string.Join(Path.DirectorySeparatorChar.ToString(), $"{key}>", "path", "to", "file.f"));
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
            Assert.That(Subject.Expand("<>"), Is.EqualTo("<>"));
        }

        [Test]
        public void OpenBracketIsReturned() {
            Assert.That(Subject.Expand("<"), Is.EqualTo("<"));
        }

        [Test]
        public void CloseBracketIsReturned() {
            Assert.That(Subject.Expand(">"), Is.EqualTo(">"));
        }

        [Test]
        public void WhiteSpaceIsReturned() {
            Assert.That(Subject.Expand("    "), Is.EqualTo("    "));
        }

        [Test]
        public void TrailingSlashIsPreserved() {
            var actual = Subject.Expand("<ApplicationData>/");
            var expected = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify) + "/";
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void TrailingBackSlashIsPreserved() {
            if (Path.DirectorySeparatorChar == '\\') {
                var actual = Subject.Expand("<ApplicationData>\\");
                var expected = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify) + "\\";
                Assert.That(actual, Is.EqualTo(expected));
            }
        }
    }
}
