// <copyright file="CatalogSearchService.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Services.Search;

using FBATrackerReact.Server.Models.ConcurrentQueue;
using FBATrackerReact.Server.Services.Batch;
using FikaAmazonAPI.AmazonSpApiSDK.Models.CatalogItems.V20220401;

public class CatalogSearchService(IBatchWorker<Item> batchWorker)
{
    public async Task<Item> FindProductFromEan(string productEan, CancellationToken token)
    {
        return (await batchWorker.EnqueueItem(new BatchQueueItem<Item>(productEan, string.Empty), token)).ItemData;
    }
}