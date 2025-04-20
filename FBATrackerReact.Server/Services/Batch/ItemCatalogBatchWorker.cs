// <copyright file="ItemCatalogBatchWorker.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Services.Batch;

using FBATrackerReact.Server.Constants;
using FBATrackerReact.Server.Models.ConcurrentQueue;
using FikaAmazonAPI;
using FikaAmazonAPI.AmazonSpApiSDK.Models.CatalogItems.V20220401;
using FikaAmazonAPI.Parameter.CatalogItems;
using static FikaAmazonAPI.Utils.Constants;

public sealed class ItemCatalogBatchWorker(AmazonConnection amazonConnection, ILogger<IBatchWorker<Item>> logger) : BaseBatchWorker<Item>(logger, (requestItems, token) => GetItemsImplementationBatch(logger, amazonConnection, requestItems, token), TimeSpan.FromSeconds(10))
{
    private static async Task<List<BatchQueueItem<Item>>> GetItemsImplementationBatch(ILogger<IBatchWorker<Item>> logger, AmazonConnection amazonConnection, IEnumerable<BatchQueueItem<Item>> productRequest, CancellationToken token)
    {
        logger.LogTrace("Searching catalog for {items}", productRequest.Select(x => x.ItemReference));

        FikaAmazonAPI.Utils.MarketPlace currentMarketplace = AppConstants.DefaultMarketPlace;

        IList<Item> result = await amazonConnection.CatalogItem.SearchCatalogItems202204Async(
            new ParameterSearchCatalogItems202204
            {
                marketplaceIds = [currentMarketplace.ID],
                sellerId = amazonConnection.GetCurrentSellerID,
                identifiersType = IdentifiersType.EAN,
                identifiers = productRequest.Select(x => x.ItemReference).ToList(),
                includedData = [IncludedData.salesRanks,
                                IncludedData.summaries,
                                IncludedData.dimensions,
                                IncludedData.identifiers,
                                IncludedData.images],
            },
            token);

        logger.LogTrace("Search catalog results {items}", result.SelectMany(x => x.Summaries));

        IEnumerable<BatchQueueItem<Item>> products = productRequest.Select(request =>
        {
            request.ItemData = result.FirstOrDefault(x => x.Summaries.Any(x => x.PartNumber == request.ItemReference) || x.Identifiers.Any(y => y.Identifiers.Any(z => z.Identifier == request.ItemReference)));
            return request;
        }).Where(x => x.ItemData != default);

        return products.ToList();
    }
}