// <copyright file="ItemPricingBatchWorker.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Services.Batch;

using FBATrackerReact.Server.Constants;
using FBATrackerReact.Server.Models.ConcurrentQueue;
using FikaAmazonAPI;
using FikaAmazonAPI.AmazonSpApiSDK.Models.ProductPricing;
using FikaAmazonAPI.Parameter.ProductPricing;
using static FikaAmazonAPI.Utils.Constants;

public sealed class ItemPricingBatchWorker(AmazonConnection amazonConnection, ILogger<IBatchWorker<GetOffersResult>> logger) : BaseBatchWorker<GetOffersResult>(logger, (requestItems, token) => GetPricingImplementationBatch(logger, amazonConnection, requestItems, token), TimeSpan.FromSeconds(10))
{
    private static async Task<List<BatchQueueItem<GetOffersResult>>> GetPricingImplementationBatch(ILogger<IBatchWorker<GetOffersResult>> logger, AmazonConnection amazonConnection, IEnumerable<BatchQueueItem<GetOffersResult>> productRequest, CancellationToken token)
    {
        logger.LogTrace("Searching pricing for {items}", productRequest.Select(x => x.ItemReference));

        FikaAmazonAPI.Utils.MarketPlace currentMarketplace = AppConstants.DefaultMarketPlace;

        List<ItemOffersRequest> itemOffers = productRequest.Select(product =>
            new ItemOffersRequest()
            {
                QueryParams = new ParameterGetItemOffers()
                {
                    MarketplaceId = currentMarketplace.ID,
                    Asin = product.ItemReference,
                    ItemCondition = ItemCondition.New,
                    CustomerType = CustomerType.Consumer,
                },
            }).ToList();

        GetBatchOffersResponse result = await amazonConnection.ProductPricing.GetItemOffersBatchAsync(
            new ParameterGetItemOffersBatchRequest()
            {
                ParameterGetItemOffers = itemOffers,
            },
            token);

        var expandedPayloads = result.Responses.Select(x => x?.Body?.Payload);
        logger.LogTrace("Search pricing results {items}", expandedPayloads);

        IEnumerable<BatchQueueItem<GetOffersResult>> products = productRequest.Select(request =>
        {
            request.ItemData = expandedPayloads?.FirstOrDefault(x => x?.ASIN == request.ItemReference);
            return request;
        }).Where(x => x.ItemData != default);

        return products.ToList();
    }
}