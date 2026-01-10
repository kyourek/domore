using System.Runtime.InteropServices;

[assembly: ComVisible(false)]

#if NETFRAMEWORK
namespace System.Runtime.CompilerServices {
    [ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)]
    internal static class IsExternalInit {
    }
}
#endif