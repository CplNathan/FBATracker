// <copyright file="BaseBatchWorker.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Services.Batch;

using System.Threading.Channels;
using FBATrackerReact.Server.Models.ConcurrentQueue;
using Polly;
using Polly.Retry;

public abstract class BaseBatchWorker<TType>(ILogger<IBatchWorker<TType>> logger, Func<IEnumerable<BatchQueueItem<TType>>, CancellationToken, Task<List<BatchQueueItem<TType>>>> batchTask, TimeSpan batchCooldown, int batchSize = 20, bool useRetry = false) : BackgroundService, IBatchWorker<TType>
     where TType : class, new()
{
    private readonly BatchQueue<TType> requestQueue = new(batchCooldown, batchSize);

    public int BatchSize { get => batchSize; }

    protected static AsyncRetryPolicy RetryPolicy { get => Policy.Handle<Exception>().WaitAndRetryAsync(5, (attempt) => TimeSpan.FromSeconds(attempt)); }

    public ValueTask<BatchQueueItem<TType>> EnqueueItem(BatchQueueItem<TType> requestItem, CancellationToken token)
    {
        Channel<BatchQueueItem<TType>> channel = this.requestQueue.Enqueue([requestItem]);

        return channel.Reader.ReadAsync(token);
    }

    public IAsyncEnumerable<BatchQueueItem<TType>> EnqueueItemsAsync(IEnumerable<BatchQueueItem<TType>> requestItems, CancellationToken token)
    {
        Channel<BatchQueueItem<TType>> channel = this.requestQueue.Enqueue(requestItems);

        return channel.Reader.ReadAllAsync(token);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return this.QueueWorker(stoppingToken);
    }

    protected async Task QueueWorker(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (this.requestQueue.IsReady && this.requestQueue.ConsumeBatch(out IEnumerable<BatchQueueItem<TType>> currentBatch))
            {
                try
                {
                    logger.LogDebug("Processing batch {currentInstance}", this.GetType().Name);
                    currentBatch = useRetry ? await RetryPolicy.ExecuteAsync(() => batchTask(currentBatch, token)) : await batchTask(currentBatch, token);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while processing batch {currentInstance}", this.GetType().Name);
                }
                finally
                {
                    await this.requestQueue.CompleteBatch(currentBatch, token);
                }
            }

            await Task.Delay(this.requestQueue.NextExecutionMillis, token);
        }
    }
}