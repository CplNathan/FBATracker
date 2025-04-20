// <copyright file="BatchQueue.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Models.ConcurrentQueue;

using System.Collections.Concurrent;
using System.Threading.Channels;

public class BatchQueue<TType>(TimeSpan batchCooldown, int batchSize)
    where TType : new()
{
    private readonly ConcurrentDictionary<string, BatchQueueItem<TType>> currentQueue = new();

    private readonly ConcurrentDictionary<Guid, Channel<BatchQueueItem<TType>>> trackedChannels = new();

    private DateTime lastExecuted = DateTime.UtcNow;

    private DateTime lastAdded = DateTime.UtcNow;

    public int NextExecutionMillis
    {
        get
        {
            TimeSpan minDelay = TimeSpan.FromSeconds(1);
            TimeSpan maxDelay = batchCooldown <= TimeSpan.MinValue ? minDelay : batchCooldown;
            TimeSpan delay = batchCooldown <= TimeSpan.MinValue ? minDelay : batchCooldown - this.LastExecuted;

            return (int)Math.Clamp(delay.TotalMilliseconds, minDelay.TotalMilliseconds, maxDelay.TotalMilliseconds);
        }
    }

    public bool IsReady
    {
        get
        {
            var hasLastAddedElapsed = this.currentQueue.Count < batchSize && this.LastAdded > TimeSpan.FromSeconds(1);
            var hasCooldownElapsed = this.LastExecuted > batchCooldown;
            var hasEnoughItems = this.currentQueue.Count >= batchSize;

            return (!this.currentQueue.IsEmpty && hasLastAddedElapsed) || (hasCooldownElapsed && hasEnoughItems);
        }
    }

    private TimeSpan LastExecuted => (DateTime.UtcNow - this.lastExecuted).Duration();

    private TimeSpan LastAdded => (DateTime.UtcNow - this.lastAdded).Duration();

    public Channel<BatchQueueItem<TType>> Enqueue(IEnumerable<BatchQueueItem<TType>> requestItems)
    {
        var requestList = requestItems.ToList();

        return this.IssueChannel(requestList.Count, (channel, batchId) => [.. requestItems.Select(x => x.SetChannel(channel, batchId))]);
    }

    public bool ConsumeBatch(out IEnumerable<BatchQueueItem<TType>> requestQueueItems)
    {
        requestQueueItems = [.. this.currentQueue
            .OrderBy(x => x.Value.RequestedBy)
            .ThenBy(x => x.Value.InternalBatch.BatchId)
            .Select(x => x.Value)
            .Take(Math.Min(this.currentQueue.Count, batchSize))];

        foreach (var requestItem in requestQueueItems)
        {
            this.currentQueue.Remove(requestItem.ItemReference, out _);
        }

        return requestQueueItems.Any();
    }

    public async Task CompleteBatch(IEnumerable<BatchQueueItem<TType>> requestQueueItems, CancellationToken token)
    {
        foreach (var item in requestQueueItems)
        {
            await item.InternalBatch.Channel.Writer.WriteAsync(item, token);
        }

        var completedChannelGuids = this.trackedChannels
            .Keys
            .Where(trackedBatch => !this.currentQueue.Values.Any(queue => trackedBatch == queue.InternalBatch.BatchId))
            .Distinct()
            .ToList();

        if (completedChannelGuids.Count > 0)
        {
            foreach (var channelGuid in completedChannelGuids)
            {
                this.trackedChannels.Remove(channelGuid, out Channel<BatchQueueItem<TType>> channel);
                channel.Writer.Complete();
            }
        }

        this.lastExecuted = DateTime.UtcNow;
    }

    private Channel<BatchQueueItem<TType>> IssueChannel(int? overrideBatchSize, Func<Channel<BatchQueueItem<TType>>, Guid, IEnumerable<BatchQueueItem<TType>>> requestsLambda)
    {
        Channel<BatchQueueItem<TType>> newChannel = Channel.CreateBounded<BatchQueueItem<TType>>(new BoundedChannelOptions(overrideBatchSize ?? batchSize)
        {
            FullMode = BoundedChannelFullMode.Wait,
        });

        var newBatchId = Guid.NewGuid();
        foreach (BatchQueueItem<TType> request in requestsLambda(newChannel, newBatchId))
        {
            this.currentQueue.TryAdd(request.ItemReference, request);
        }

        this.trackedChannels.TryAdd(newBatchId, newChannel);
        this.lastAdded = DateTime.UtcNow;

        return newChannel;
    }
}
