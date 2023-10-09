using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Domore.Conf.IO {
    [TestFixture]
    public sealed class FileOrTextContentProviderTest {
        private FileOrTextContentProvider Subject {
            get => _Subject ?? (_Subject = new FileOrTextContentProvider());
            set => _Subject = value;
        }
        private FileOrTextContentProvider _Subject;

        [SetUp]
        public void SetUp() {
            Subject = null;
        }

        private class ClassWithListExposedAsICollection {
            public ICollection<Inner> Inners {
                get => _Inners ?? (_Inners = new List<Inner>());
                set => _Inners = value;
            }
            private ICollection<Inner> _Inners;

            public sealed class Inner {
                public double Value { get; set; }
            }
        }

        [Test]
        public void Configure_UsesFileIfItExists() {
            var path = Path.GetTempFileName();
            var container = new ConfContainer { ContentProvider = Subject, Source = path };
            File.WriteAllText(path, @"
                item.inners[0].value = 1.1
                item.inners[1].value = 1.2
                item.inners[2].value = 1.3
            ");
            var obj = container.Configure(new ClassWithListExposedAsICollection(), "item");
            CollectionAssert.AreEqual(new[] { 1.1, 1.2, 1.3 }, obj.Inners.Select(i => i.Value));
        }

        [Test]
        public void Sources_IncludesFileIfFileExists() {
            var path = Path.GetTempFileName();
            var container = new ConfContainer { ContentProvider = Subject, Source = path };
            File.WriteAllText(path, @"
                item.inners[0].value = 1.1
                item.inners[1].value = 1.2
                item.inners[2].value = 1.3
            ");
            var obj = container.Configure(new ClassWithListExposedAsICollection(), "item");
            var expected = new string[] {
                path,
                @"
                item.inners[0].value = 1.1
                item.inners[1].value = 1.2
                item.inners[2].value = 1.3
            " };
            var actual = container.Sources;
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Configure_UsesTextIfFileDoesNotExist() {
            var container = new ConfContainer {
                ContentProvider = Subject,
                Source = @"
                    item.inners[0].value = 1.1
                    item.inners[1].value = 1.2
                    item.inners[2].value = 1.3
                "
            };
            var obj = container.Configure(new ClassWithListExposedAsICollection(), "item");
            CollectionAssert.AreEqual(new[] { 1.1, 1.2, 1.3 }, obj.Inners.Select(i => i.Value));
        }
    }
}
