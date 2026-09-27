using Domore.Notification;
using System;
using System.ComponentModel;
using System.Text;
using System.Threading;

namespace Domore.Diagnostics;

internal sealed class ProcessStreamOutput : Notifier, IProcessStreamOutput {
    private static readonly PropertyChangedEventArgs LineEvent = new(nameof(Line));

    private readonly StringBuilder LineBuilder = new(256);
    private readonly
#if NET9_0_OR_GREATER
        Lock
#else
        object
#endif
        LineLocker = new();

    private string LineSnapshot = string.Empty;
    private bool LineSnapshotIsDirty;

    public string Line {
        get {
            lock (LineLocker) {
                if (LineSnapshotIsDirty) {
                    LineSnapshot = LineBuilder.ToString();
                    LineSnapshotIsDirty = false;
                }
                return LineSnapshot;
            }
        }
    }

    public ProcessOutputKind Kind { get; }

    public ProcessStreamOutput(ProcessOutputKind kind) {
        Kind = kind;
    }

    public void Append(char[] buffer, int index, int count) {
        if (buffer is null) {
            throw new ArgumentNullException(nameof(buffer));
        }
        if (count == 0) {
            return;
        }
        lock (LineLocker) {
            LineBuilder.Append(buffer, index, count);
            LineSnapshotIsDirty = true;
        }
        NotifyPropertyChanged(LineEvent);
    }

    public void Backspace() {
        lock (LineLocker) {
            if (LineBuilder.Length == 0) {
                return;
            }
            LineBuilder.Length--;
            LineSnapshotIsDirty = true;
        }
        NotifyPropertyChanged(LineEvent);
    }
}
