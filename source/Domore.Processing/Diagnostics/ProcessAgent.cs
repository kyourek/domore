using System;
using System.Collections.Generic;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Diagnostics;

/// <summary>
/// Configures a process to start and provides access to its output stream.
/// </summary>
public sealed class ProcessAgent {
    /// <summary>
    /// Gets or sets the command-line arguments passed to the executable.
    /// </summary>
    public string Arguments { get; set; }

    /// <summary>
    /// Gets or sets the domain used with <see cref="UserName"/> when supported.
    /// </summary>
    public string Domain { get; set; }

    /// <summary>
    /// Gets or sets environment variables to add to or override in the process.
    /// </summary>
    public IDictionary<string, string> Environment { get; set; }

    /// <summary>
    /// Gets or sets the path or name of the executable to start.
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// Gets or sets whether to load the user profile when starting the process.
    /// </summary>
    public bool? LoadUserProfile { get; set; }

    /// <summary>
    /// Gets or sets the password to use when starting the process, as a
    /// <see cref="SecureString"/>.
    /// </summary>
    public SecureString Password { get; set; }

    /// <summary>
    /// Gets or sets the plain-text password to use when starting the process,
    /// where supported.
    /// </summary>
    public string PasswordInClearText { get; set; }

    /// <summary>
    /// Gets or sets the user account under which to start the process.
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// Gets or sets the process-start verb, where supported.
    /// </summary>
    public string Verb { get; set; }

    /// <summary>
    /// Gets or sets the working directory for the process.
    /// </summary>
    public string WorkingDirectory { get; set; }

    /// <summary>
    /// Gets or sets whether output collection updates use the
    /// <see cref="SynchronizationContext"/> current when
    /// <see cref="Start(Action{IProcessProxy}, CancellationToken)"/> is called.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool SynchronizeWithCurrentContext { get; set; } = true;

    /// <summary>
    /// Gets or sets the size, in characters, of the buffer used to read process output.
    /// </summary>
    /// <remarks>
    /// The default value of zero uses the default buffer size of 4096 characters.
    /// A negative value causes <see cref="Start(Action{IProcessProxy}, CancellationToken)"/>
    /// to throw an <see cref="InvalidOperationException"/> synchronously before
    /// invoking its proxy callback or starting the process.
    /// </remarks>
    public int StreamBufferSize { get; set; }

    /// <summary>
    /// Starts the configured process without a custom handler for caught
    /// exceptions.
    /// </summary>
    /// <param name="onProxyCreated">
    /// A callback invoked synchronously with the proxy before process startup
    /// begins. Pass <see langword="null"/> to omit it.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can cancel process execution.
    /// </param>
    /// <returns>
    /// A task that completes after the process exits and its redirected output
    /// has been read. The task is canceled when cancellation is requested and
    /// faults if process startup or output reading fails, or if an exception
    /// occurs while closing standard input or retrieving exit metadata.
    /// </returns>
    /// <remarks>
    /// If <paramref name="onProxyCreated"/> throws, the exception is propagated
    /// synchronously and the process is not started. This overload propagates
    /// exceptions encountered while closing standard input or retrieving the
    /// process exit code and exit time.
    /// </remarks>
    public Task Start(Action<IProcessProxy> onProxyCreated,
                      CancellationToken cancellationToken) {
        return Start(onProxyCreated, onErrorCaught: null, cancellationToken);
    }

    /// <summary>
    /// Starts the configured process.
    /// </summary>
    /// <param name="onProxyCreated">
    /// A callback invoked synchronously with the proxy before process startup
    /// begins. Pass <see langword="null"/> to omit it.
    /// </param>
    /// <param name="onErrorCaught">
    /// A callback for exceptions encountered while closing standard input or
    /// retrieving the process exit code and exit time. It can be invoked once
    /// for each such exception. Pass <see langword="null"/> to let those
    /// exceptions fault the returned task.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can cancel process execution.
    /// </param>
    /// <returns>
    /// A task that completes after the process exits and its redirected output
    /// has been read. The task is canceled when cancellation is requested and
    /// faults if process startup or output reading fails, if a handled-operation
    /// exception occurs and <paramref name="onErrorCaught"/> is
    /// <see langword="null"/>, or if <paramref name="onErrorCaught"/> throws.
    /// </returns>
    /// <remarks>
    /// The callback runs before the proxy's output stream is initialized. If
    /// the callback throws, the exception is propagated synchronously and the
    /// process is not started. If <paramref name="onErrorCaught"/> is
    /// supplied, an exception it receives is considered handled if the
    /// callback returns normally. Exceptions from process startup and output
    /// reading are not passed to this callback. A negative
    /// <see cref="StreamBufferSize"/> causes a synchronous
    /// <see cref="InvalidOperationException"/> before this callback is invoked.
    /// </remarks>
    public Task Start(Action<IProcessProxy> onProxyCreated,
                      Action<Exception> onErrorCaught,
                      CancellationToken cancellationToken) {
        var streamBufferSize = StreamBufferSize;
        if (streamBufferSize < 0) {
            throw new InvalidOperationException("The stream buffer size cannot be less than zero (0).");
        }
        var synchronizationContext = SynchronizeWithCurrentContext
            ? SynchronizationContext.Current
            : null;
        var pp = new ProcessProxy(
            arguments: Arguments,
            domain: Domain,
            environment: Environment,
            fileName: FileName,
            loadUserProfile: LoadUserProfile,
            password: Password,
            passwordInClearText: PasswordInClearText,
            userName: UserName,
            verb: Verb,
            workingDirectory: WorkingDirectory);
        if (onProxyCreated is not null) {
            onProxyCreated(pp);
        }
        return pp.Start(
            errorHandler: onErrorCaught,
            synchronizationContext: synchronizationContext,
            streamBufferSize: streamBufferSize,
            cancellationToken: cancellationToken);
    }
}
