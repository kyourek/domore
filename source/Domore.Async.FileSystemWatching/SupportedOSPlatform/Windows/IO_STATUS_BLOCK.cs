using System.Runtime.InteropServices;

namespace Domore.SupportedOSPlatform.Windows;

[StructLayout(LayoutKind.Sequential)]
internal struct IO_STATUS_BLOCK {
    [MarshalAs(UnmanagedType.U4)]
    public NTSTATUS Status;
    public ulong Information;
}
