using NUnit.Framework;
using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Domore.Windows;

/// <summary>
/// Runs asynchronous tests on a dedicated STA thread with a running WPF dispatcher, so that
/// continuations resume on the UI thread as they do in an application.
/// </summary>
internal static class DispatcherTest {
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    public static void Run(Func<Task> test) {
        if (test is null) throw new ArgumentNullException(nameof(test));
        var error = default(ExceptionDispatchInfo);
        var thread = new Thread(() => {
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
            dispatcher.UnhandledException += (s, e) => {
                error ??= ExceptionDispatchInfo.Capture(e.Exception);
                e.Handled = true;
            };
            dispatcher.InvokeAsync(async () => {
                try {
                    await test();
                }
                catch (Exception ex) {
                    error ??= ExceptionDispatchInfo.Capture(ex);
                }
                finally {
                    dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                }
            });
            Dispatcher.Run();
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        if (!thread.Join(Timeout + TimeSpan.FromSeconds(10))) {
            Assert.Fail("The dispatcher test did not finish.");
        }
        error?.Throw();
    }

    public static async Task Until(Func<bool> condition, string message) {
        var deadline = DateTime.UtcNow + Timeout;
        while (!condition()) {
            if (DateTime.UtcNow > deadline) {
                Assert.Fail($"Timed out waiting for: {message}");
            }
            await Task.Delay(10);
        }
    }
}
