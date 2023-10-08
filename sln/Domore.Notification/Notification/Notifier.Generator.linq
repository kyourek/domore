<Query Kind="Statements">
  <NuGetReference>Domore.Conf</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Domore.Conf</Namespace>
  <Namespace>Domore.Conf.Extensions</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

var types = new[] {
    "bool",
    "bool?",
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
    "ushort?" }
        .Select(t => new { T = t, G = "", W = " ", E = "field == value" })
        .Concat(new[] {
            new { T = "string", G = "", W = " ", E = "string.Equals(field, value)" },
            new { T = "T", G = "<T>", W = " ", E = "EqualityComparer<T>.Default.Equals(field, value)" }
        });
var file = new StringBuilder();
var f = new Action<string>(l => file.AppendLine(l));
f(@"using System.Collections.Generic;");
f(@"using System.ComponentModel;");
f(@"#if !NET40");
f(@"using System.Runtime.CompilerServices;");
f(@"#endif");
f(@"");
f(@"namespace Domore.Notification {");
f(@"    public partial class Notifier {");
foreach (var type in types) {
    var methods = new StringBuilder();
    var m = new Action<string>(s => methods.AppendLine("        " + s));
    m(@"protected internal bool Change$G(");
    m(@"    ref T field,");
    m(@"    T value,");
    m(@"    #if NET40");
    m(@"        string propertyName");
    m(@"    #else");
    m(@"        [CallerMemberName] string propertyName = null");
    m(@"    #endif");
    m(@"    )$W{");
    m(@"    if ($E) return false;");
    m(@"    field = value;");
    m(@"    NotifyPropertyChanged(propertyName);");
    m(@"    return true;");
    m(@"}");
    m(@"");
    m(@"protected internal bool Change$G(");
    m(@"    ref T field,");
    m(@"    T value,");
    m(@"    #if NET40");
    m(@"        string propertyName,");
    m(@"    #else");
    m(@"        [CallerMemberName] string propertyName = null,");
    m(@"    #endif");
    m(@"    params string[] dependentPropertyNames");
    m(@"    )$W{");
    m(@"    if ($E) return false;");
    m(@"    field = value;");
    m(@"    NotifyPropertyChanged(propertyName, dependentPropertyNames);");
    m(@"    return true;");
    m(@"}");
    m(@"");
    m(@"protected internal bool Change$G(ref T field, T value, PropertyChangedEventArgs e)$W{");
    m(@"    if ($E) return false;");
    m(@"    field = value;");
    m(@"    NotifyPropertyChanged(e);");
    m(@"    return true;");
    m(@"}");
    m(@"");
    m(@"protected internal bool Change$G(ref T field, T value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents)$W{");
    m(@"    if ($E) return false;");
    m(@"    field = value;");
    m(@"    NotifyPropertyChanged(e, dependents);");
    m(@"    return true;");
    m(@"}");
    file.Append(methods.ToString()
        .Replace(" T ", $" {type.T} ")
        .Replace("$G", type.G)
        .Replace("$W", type.W)
        .Replace("$E", type.E));
}
f(@"    }");
f(@"}");
file.ToString().Dump();
File.WriteAllText(
    path: Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath), "Notifier.Generated.cs"),
    contents: file.ToString());