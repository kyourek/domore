using System;
using System.Threading;
#if NETFRAMEWORK
using System.Runtime.Remoting.Messaging;
#endif

namespace Domore.Logs;

/// <summary>
/// Tracks public callbacks so logging feedback can be suppressed while callback
/// execution and its ordinary execution-context descendants are active.
/// </summary>
internal static class LogCallbackGuard {
#if NETFRAMEWORK
    private const string StateName = "Domore.Logs.LogCallbackGuard";
#else
    private static readonly AsyncLocal<State> Local = new();
#endif

    private sealed class State
#if NETFRAMEWORK
        : ILogicalThreadAffinative
#endif
    {
        public int CallbackDepth { get; }
        public int DiagnosticDepth { get; }
        public LogManager Manager { get; }

        public State(int callbackDepth, int diagnosticDepth, LogManager manager) {
            CallbackDepth = callbackDepth;
            DiagnosticDepth = diagnosticDepth;
            Manager = manager;
        }
    }

    private sealed class RestoreScope : IDisposable {
        private readonly State Previous;
        private int Disposed;

        public RestoreScope(State previous) {
            Previous = previous;
        }

        public void Dispose() {
            if (Interlocked.Exchange(ref Disposed, 1) != 0) {
                return;
            }
            SetState(Previous);
        }
    }

    private static State GetState() {
#if NETFRAMEWORK
        return CallContext.LogicalGetData(StateName) as State;
#else
        return Local.Value;
#endif
    }

    private static void SetState(State state) {
#if NETFRAMEWORK
        if (state is null) {
            CallContext.FreeNamedDataSlot(StateName);
        }
        else {
            CallContext.LogicalSetData(StateName, state);
        }
#else
        Local.Value = state;
#endif
    }

    public static bool IsActive =>
        GetState()?.CallbackDepth > 0;

    public static bool IsDiagnosing =>
        GetState()?.DiagnosticDepth > 0;

    public static LogManager CurrentManager =>
        GetState()?.Manager;

    /// <summary>
    /// Marks the current execution context as being inside a user callback.
    /// </summary>
    public static IDisposable Enter() {
        var previous = GetState();
        SetState(new State((previous?.CallbackDepth ?? 0) + 1, previous?.DiagnosticDepth ?? 0, previous?.Manager));
        return new RestoreScope(previous);
    }

    /// <summary>
    /// Marks diagnostic output so a failing diagnostic writer cannot recurse.
    /// </summary>
    public static IDisposable EnterDiagnostic() {
        var previous = GetState();
        SetState(new State((previous?.CallbackDepth ?? 0) + 1, (previous?.DiagnosticDepth ?? 0) + 1, previous?.Manager));
        return new RestoreScope(previous);
    }

    /// <summary>
    /// Associates an active manager with the current execution context.
    /// </summary>
    public static IDisposable EnterManager(LogManager manager) {
        var previous = GetState();
        SetState(new State(previous?.CallbackDepth ?? 0, previous?.DiagnosticDepth ?? 0, manager ?? previous?.Manager));
        return new RestoreScope(previous);
    }
}
