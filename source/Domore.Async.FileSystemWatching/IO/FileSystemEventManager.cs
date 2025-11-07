using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.IO;

/// <summary>
/// Manages file-system event subscriptions.
/// </summary>
public sealed class FileSystemEventManager {
    private readonly Agent CaseSensitiveAgent = new();
    private readonly Agent CaseInsensitiveAgent = new();

    private async Task<PostInfo> Get(string path, FileSystemEventOptions options, CancellationToken token) {
        var key = new FileSystemEventsKey(path, options);
        var caseSensitive = await FileSystemPath.IsCaseSensitive(path, token).ConfigureAwait(false);
        var agent = caseSensitive
            ? CaseSensitiveAgent
            : CaseInsensitiveAgent;
        return new(agent, agent.Get(key), key);
    }

    internal TimeSpan ClearDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Adds an event subscription to the specified path with the specified options.
    /// </summary>
    /// <param name="subscription">The event subscription to be added.</param>
    /// <param name="path">The path of the watched directory.</param>
    /// <param name="options">The options for watching the path.</param>
    /// <param name="token">The cancellation token for the task.</param>
    /// <returns>A task that completes when the event subscription has been added.</returns>
    public async Task Add(FileSystemEventSubscription subscription, string path, FileSystemEventOptions options, CancellationToken token) {
        var item = await Get(path, options, token).ConfigureAwait(false);
        lock (item.Agent) {
            item.Post.Add(subscription);
        }
    }

    /// <summary>
    /// Removes an event subscription from the specified path with the specified options.
    /// </summary>
    /// <param name="subscription">The event subscription to be removed.</param>
    /// <param name="path">The path of the watched directory.</param>
    /// <param name="options">The options for watching the path.</param>
    /// <param name="token">The cancellation token for the task.</param>
    /// <returns>A task that completes when the event subscription has been removed.</returns>
    public async Task Remove(FileSystemEventSubscription subscription, string path, FileSystemEventOptions options, CancellationToken token) {
        var item = await Get(path, options, token).ConfigureAwait(false);
        lock (item.Agent) {
            var count = item.Post.Remove(subscription);
            if (count == 0) {
                _ = Task.Run(cancellationToken: default, function: async () => {
                    var delay = ClearDelay;
                    if (delay > TimeSpan.Zero) {
                        await Task.Delay(delay).ConfigureAwait(false);
                    }
                    lock (item.Agent) {
                        if (item.Post.SubscriptionCount == 0) {
                            item.Post.Dispose();
                            item.Agent.Clear(item.Key);
                        }
                    }
                });
            }
        }
    }

    private readonly struct PostInfo(Agent agent, FileSystemEventPost post, FileSystemEventsKey key) {
        public readonly Agent Agent { get; } = agent;
        public readonly FileSystemEventsKey Key { get; } = key;
        public readonly FileSystemEventPost Post { get; } = post;
    }

    private sealed class Agent {
        private readonly Dictionary<FileSystemEventsKey, FileSystemEventPost> Lookup = [];

        private FileSystemEventPost Create(FileSystemEventsKey key) {
            if (key is null) {
                throw new ArgumentNullException(nameof(key));
            }
            return new(key.Path, key.Options);
        }

        public FileSystemEventPost Get(FileSystemEventsKey key) {
            lock (Lookup) {
                if (Lookup.TryGetValue(key, out var post) == false) {
                    Lookup[key] = post = Create(key);
                }
                return post;
            }
        }

        public bool Clear(FileSystemEventsKey key) {
            lock (Lookup) {
                return Lookup.Remove(key);
            }
        }
    }
}
