using Domore.SupportedOSPlatform.Windows;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.IO;

internal static class FileSystemPath {
#if !NETFRAMEWORK
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
    private static bool IsCaseSensitiveWin32(string path) {
        const uint FILE_CS_FLAG_CASE_SENSITIVE_DIR = 0x00000001;
        var hFile = kernel32.CreateFileW(
            filename: path,
            access: 0,
            share: FileShare.ReadWrite,
            securityAttributes: IntPtr.Zero,
            creationDisposition: FileMode.Open,
            flagsAndAttributes: (FileAttributes)FILE_FLAG.BACKUP_SEMANTICS,
            templateFile: IntPtr.Zero);
        using (hFile) {
            if (hFile.IsInvalid) {
                throw new Win32Exception();
            }
            var iosb = new IO_STATUS_BLOCK();
            var fcsi = new FILE_CASE_SENSITIVE_INFORMATION();
            var size = (uint)Marshal.SizeOf<FILE_CASE_SENSITIVE_INFORMATION>();
            var status = ntdll.NtQueryInformationFile(
                hFile,
                ref iosb,
                ref fcsi,
                size,
                FILE_INFORMATION_CLASS.FileCaseSensitiveInformation);
            switch (status) {
                case NTSTATUS.STATUS_SUCCESS:
                    return FILE_CS_FLAG_CASE_SENSITIVE_DIR == (fcsi.Flags & FILE_CS_FLAG_CASE_SENSITIVE_DIR);
                case NTSTATUS.STATUS_NOT_IMPLEMENTED:
                case NTSTATUS.STATUS_NOT_SUPPORTED:
                case NTSTATUS.STATUS_INVALID_INFO_CLASS:
                case NTSTATUS.STATUS_INVALID_PARAMETER:
                    return false;
                default:
                    // TODO: An error may have occurred here. Possibly throw an exception.
                    return false;
            }
        }
    }

    public static Task<bool> IsCaseSensitive(string path, CancellationToken token) {
        return Task.Run(cancellationToken: token, function: () => {
            var isWindows =
#if NETFRAMEWORK
                true
#else
                OperatingSystem.IsWindows()
#endif
            ;
            var isCaseSensitive = isWindows
                ? IsCaseSensitiveWin32(path)
                : true /* TODO: Not always the case. */;
            return isCaseSensitive;
        });
    }
}
