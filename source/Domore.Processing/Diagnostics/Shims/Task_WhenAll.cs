#if NET40
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Diagnostics.Shims;

internal static class Task_WhenAll {
    public static Task Shim(params Task[] tasks) {
        if (tasks is null) {
            throw new ArgumentNullException(nameof(tasks));
        }
        var taskCount = tasks.Length;
        for (var i = 0; i < taskCount; i++) {
            if (tasks[i] is null) {
                throw new ArgumentException("The tasks array contains a null value.", nameof(tasks));
            }
        }
        var completion = new TaskCompletionSource<bool>();
        if (taskCount == 0) {
            completion.SetResult(true);
            return completion.Task;
        }
        Task.Factory.ContinueWhenAll(
            tasks,
            completedTasks => {
                var exceptions = new List<Exception>();
                var canceled = false;
                foreach (var task in completedTasks) {
                    if (task.IsFaulted) {
                        exceptions.AddRange(task.Exception.InnerExceptions);
                    }
                    else if (task.IsCanceled) {
                        canceled = true;
                    }
                }
                if (exceptions.Count > 0) {
                    completion.TrySetException(exceptions);
                }
                else if (canceled) {
                    completion.TrySetCanceled();
                }
                else {
                    completion.TrySetResult(true);
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);
        return completion.Task;
    }
}
#endif
