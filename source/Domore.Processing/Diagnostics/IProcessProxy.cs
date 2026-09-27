using System;
using System.Collections.Generic;
using System.Security;

namespace Domore.Diagnostics;

/// <summary>
/// Provides access to a process's redirected output stream.
/// </summary>
public interface IProcessProxy {
    /// <summary>
    /// Gets whether the process is currently running.
    /// </summary>
    bool Running { get; }

    /// <summary>
    /// Gets the process identifier, or <see langword="null"/> if the process
    /// has not started successfully.
    /// </summary>
    int? ProcessID { get; }

    /// <summary>
    /// Gets the process exit code, or <see langword="null"/> if it is not
    /// available.
    /// </summary>
    int? ExitCode { get; }

    /// <summary>
    /// Gets the process exit time, or <see langword="null"/> if it is not
    /// available.
    /// </summary>
    DateTime? ExitTime { get; }

    /// <summary>
    /// Gets the time at which process startup succeeded, or
    /// <see langword="null"/> if it has not started successfully.
    /// </summary>
    DateTime? StartTime { get; }

    /// <summary>
    /// Gets whether this proxy has successfully started its process.
    /// </summary>
    bool Started { get; }

    /// <summary>
    /// Gets whether this proxy is currently starting its process.
    /// </summary>
    bool Starting { get; }

    /// <summary>
    /// Gets the configured executable path or name.
    /// </summary>
    string FileName { get; }

    /// <summary>
    /// Gets the command-line arguments configured for the process.
    /// </summary>
    string Arguments { get; }

    /// <summary>
    /// Gets the configured process working directory.
    /// </summary>
    string WorkingDirectory { get; }

    /// <summary>
    /// Gets the user domain configured for the process, where supported.
    /// </summary>
    string Domain { get; }

    /// <summary>
    /// Gets whether the user profile is configured to load, or
    /// <see langword="null"/> when the platform default is used.
    /// </summary>
    bool? LoadUserProfile { get; }

    /// <summary>
    /// Gets the user account configured for the process.
    /// </summary>
    string UserName { get; }

    /// <summary>
    /// Gets the secure password configured for the process.
    /// </summary>
    SecureString Password { get; }

    /// <summary>
    /// Gets the plain-text password configured for the process, where
    /// supported.
    /// </summary>
    string PasswordInClearText { get; }

    /// <summary>
    /// Gets the process-start verb, where supported.
    /// </summary>
    string Verb { get; }

    /// <summary>
    /// Gets the environment variables configured to add to or override in the
    /// process environment, or <see langword="null"/> if none were supplied.
    /// </summary>
    IDictionary<string, string> Environment { get; }

    /// <summary>
    /// Gets the process output stream, or <see langword="null"/> before it is
    /// initialized during process startup.
    /// </summary>
    IProcessStream Stream { get; }
}
