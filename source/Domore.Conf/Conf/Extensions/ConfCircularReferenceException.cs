using System;

namespace Domore.Conf.Extensions; 
internal sealed class ConfCircularReferenceException : Exception {
    public sealed override string Message =>
        $"A circular reference was encountered while generating conf for type '{Obj?.GetType()}'.";

    public object Obj { get; }

    public ConfCircularReferenceException(object obj) {
        Obj = obj;
    }
}
