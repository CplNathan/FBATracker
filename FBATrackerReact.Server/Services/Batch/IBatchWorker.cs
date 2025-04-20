// <copyright file="IBatchWorker.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Services.Batch;

using FBATrackerReact.Server.Models.ConcurrentQueue;

public interface IBatchWorker<TType> : IHostedService
    where TType : class, new()
{
    int BatchSize { get; }

    ValueTask<BatchQueueItem<TType>> EnqueueItem(BatchQueueItem<TType> requestsLambda, CancellationToken token);

    ValueTask<List<BatchQueueItem<TType>>> EnqueueItems(IEnumerable<BatchQueueItem<TType>> requestsLambda, CancellationToken token)
        => this.EnqueueItemsAsync(requestsLambda, token).ToListAsync(cancellationToken: token);

    IAsyncEnumerable<BatchQueueItem<TType>> EnqueueItemsAsync(IEnumerable<BatchQueueItem<TType>> requestLambda, CancellationToken token);
}