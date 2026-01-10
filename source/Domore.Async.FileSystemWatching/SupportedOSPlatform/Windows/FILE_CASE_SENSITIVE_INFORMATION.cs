using System.Runtime.InteropServices;

namespace Domore.SupportedOSPlatform.Windows;

[StructLayout(LayoutKind.Sequential)]
internal struct FILE_CASE_SENSITIVE_INFORMATION {
    [MarshalAs(UnmanagedType.U4)]
    public uint Flags;
}
