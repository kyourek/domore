using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Domore.Conf.IO {
    [TestFixture]
    public sealed class FileContentProviderTest {
        private string FilePath {
            get => _FilePath ?? (_FilePath = Path.GetTempFileName());
            set => _FilePath = value;
        }
        private string _FilePath;

        private FileContentProvider Subject {
            get => _Subject ?? (_Subject = new FileContentProvider());
            set => _Subject = value;
        }
        private FileContentProvider _Subject;

        private ConfContainer Container {
            get => _Container ?? (_Container = new ConfContainer { ContentProvider = Subject, Source = FilePath });
            set => _Container = value;
        }
        private ConfContainer _Container;

        [SetUp]
        public void SetUp() {
            Subject = null;
            FilePath = null;
            Container = null;
        }

        [TearDown]
        public void TearDown() {
            if (_FilePath != null) {
                File.Delete(_FilePath);
            }
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
        public void Configure_ReadsFile() {
            File.WriteAllText(FilePath, @"
                item.inners[0].value = 1.1
                item.inners[1].value = 1.2
                item.inners[2].value = 1.3
            ");
            var obj = Container.Configure(new ClassWithListExposedAsICollection(), "item");
            CollectionAssert.AreEqual(new[] { 1.1, 1.2, 1.3 }, obj.Inners.Select(i => i.Value));
        }

        [Test]
        public void Sources_IncludesFile() {
            File.WriteAllText(FilePath, @"
                item.inners[0].value = 1.1
                item.inners[1].value = 1.2
                item.inners[2].value = 1.3
            ");
            var obj = Container.Configure(new ClassWithListExposedAsICollection(), "item");
            var expected = new string[] {
                FilePath,
                @"
                item.inners[0].value = 1.1
                item.inners[1].value = 1.2
                item.inners[2].value = 1.3
            " };
            var actual = Container.Sources;
            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
