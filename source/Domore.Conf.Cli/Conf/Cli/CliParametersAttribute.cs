using System;

namespace Domore.Conf.Cli; 
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class CliParametersAttribute : Attribute {
}
