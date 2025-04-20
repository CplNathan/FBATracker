// <copyright file="ItemFeesBatchWorker.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Services.Batch;

using FBATrackerReact.Server.Constants;
using FBATrackerReact.Server.Models.ConcurrentQueue;
using FikaAmazonAPI;
using FikaAmazonAPI.AmazonSpApiSDK.Models.ProductFees;

public sealed class ItemFeesBatchWorker(AmazonConnection amazonConnection, ILogger<IBatchWorker<FeesEstimate>> logger) : BaseBatchWorker<FeesEstimate>(logger, (requestItems, token) => GetFeesImplementation(logger, amazonConnection, requestItems, token), TimeSpan.FromSeconds(1), 1)
{
    private static async Task<List<BatchQueueItem<FeesEstimate>>> GetFeesImplementation(ILogger<IBatchWorker<FeesEstimate>> logger, AmazonConnection amazonConnection, IEnumerable<BatchQueueItem<FeesEstimate>> referenceList, CancellationToken token)
    {
        BatchQueueItem<FeesEstimate> reference = referenceList.Single();

        logger.LogTrace("Searching fee for {items}", reference.ItemReference);

        if ((reference.OptionalData is decimal itemCost && itemCost <= 0) || reference.OptionalData == default)
        {
            logger.LogTrace("Searching fee for {items} failed because amount was default", reference.ItemReference);

            return [reference];
        }

        FeesEstimateResult feeData = await amazonConnection.ProductFee.GetMyFeesEstimateForASINAsync(
            reference.ItemReference,
            new FeesEstimateRequest
            {
                MarketplaceId = AppConstants.DefaultMarketPlace.ID,
                Identifier = Guid.NewGuid().ToString(),
                IsAmazonFulfilled = true,
                PriceToEstimateFees = new PriceToEstimateFees
                {
                    ListingPrice = new MoneyType
                    {
                        Amount = reference.OptionalData as decimal?,
                        CurrencyCode = AppConstants.DefaultMarketPlace.CurrencyCode.ToString(),
                    },
                },
            },
            token);

        logger.LogTrace("Search fee results {items}", feeData.FeesEstimate);

        reference.ItemData = feeData.FeesEstimate;

        return [reference];
    }
}