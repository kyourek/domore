using System;

namespace Domore.Logs;

/// <summary>
/// A snapshot of one logging queue's pending and rejected message totals.
/// </summary>
public sealed class LogQueueStatistics {
    public LogQueueStatistics(int itemLimit,
                              long messageByteLimit,
                              long pendingItemCount,
                              long pendingMessageBytes,
                              long droppedItemCount,
                              long droppedMessageBytes) {
        if (itemLimit <= 0) {
            throw new ArgumentOutOfRangeException(nameof(itemLimit));
        }
        if (messageByteLimit <= 0) {
            throw new ArgumentOutOfRangeException(nameof(messageByteLimit));
        }
        if (pendingItemCount < 0) {
            throw new ArgumentOutOfRangeException(nameof(pendingItemCount));
        }
        if (pendingMessageBytes < 0) {
            throw new ArgumentOutOfRangeException(nameof(pendingMessageBytes));
        }
        if (droppedItemCount < 0) {
            throw new ArgumentOutOfRangeException(nameof(droppedItemCount));
        }
        if (droppedMessageBytes < 0) {
            throw new ArgumentOutOfRangeException(nameof(droppedMessageBytes));
        }
        ItemLimit = itemLimit;
        MessageByteLimit = messageByteLimit;
        PendingItemCount = pendingItemCount;
        PendingMessageBytes = pendingMessageBytes;
        DroppedItemCount = droppedItemCount;
        DroppedMessageBytes = droppedMessageBytes;
    }

    /// <summary>Gets the configured maximum number of pending items.</summary>
    public int ItemLimit { get; }

    /// <summary>Gets the configured maximum retained UTF-16 message size.</summary>
    public long MessageByteLimit { get; }

    /// <summary>Gets the number of accepted items waiting to be processed.</summary>
    public long PendingItemCount { get; }

    /// <summary>Gets the UTF-16 byte size of accepted items waiting to be processed.</summary>
    public long PendingMessageBytes { get; }

    /// <summary>Gets the cumulative number of newest items rejected by this queue.</summary>
    public long DroppedItemCount { get; }

    /// <summary>Gets the cumulative UTF-16 byte size of rejected messages.</summary>
    public long DroppedMessageBytes { get; }
}

/// <summary>
/// An optional status source for queues maintained by a log service.
/// </summary>
public interface ILogQueueStatusProvider {
    /// <summary>Gets a snapshot of the service's own pending queue.</summary>
    LogQueueStatistics QueueStatus { get; }
}

/// <summary>
/// Queue status for one named log destination.
/// </summary>
public sealed class LogQueueStatus {
    internal LogQueueStatus(LogQueueStatistics dispatchQueue,
                            LogQueueStatistics serviceQueue) {
        DispatchQueue = dispatchQueue;
        ServiceQueue = serviceQueue;
    }

    /// <summary>Gets the per-destination service dispatch queue snapshot.</summary>
    public LogQueueStatistics DispatchQueue { get; }

    /// <summary>
    /// Gets the service's own queue snapshot when it provides one; otherwise null.
    /// </summary>
    public LogQueueStatistics ServiceQueue { get; }
}
