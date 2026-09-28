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
    /// <see cref="Start(CancellationToken)"/> is called.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool SynchronizeWithCurrentContext { get; set; } = true;

    /// <summary>
    /// Gets or sets the size, in characters, of the buffer used to read process output.
    /// </summary>
    /// <remarks>
    /// The default value of zero uses the default buffer size of 4096 characters.
    /// A negative value causes <see cref="Start(CancellationToken)"/>
    /// to throw an <see cref="InvalidOperationException"/> synchronously before
    /// invoking <see cref="OnProxyCreated"/> or starting the process.
    /// </remarks>
    public int StreamBufferSize { get; set; }

    /// <summary>
    /// Gets or sets a callback invoked synchronously with the process proxy
    /// before the process starts.
    /// </summary>
    /// <remarks>
    /// The proxy's output stream is not initialized when this callback runs.
    /// Set this property to <see langword="null"/> to omit the callback.
    /// </remarks>
    public Action<IProcessProxy> OnProxyCreated { get; set; }

    /// <summary>
    /// Gets or sets a callback for exceptions encountered while closing
    /// standard input or retrieving the process exit code and exit time.
    /// </summary>
    /// <remarks>
    /// This callback can be invoked once for each such exception. An exception
    /// is handled if this callback returns normally. If this property is
    /// <see langword="null"/>, the exception faults the task returned by
    /// <see cref="Start(CancellationToken)"/>. Exceptions from process startup
    /// and output reading are not passed to this callback.
    /// </remarks>
    public Action<Exception> OnErrorCaught { get; set; }

    /// <summary>
    /// Gets or sets a fixer that can update process-start information after a
    /// process-start attempt fails.
    /// </summary>
    /// <remarks>
    /// When the fixer returns <see langword="true"/>, process startup is
    /// retried with the possibly updated start information. If it returns
    /// <see langword="false"/>, the start exception is propagated. Exceptions
    /// and cancellation from the fixer are propagated by
    /// <see cref="Start(CancellationToken)"/>.
    /// </remarks>
    public IProcessStartInfoFixer StartInfoFixer { get; set; }

    /// <summary>
    /// Starts the configured process.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token that can cancel process execution.
    /// </param>
    /// <returns>
    /// A task that completes after the process exits and its redirected output
    /// has been read. The task is canceled when cancellation is requested and
    /// faults if process startup or output reading fails, if
    /// an exception encountered while closing standard input or retrieving
    /// exit metadata occurs and
    /// <see cref="OnErrorCaught"/> is <see langword="null"/>, or if
    /// <see cref="OnErrorCaught"/> throws.
    /// Failures and cancellation from <see cref="StartInfoFixer"/> are propagated
    /// through the returned task.
    /// </returns>
    /// <remarks>
    /// <see cref="OnProxyCreated"/> runs before the proxy's output stream is
    /// initialized. If it throws, the exception is propagated synchronously
    /// and the process is not started. If <see cref="OnErrorCaught"/> is
    /// supplied, an exception it receives is considered handled if the
    /// callback returns normally. A negative <see cref="StreamBufferSize"/>
    /// causes a synchronous <see cref="InvalidOperationException"/> before
    /// <see cref="OnProxyCreated"/> is invoked.
    /// </remarks>
    public Task<IProcessProxy> Start(CancellationToken cancellationToken) {
        if (cancellationToken.IsCancellationRequested) {
#if NET40 || NET45
            static async Task<IProcessProxy> cancel(CancellationToken cancellationToken) {
                await Task.Factory.StartNew(() => { }, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                return default;
            }
            return cancel(cancellationToken);
#else
            return Task.FromCanceled<IProcessProxy>(cancellationToken);
#endif
        }
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
            workingDirectory: WorkingDirectory) {
            ErrorHandler = OnErrorCaught,
            StartInfoFixer = StartInfoFixer,
            StreamBufferSize = streamBufferSize,
            SynchronizationContext = synchronizationContext,
        };
        var onProxyCreated = OnProxyCreated;
        if (onProxyCreated is not null) {
            onProxyCreated(pp);
        }
        return pp.Start(cancellationToken: cancellationToken);
    }
}
