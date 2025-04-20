// <copyright file="PricingSearchService.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Services.Search;

using FBATrackerReact.Server.Models.ConcurrentQueue;
using FBATrackerReact.Server.Services.Batch;
using FikaAmazonAPI.AmazonSpApiSDK.Models.ProductPricing;

public class PricingSearchService(IBatchWorker<GetOffersResult> pricingWorker)
{
    public async Task<GetOffersResult> GetPricing(string productAsin, CancellationToken token)
    {
        return (await pricingWorker.EnqueueItem(new BatchQueueItem<GetOffersResult>(productAsin, string.Empty), token))?.ItemData;
    }
}