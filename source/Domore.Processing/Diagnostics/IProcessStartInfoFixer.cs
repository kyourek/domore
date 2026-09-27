using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Diagnostics;

/// <summary>
/// Adjusts process-start information after an attempt to start a process fails.
/// </summary>
/// <remarks>
/// A fixer can update the supplied <see cref="ProcessStartInfo"/> and request a
/// retry. It can be called again if a subsequent attempt also fails.
/// </remarks>
public interface IProcessStartInfoFixer {
    /// <summary>
    /// Attempts to fix the process-start information after a start failure.
    /// </summary>
    /// <param name="startInfo">
    /// The process-start information used by the failed attempt. Changes to
    /// this object are used by a subsequent attempt.
    /// </param>
    /// <param name="error">
    /// The exception thrown by the failed process-start attempt.
    /// </param>
    /// <param name="token">
    /// The cancellation token supplied for process execution.
    /// </param>
    /// <returns>
    /// A task whose result is <see langword="true"/> to retry process startup,
    /// or <see langword="false"/> to propagate the start failure.
    /// </returns>
    Task<bool> Fix(ProcessStartInfo startInfo, Exception error, CancellationToken token);
}
