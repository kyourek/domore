using NUnit.Framework;
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
    }
}
