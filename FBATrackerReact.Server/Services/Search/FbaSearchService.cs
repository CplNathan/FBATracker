// <copyright file="FbaSearchService.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Services.Search;

using FBATrackerReact.Models.Amazon;
using FBATrackerReact.Server.Models.ConcurrentQueue;
using FBATrackerReact.Server.Services.Batch;
using FikaAmazonAPI.AmazonSpApiSDK.Models.ProductFees;

public class FbaSearchService(IBatchWorker<AmazonEligibility> eligibilityWorker, IBatchWorker<FeesEstimate> feeWorker)
{
    public async Task<FeesEstimate> GetFees(string productAsin, decimal priceGbp, CancellationToken token)
    {
        return (await feeWorker.EnqueueItem(new BatchQueueItem<FeesEstimate>(productAsin, string.Empty) { OptionalData = priceGbp }, token))?.ItemData;
    }

    public async Task<AmazonEligibility> GetEligibility(string productAsin, CancellationToken token)
    {
        return (await eligibilityWorker.EnqueueItem(new BatchQueueItem<AmazonEligibility>(productAsin, string.Empty), token))?.ItemData;
    }
}