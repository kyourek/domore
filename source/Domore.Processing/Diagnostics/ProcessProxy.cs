using Domore.Diagnostics.Shims;
using Domore.Notification;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Diagnostics;

internal sealed class ProcessProxy : Notifier, IProcessProxy {
    private Process Process;

    private readonly
#if NET9_0_OR_GREATER
        Lock
#else
        object
#endif
        StartLocker = new();

    private static Task WaitForExitAsync(Process process, CancellationToken cancellationToken) {
        if (process is null) {
            throw new ArgumentNullException(nameof(process));
        }
#if NET
        return process.WaitForExitAsync(cancellationToken);
#else
        return Process_WaitForExitAsync.Shim(process, cancellationToken);
#endif
    }

    private static async Task Kill(Process process) {
        if (process is null) {
            throw new ArgumentNullException(nameof(process));
        }
        if (process.HasExited == false) {
            try {
                process.Kill();
            }
            catch (InvalidOperationException) when (process.HasExited) {
            }
            catch (Win32Exception) when (process.HasExited) {
            }
        }
        await WaitForExitAsync(process, CancellationToken.None);
    }

    internal int StreamBufferSize { get; set; }
    internal IProcessStartInfoFixer StartInfoFixer { get; set; }
    internal SynchronizationContext SynchronizationContext { get; set; }
    internal Action<Exception> ErrorHandler { get; set; }

    public ProcessStream Stream {
        get;
        private set => Change(ref field, value, nameof(Stream));
    }

    public bool Running {
        get;
        private set => Change(ref field, value, nameof(Running));
    }

    public int? ProcessID {
        get;
        private set => Change(ref field, value, nameof(ProcessID));
    }

    public int? ExitCode {
        get;
        private set => Change(ref field, value, nameof(ExitCode));
    }

    public DateTime? ExitTime {
        get;
        private set => Change(ref field, value, nameof(ExitTime));
    }

    public DateTime? StartTime {
        get;
        private set => Change(ref field, value, nameof(StartTime));
    }

    public bool Started {
        get;
        private set => Change(ref field, value, nameof(Started));
    }

    public bool Starting {
        get;
        private set => Change(ref field, value, nameof(Starting));
    }

    public string FileName { get; }
    public string Arguments { get; }
    public string WorkingDirectory { get; }
    public string Domain { get; }
    public bool? LoadUserProfile { get; }
    public string UserName { get; }
    public SecureString Password { get; }
    public string PasswordInClearText { get; }
    public string Verb { get; }
    public IDictionary<string, string> Environment { get; }

    public ProcessProxy(string fileName,
                        string arguments = null,
                        string workingDirectory = null,
                        string domain = null,
                        bool? loadUserProfile = null,
                        string userName = null,
                        SecureString password = null,
                        string passwordInClearText = null,
                        string verb = null,
                        IDictionary<string, string> environment = null) {
        FileName = fileName;
        Arguments = arguments;
        WorkingDirectory = workingDirectory;
        Domain = domain;
        LoadUserProfile = loadUserProfile;
        UserName = userName;
        Password = password;
        PasswordInClearText = passwordInClearText;
        Verb = verb;
        Environment = environment is null
            ? null
            : new ReadOnlyDictionary<string, string>(environment);
    }

    public async Task Kill() {
        var process = Process;
        if (process is not null) {
            try {
                await Kill(process);
            }
            catch (InvalidOperationException) when (!ReferenceEquals(Process, process)) {
            }
        }
    }

    public async Task<IProcessProxy> Start(CancellationToken cancellationToken = default) {
        lock (StartLocker) {
            if (Starting) {
                throw new InvalidOperationException(message: $"Already starting!");
            }
            if (Started) {
                throw new InvalidOperationException(message: $"Already started!");
            }
            Starting = true;
        }
        var errorHandler = ErrorHandler;
        var streamBufferSize = StreamBufferSize;
        var synchronizationContext = SynchronizationContext;
        try {
            if (cancellationToken.IsCancellationRequested) {
                cancellationToken.ThrowIfCancellationRequested();
            }
            var psi = new ProcessStartInfo {
                CreateNoWindow = true,
                ErrorDialog = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            if (Environment is not null) {
                foreach (var key in Environment.Keys) {
                    psi.EnvironmentVariables[key] = Environment[key];
                }
            }
#if NETFRAMEWORK
            psi.Domain = Domain ?? psi.Domain;
            psi.LoadUserProfile = LoadUserProfile ?? psi.LoadUserProfile;
            psi.Password = Password ?? psi.Password;
#if NET461_OR_GREATER
            psi.PasswordInClearText = PasswordInClearText ?? psi.PasswordInClearText;
#endif
#endif
#if NET
            if (OperatingSystem.IsWindows()) {
                psi.Domain = Domain ?? psi.Domain;
                psi.LoadUserProfile = LoadUserProfile ?? psi.LoadUserProfile;
                psi.Password = Password ?? psi.Password;
                psi.PasswordInClearText = PasswordInClearText ?? psi.PasswordInClearText;
            }
#endif
            psi.Arguments = Arguments ?? psi.Arguments;
            psi.FileName = FileName ?? psi.FileName;

            psi.UserName = UserName ?? psi.UserName;
            psi.Verb = Verb ?? psi.Verb;
            psi.WorkingDirectory = WorkingDirectory ?? psi.WorkingDirectory;

            using (var process = new Process { StartInfo = psi }) {
                var processStarted = false;
                for (; ; ) {
                    if (cancellationToken.IsCancellationRequested) {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    try {
                        processStarted = process.Start();
                        break;
                    }
                    catch (Exception ex) {
                        var fixer = StartInfoFixer;
                        if (fixer is null) {
                            throw;
                        }
                        if (cancellationToken.IsCancellationRequested) {
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                        var fixing = fixer.Fix(process.StartInfo, ex, cancellationToken);
                        var @fixed = fixing is null
                            ? false
                            : await fixing;
                        if (cancellationToken.IsCancellationRequested) {
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                        if (@fixed != true) {
                            throw;
                        }
                    }
                }
                if (processStarted != true) {
                    throw new InvalidOperationException("The process did not start.");
                }
                try {
                    try {
                        process.StandardInput.Close();
                    }
                    catch (Exception ex) {
                        if (errorHandler is not null) {
                            errorHandler(ex);
                        }
                        else {
                            throw;
                        }
                    }
                    NotifyPropertyChanging(nameof(ProcessID),
                                           nameof(Started),
                                           nameof(Starting),
                                           nameof(StartTime),
                                           nameof(Running));
                    NotifyState = false;
                    try {
                        lock (StartLocker) {
                            Process = process;
                            ProcessID = Process.Id;
                            Started = true;
                            Starting = false;
                            StartTime = DateTime.Now;
                            Running = true;
                        }
                    }
                    finally {
                        NotifyState = true;
                    }
                    NotifyPropertyChanged(nameof(ProcessID),
                                          nameof(Started),
                                          nameof(Starting),
                                          nameof(StartTime),
                                          nameof(Running));
                    using (var processStream = new ProcessStream(process, synchronizationContext, streamBufferSize)) {
                        Stream = processStream;
                        try {
                            await WaitForExitAsync(process, cancellationToken);
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                            await Kill(process);
                            await processStream.Cancel();
                            throw;
                        }
                        await processStream.Complete();
                    }
                    NotifyPropertyChanging(nameof(ExitCode), nameof(ExitTime));
                    NotifyState = false;
                    try {
                        try {
                            ExitCode = process.ExitCode;
                        }
                        catch (Exception ex) {
                            if (errorHandler is not null) {
                                errorHandler(ex);
                            }
                            else {
                                throw;
                            }
                        }
                        try {
                            ExitTime = process.ExitTime;
                        }
                        catch (Exception ex) {
                            if (errorHandler is not null) {
                                errorHandler(ex);
                            }
                            else {
                                throw;
                            }
                        }
                    }
                    finally {
                        NotifyState = true;
                    }
                    NotifyPropertyChanged(nameof(ExitCode), nameof(ExitTime));
                    return this;
                }
                finally {
                    await Kill(process);
                }
            }
        }
        finally {
            NotifyPropertyChanging(nameof(Starting), nameof(Running));
            NotifyState = false;
            try {
                lock (StartLocker) {
                    Starting = false;
                    Running = false;
                    Process = null;
                }
            }
            finally {
                NotifyState = true;
            }
            NotifyPropertyChanged(nameof(Starting), nameof(Running));
        }
    }

    IProcessStream IProcessProxy.Stream => Stream;
}
