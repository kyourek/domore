using NUnit.Framework;
using System.IO;
using System.Linq;

namespace Domore.Conf.Text.Parsing {
    [TestFixture]
    internal sealed class SpecialKeyTest {
        class ObjWithProps {
            public string StringProp { get; set; }
            public double DoubleProp { get; set; }
        }

        [TestCase(@"
            @conf.key[the-obj] = '''
                string prop = abcd
                double prop = 1234
            '''
        ")]
        [TestCase(@"
            something.else = 9876
            @conf.key[the-obj] = '''
                string prop = abcd
                double prop = 1234
            '''
        ")]
        [TestCase(@"
            @conf.key[the-obj] = '''
                string prop = abcd
                something.else = 9876
                double prop = 1234
            '''
        ")]
        public void SpecialKeyConfiguresObject(string conf) {
            var obj = new ObjWithProps();
            Conf.Contain(conf)
                .Configure(obj, "the-obj");
            using (Assert.EnterMultipleScope()) {
                Assert.That(obj.StringProp, Is.EqualTo("abcd"));
                Assert.That(obj.DoubleProp, Is.EqualTo(1234));
            }
        }

        [TestCase(@"
            @conf.key[the-obj.] = '''
                string prop = abcd
                double prop = 1234
            '''")]
        [TestCase(@"
            @conf.key[.the-obj] = '''
                string prop = abcd
                double prop = 1234
            '''")]
        [TestCase(@"
            @conf.key[the-obj] = '''
                .string prop = abcd
                .double prop = 1234
            '''")]
        public void ExtraDotsAreNotAllowed(string conf) {
            var obj = Conf
                .Contain(conf)
                .Configure(new ObjWithProps(), "the-obj");
            using (Assert.EnterMultipleScope()) {
                Assert.That(obj.StringProp, Is.Default);
                Assert.That(obj.DoubleProp, Is.Default);
            }
        }

        [TestCase(@"
            theobj.stringprop = efgh            
            @conf.key[the-obj] = '''
                string prop = abcd
                double prop = 1234
            '''
            theobj.doubleprop = 5678
        ")]
        [TestCase(@"
            theobj.stringprop = efgh            
            @conf.key[the-obj] = '''
                string prop = abcd
                double prop = 1234
                theobj.stringprop = ijkl
            '''
            theobj.doubleprop = 5678
        ")]
        public void SpecialKeyDoesNotAffectOtherObjects(string conf) {
            var obj = new ObjWithProps();
            Conf.Contain(conf)
                .Configure(obj, "theobj");
            using (Assert.EnterMultipleScope()) {
                Assert.That(obj.StringProp, Is.EqualTo("efgh"));
                Assert.That(obj.DoubleProp, Is.EqualTo(5678));
            }
        }

        class MoreComplexObj {
            public ObjWithProps OtherObj { get; set; }
        }

        [TestCase(@"
            @conf.key[MoreComplexObj] = {
                other obj . string prop = '''
                    the
                    other
                    string
                '''
                otherobj.doubleprop = 1.234
            }
        ")]
        [TestCase(@"
            @conf.key[MoreComplexObj] = {
                other obj . string prop = '''
                    the
                    other
                    string
                '''
                @conf.key[notTheObj] = '''
                    otherobj.stringprop = abcd
                '''
                otherobj.doubleprop = 1.234
            }
        ")]
        public void SpecialKeyConfiguresNestedObjects(string conf) {
            var obj = new MoreComplexObj();
            Conf.Contain(conf)
                .Configure(obj);
            using (Assert.EnterMultipleScope()) {
                Assert.That(obj.OtherObj.StringProp.Split('\n').Select(s => s.Trim()), Is.EqualTo(["the", "other", "string"]));
                Assert.That(obj.OtherObj.DoubleProp, Is.EqualTo(1.234));
            }
        }

        [TestCase(@"
            @conf.key[complex] = {
                @conf.key[other obj] = '''
                    stringprop = dcba
                    double prop = 2.345
                '''
            }")]
        [TestCase(@"
            @conf.key[complex] = {
                otherobj.stringprop = abcd
                @conf.key[other obj] = '''
                    stringprop = dcba
                    double prop = 2.345
                '''
            }")]
        public void SpecialKeysCanBeNested(string conf) {
            var obj =
                Conf.Contain(conf)
                    .Configure(new MoreComplexObj(), "complex");
            using (Assert.EnterMultipleScope()) {
                Assert.That(obj.OtherObj.StringProp, Is.EqualTo("dcba"));
                Assert.That(obj.OtherObj.DoubleProp, Is.EqualTo(2.345));
            }
        }

        [TestCase(@"
            @conf.key[complex] = {
                @conf.key[other obj] = '''
                    stringprop = dcba
                    double prop = 2.345
                '''
            }
            complex.other obj.string prop = overridden
            complex.otherobj.doubleprop = 1000
        ")]
        public void SpecialKeysCanBeOverridden(string conf) {
            var obj =
                Conf.Contain(conf)
                    .Configure(new MoreComplexObj(), "complex");
            using (Assert.EnterMultipleScope()) {
                Assert.That(obj.OtherObj.StringProp, Is.EqualTo("overridden"));
                Assert.That(obj.OtherObj.DoubleProp, Is.EqualTo(1000));
            }
        }

        class SimilarObj {
            public string StringProp { get; set; }
            public double DoubleProp { get; set; }
        }

        [TestCase(@"
            string prop = my string
            double prop = 987.654
            @conf.key[the-obj] = '''
                string prop = abcd
                double prop = 1234
            '''
        ")]
        public void SpecialKeysDoNotAffectSimilarObjects(string conf) {
            var obj =
                Conf.Contain(conf)
                    .Configure(new SimilarObj(), "");
            using (Assert.EnterMultipleScope()) {
                Assert.That(obj.StringProp, Is.EqualTo("my string"));
                Assert.That(obj.DoubleProp, Is.EqualTo(987.654));
            }
        }

        [TestCase(@"
            @conf.key[the-obj] = otherobj.string prop = Hello, World!")]
        [TestCase(@"
            @conf.key[the-obj.other obj] = string prop = Hello, World!
        ")]
        public void SpecialKeyCanBeOnASingleLine(string conf) {
            var obj = Conf
                .Contain(conf)
                .Configure(new MoreComplexObj(), "the-obj");
            Assert.That(obj.OtherObj.StringProp, Is.EqualTo("Hello, World!"));
        }

        [TestCase(@"
            other obj . string prop = Goodbye, Earth.
            OtherObj.DoubleProp=65.87", "the-obj")]
        [TestCase(@"
            string prop = Goodbye, Earth.
            DoubleProp=65.87", "the-obj.otherObj")]
        public void SpecialKeyValueCanBeAFilePath(string conf, string key) {
            var tmp = Path.GetTempFileName();
            try {
                File.WriteAllText(tmp, conf);
                var obj = Conf
                    .Contain($"@conf.key[{key}] = {tmp}")
                    .Configure(new MoreComplexObj(), "the-obj");
                using (Assert.EnterMultipleScope()) {
                    Assert.That(obj.OtherObj.StringProp, Is.EqualTo("Goodbye, Earth."));
                    Assert.That(obj.OtherObj.DoubleProp, Is.EqualTo(65.87));
                }
            }
            finally {
                File.Delete(tmp);
            }
        }

        [TestCase(@"
            @conf.key[the-obj] = {
            other obj . string prop = Goodbye, Earth.
            OtherObj.DoubleProp=65.87
            }")]
        public void IncludedFileCanUseSpecialKeys(string conf) {
            var tmp = Path.GetTempFileName();
            try {
                File.WriteAllText(tmp, conf);
                var obj = Conf
                    .Contain($"@conf . Include =  {tmp} ")
                    .Configure(new MoreComplexObj(), "the-obj");
                using (Assert.EnterMultipleScope()) {
                    Assert.That(obj.OtherObj.StringProp, Is.EqualTo("Goodbye, Earth."));
                    Assert.That(obj.OtherObj.DoubleProp, Is.EqualTo(65.87));
                }
            }
            finally {
                File.Delete(tmp);
            }
        }

        [Test]
        public void SpecialKeyCanUseInclude() {
            var tmp = Path.GetTempFileName();
            try {
                File.WriteAllText(tmp, @"
                    other obj . string prop = Goodbye, Earth.
                    OtherObj.DoubleProp=65.87                    
                ");
                var conf = @"
                    @conf.key[the-obj] = '''
                        @conf.include = " + tmp + @"
                    '''
                ";
                var obj = Conf
                    .Contain(conf)
                    .Configure(new MoreComplexObj(), "the-obj");
                using (Assert.EnterMultipleScope()) {
                    Assert.That(obj.OtherObj.StringProp, Is.EqualTo("Goodbye, Earth."));
                    Assert.That(obj.OtherObj.DoubleProp, Is.EqualTo(65.87));
                }
            }
            finally {
                File.Delete(tmp);
            }
        }
    }
}
