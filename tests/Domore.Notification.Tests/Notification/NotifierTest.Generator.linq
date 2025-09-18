<Query Kind="Statements">
  <NuGetReference>Domore.Conf</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Domore.Conf</Namespace>
  <Namespace>Domore.Conf.Extensions</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

var types = new[] {
    "byte",
    "byte?",
    "sbyte",
    "sbyte?",
    "char",
    "char?",
    "decimal",
    "decimal?",
    "double",
    "double?",
    "float",
    "float?",
    "int",
    "int?",
    "uint",
    "uint?",
    "nint",
    "nint?",
    "nuint",
    "nuint?",
    "long",
    "long?",
    "ulong",
    "ulong?",
    "short",
    "short?",
    "ushort",
    "ushort?",
    "object" }
        .Select(t => new { T = t, Z = $"({t})0", O = $"({t})1" })
        .Concat(new[] {
            new { T = "bool", Z = "false", O = "true " },
            new { T = "string", Z = "\"0\"", O = "\"1\"" },
            new { T = "DateTime", Z = "DateTime.MinValue", O = "DateTime.MaxValue" },
            new { T = "TimeSpan", Z = "TimeSpan.Zero", O = "TimeSpan.MaxValue" }
        });
var file = new StringBuilder();
var f = new Action<string>(l => file.AppendLine(l));
f(@"using NUnit.Framework;");
f(@"using System;");
f(@"using System.Collections.Generic;");
f(@"using System.ComponentModel;");
f(@"using System.Linq;");
f(@"");
f(@"namespace Domore.Notification {");
f(@"    public partial class NotifierTest {");
foreach (var type in types) {
    var methods = new StringBuilder();
    var m = new Action<string>(s => methods.AppendLine("        " + s));
    m(@"[Test]");
    m(@"public void Change_T_1_RaisesPropertyChanged() {");
    m(@"    var actual = """";");
    m(@"    Subject.PropertyChanged += (s, e) => actual = e.PropertyName;");
    m(@"");
    m(@"    var field = $Z;");
    m(@"    Subject.Change(ref field, $O, ""expected"");");
    m(@"");
    m(@"    Assert.That(actual, Is.EqualTo(""expected""));");
    m(@"}");
	m(@"");
	m(@"[Test]");
	m(@"public void Change_T_1_RaisesPropertyChanging() {");
	m(@"    var actual = """";");
	m(@"    Subject.PropertyChanging += (s, e) => actual = e.PropertyName;");
	m(@"");
	m(@"    var field = $Z;");
	m(@"    Subject.Change(ref field, $O, ""expected"");");
	m(@"");
	m(@"    Assert.That(actual, Is.EqualTo(""expected""));");
	m(@"}");
	m(@"");
	m(@"[Test]");
	m(@"public void Change_T_1_RaisesPropertyChangingBeforePropertyChanged() {");
	m(@"    var actual = """";");
	m(@"    Subject.PropertyChanged += (s, e) => actual = e.PropertyName;");
	m(@"    Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);");
	m(@"");
	m(@"    var field = $Z;");
	m(@"    Subject.Change(ref field, $O, ""expected"");");
	m(@"}");
	m(@"");
	m(@"[Test]");
    m(@"public void Change_T_1_DoesNotRaisePropertyChanged() {");
    m(@"    var actual = """";");
    m(@"    Subject.PropertyChanged += (s, e) => actual = ""fail"";");
    m(@"");
    m(@"    var field = $Z;");
    m(@"    Subject.Change(ref field, $Z, ""expected"");");
    m(@"");
    m(@"    Assert.That(actual, Is.EqualTo(""""));");
    m(@"}");
	m(@"");
	m(@"[Test]");
	m(@"public void Change_T_1_DoesNotRaisePropertyChanging() {");
	m(@"    var actual = """";");
	m(@"    Subject.PropertyChanging += (s, e) => actual = ""fail"";");
	m(@"");
	m(@"    var field = $Z;");
	m(@"    Subject.Change(ref field, $Z, ""expected"");");
	m(@"");
	m(@"    Assert.That(actual, Is.EqualTo(""""));");
	m(@"}");
	m(@"");
	m(@"[Test]");
    m(@"public void Change_T_1_RaisesPropertyChangedDependents() {");
    m(@"    var actual = new List<string>();");
    m(@"    var expected = new List<string> { ""1"", ""2"", ""3"" };");
    m(@"    Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);");
    m(@"");
    m(@"    var field = $Z;");
    m(@"    Subject.Change(ref field, $O, expected[0], expected[1], expected[2]);");
    m(@"");
    m(@"    Assert.That(actual, Is.EqualTo(expected));");
    m(@"}");
	m(@"");
	m(@"[Test]");
	m(@"public void Change_T_1_RaisesPropertyChangingDependents() {");
	m(@"    var actual = new List<string>();");
	m(@"    var expected = new List<string> { ""1"", ""2"", ""3"" };");
	m(@"    Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);");
	m(@"");
	m(@"    var field = $Z;");
	m(@"    Subject.Change(ref field, $O, expected[0], expected[1], expected[2]);");
	m(@"");
	m(@"    Assert.That(actual, Is.EqualTo(expected));");
	m(@"}");
	m(@"");
	m(@"[Test]");
	m(@"public void Change_T_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {");
	m(@"    var actual = new List<string>();");
	m(@"    var expected = new List<string> { ""1"", ""2"", ""3"" };");
	m(@"    Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));");
	m(@"    Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);");
	m(@"");
	m(@"    var field = $Z;");
	m(@"    Subject.Change(ref field, $O, expected[0], expected[1], expected[2]);");
	m(@"}");
	m(@"");
	m(@"[Test]");
    m(@"public void Change_T_1_DoesNotRaisePropertyChangedDependents() {");
    m(@"    var actual = new List<string>();");
    m(@"    var expected = new List<string> { ""1"", ""2"", ""3"" };");
    m(@"    Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);");
    m(@"");
    m(@"    var field = $Z;");
    m(@"    Subject.Change(ref field, $Z, expected[0], expected[1], expected[2]);");
    m(@"");
    m(@"    Assert.That(actual, Is.Empty);");
    m(@"}");
	m(@"");
	m(@"[Test]");
	m(@"public void Change_T_1_DoesNotRaisePropertyChangingDependents() {");
	m(@"    var actual = new List<string>();");
	m(@"    var expected = new List<string> { ""1"", ""2"", ""3"" };");
	m(@"    Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);");
	m(@"");
	m(@"    var field = $Z;");
	m(@"    Subject.Change(ref field, $Z, expected[0], expected[1], expected[2]);");
	m(@"");
	m(@"    Assert.That(actual, Is.Empty);");
	m(@"}");
	m(@"");
	m(@"[Test]");
    m(@"public void Change_T_1_RaisesPropertyChangedEventArgs() {");
    m(@"    var actual = default(PropertyChangedEventArgs);");
    m(@"    var expected = new PropertyChangedEventArgs(""expected"");");
    m(@"    Subject.PropertyChanged += (s, e) => actual = e;");
    m(@"");
    m(@"    var field = $Z;");
    m(@"    Subject.Change(ref field, $O, expected);");
    m(@"");
    m(@"    Assert.That(actual, Is.SameAs(expected));");
    m(@"}");
	m(@"");
	m(@"[Test]");
	m(@"public void Change_T_1_RaisesPropertyChangingEventArgs() {");
	m(@"    var actual = default(PropertyChangingEventArgs);");
	m(@"    var expected = new PropertyChangingEventArgs(""expected"");");
	m(@"    Subject.PropertyChanging += (s, e) => actual = e;");
	m(@"");
	m(@"    var field = $Z;");
	m(@"    Subject.Change(ref field, $O, new PropertyChangedEventArgs(expected.PropertyName));");
	m(@"");
	m(@"    Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));");
	m(@"}");
	m(@"");
	m(@"[Test]");
	m(@"public void Change_T_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {");
	m(@"    var actual = default(PropertyChangingEventArgs);");
	m(@"    var expected = new PropertyChangingEventArgs(""expected"");");
	m(@"    Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));");
	m(@"    Subject.PropertyChanging += (s, e) => actual = e;");
	m(@"");
	m(@"    var field = $Z;");
	m(@"    Subject.Change(ref field, $O, new PropertyChangedEventArgs(expected.PropertyName));");
	m(@"}");
	m(@"");
	m(@"[Test]");
    m(@"public void Change_T_1_DoesNotRaisePropertyChangedEventArgs() {");
    m(@"    var actual = default(PropertyChangedEventArgs);");
    m(@"    Subject.PropertyChanged += (s, e) => actual = e;");
    m(@"");
    m(@"    var field = $Z;");
    m(@"    Subject.Change(ref field, $Z, new PropertyChangedEventArgs(""fail""));");
    m(@"");
    m(@"    Assert.That(actual, Is.Null);");
    m(@"}");
	m(@"");
	m(@"[Test]");
	m(@"public void Change_T_1_DoesNotRaisePropertyChangingEventArgs() {");
	m(@"    var actual = default(PropertyChangingEventArgs);");
	m(@"    Subject.PropertyChanging += (s, e) => actual = e;");
	m(@"");
	m(@"    var field = $Z;");
	m(@"    Subject.Change(ref field, $Z, new PropertyChangedEventArgs(""fail""));");
	m(@"");
	m(@"    Assert.That(actual, Is.Null);");
	m(@"}");
	m(@"");
	m(@"[Test]");
    m(@"public void Change_T_1_RaisesPropertyChangedEventArgsDependents() {");
    m(@"    var actual = new List<PropertyChangedEventArgs>();");
    m(@"    var expected = new[] { new PropertyChangedEventArgs(""1""), new PropertyChangedEventArgs(""2""), new PropertyChangedEventArgs(""3"") };");
    m(@"    Subject.PropertyChanged += (s, e) => actual.Add(e);");
    m(@"");
    m(@"    var field = $Z;");
    m(@"    Subject.Change(ref field, $O, expected[0], expected[1], expected[2]);");
    m(@"");
    m(@"    Assert.That(actual, Is.EqualTo(expected));");
    m(@"}");
	m(@"");
	m(@"[Test]");
	m(@"public void Change_T_1_RaisesPropertyChangingEventArgsDependents() {");
	m(@"    var actual = new List<PropertyChangingEventArgs>();");
	m(@"    var expected = new[] { new PropertyChangingEventArgs(""1""), new PropertyChangingEventArgs(""2""), new PropertyChangingEventArgs(""3"") };");
	m(@"    Subject.PropertyChanging += (s, e) => actual.Add(e);");
	m(@"");
	m(@"    var field = $Z;");
	m(@"    Subject.Change(ref field, $O, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));");
	m(@"");
	m(@"    Assert.That(actual.Select(e => e.PropertyName), Is.EqualTo(expected.Select(e => e.PropertyName)));");
	m(@"}");
	m(@"");
	m(@"[Test]");
	m(@"public void Change_T_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {");
	m(@"    var actual = new List<PropertyChangingEventArgs>();");
	m(@"    var expected = new[] { new PropertyChangingEventArgs(""1""), new PropertyChangingEventArgs(""2""), new PropertyChangingEventArgs(""3"") };");
	m(@"    Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property(""PropertyName"").EqualTo(e.PropertyName));");
	m(@"    Subject.PropertyChanging += (s, e) => actual.Add(e);");
	m(@"");
	m(@"    var field = $Z;");
	m(@"    Subject.Change(ref field, $O, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));");
	m(@"}");
	m(@"");
	m(@"[Test]");
    m(@"public void Change_T_1_DoesNotRaisePropertyChangedEventArgsDependents() {");
    m(@"    var actual = new List<PropertyChangedEventArgs>();");
    m(@"    var expected = new[] { new PropertyChangedEventArgs(""1""), new PropertyChangedEventArgs(""2""), new PropertyChangedEventArgs(""3"") };");
    m(@"    Subject.PropertyChanged += (s, e) => actual.Add(e);");
    m(@"");
    m(@"    var field = $Z;");
    m(@"    Subject.Change(ref field, $Z, expected[0], expected[1], expected[2]);");
    m(@"");
    m(@"    Assert.That(actual, Is.Empty);");
    m(@"}");
	m(@"");
	m(@"[Test]");
	m(@"public void Change_T_1_DoesNotRaisePropertyChangingEventArgsDependents() {");
	m(@"    var actual = new List<PropertyChangingEventArgs>();");
	m(@"    var expected = new[] { new PropertyChangedEventArgs(""1""), new PropertyChangedEventArgs(""2""), new PropertyChangedEventArgs(""3"") };");
	m(@"    Subject.PropertyChanging += (s, e) => actual.Add(e);");
	m(@"");
	m(@"    var field = $Z;");
	m(@"    Subject.Change(ref field, $Z, expected[0], expected[1], expected[2]);");
	m(@"");
	m(@"    Assert.That(actual, Is.Empty);");
	m(@"}");
	m(@"");
	m(@"[Test]");
    m(@"public void Change_T_1_ChangesValue() {");
    m(@"    var field = $Z;");
    m(@"    Subject.Change(ref field, $O, ""expected"");");
    m(@"    Assert.That(field, Is.EqualTo($O));");
    m(@"}");
    m(@"");
    m(@"[Test]");
    m(@"public void Change_T_1_ReturnsFalse() {");
    m(@"    var field = $Z;");
    m(@"    var actual = Subject.Change(ref field, $Z, """");");
    m(@"    Assert.That(actual, Is.False);");
    m(@"}");
    m(@"");
    m(@"[Test]");
    m(@"public void Change_T_1_ReturnsTrue() {");
    m(@"    var field = $Z;");
    m(@"    var actual = Subject.Change(ref field, $O, """");");
    m(@"    Assert.That(actual, Is.True);");
    m(@"}");
	m(@"");
	m(@"[Test]");
	m(@"public void Change_T_1_ReturnsFalseIfPrevented() {");
	m(@"    var subject = new NotifierSubject1();");
	m(@"    subject.PreviewPropertyChangeLookup[""P_T_""] = false;");	
	m(@"    var field = $Z;");
	m(@"    var actual = subject.Change(ref field, $O, ""P_T_"");");
	m(@"    Assert.That(actual, Is.False);");
	m(@"}");
	m(@"");
	m(@"[Test]");
	m(@"public void Change_T_1_ReturnsTrueIfNotPrevented() {");
	m(@"    var subject = new NotifierSubject1();");
	m(@"    subject.PreviewPropertyChangeLookup[""P_T_""] = true;");
	m(@"    var field = $Z;");
	m(@"    var actual = subject.Change(ref field, $O, ""P_T_"");");
	m(@"    Assert.That(actual, Is.True);");
	m(@"}");
	file.Append(methods.ToString()
        .Replace("_T_", $"_{type.T}_".Replace("?", "n"))
        .Replace("$Z", $"{type.Z}")
        .Replace("$O", $"{type.O}"));
}
f(@"    }");
f(@"}");
file.ToString().Dump();
File.WriteAllText(
    path: Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath), "NotifierTest.Generated.cs"),
    contents: file.ToString());