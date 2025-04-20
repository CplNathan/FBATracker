// <copyright file="AmazonBatchWorker.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Services.Batch;

using FBATrackerReact.Models.Amazon;
using FBATrackerReact.Models.Enum;
using FBATrackerReact.Server.Models.ConcurrentQueue;
using FBATrackerReact.Server.Services.Database;
using FikaAmazonAPI.AmazonSpApiSDK.Models.CatalogItems.V20220401;
using FikaAmazonAPI.AmazonSpApiSDK.Models.ProductFees;
using FikaAmazonAPI.AmazonSpApiSDK.Models.ProductPricing;
using Microsoft.EntityFrameworkCore;

public sealed class AmazonBatchWorker(IServiceProvider serviceProvider, ILogger<IBatchWorker<AmazonProduct>> logger) : BaseBatchWorker<AmazonProduct>(logger, (requestItems, token) => EnrichBatchImplementation(logger, serviceProvider, requestItems, token), TimeSpan.MinValue)
{
    private static async Task<List<BatchQueueItem<AmazonProduct>>> EnrichBatchImplementation(ILogger<IBatchWorker<AmazonProduct>> logger, IServiceProvider serviceProvider, IEnumerable<BatchQueueItem<AmazonProduct>> productRequest, CancellationToken token)
    {
        using var scope = serviceProvider.CreateScope();
        var serviceContainer = scope.ServiceProvider;

        var dbContext = serviceContainer.GetRequiredService<IFbaDbContext>();
        var itemWorker = serviceProvider.GetService<IBatchWorker<Item>>();
        var pricingWorker = serviceProvider.GetRequiredService<IBatchWorker<GetOffersResult>>();
        var eligibilityWorker = serviceProvider.GetRequiredService<IBatchWorker<AmazonEligibility>>();
        var feeWorker = serviceProvider.GetRequiredService<IBatchWorker<FeesEstimate>>();

        var productDetailsList = await GetProductData(dbContext, itemWorker, productRequest, token);

        if (productDetailsList.Count == 0)
        {
            return productRequest.ToList();
        }

        var eligibilityTask = eligibilityWorker.EnqueueItems(productDetailsList.Select(x => new BatchQueueItem<AmazonEligibility>(x.Asin, x.Name, x.Source)), token);
        var pricingTask = pricingWorker.EnqueueItems(productDetailsList.Select(x => new BatchQueueItem<GetOffersResult>(x.Asin, x.Name, x.Source)), token);

        var pricingBatch = (await pricingTask).ToDictionary(x => x.ItemReference, x => x);

        var feeTask = feeWorker.EnqueueItems(pricingBatch.Values.Select(price => new BatchQueueItem<FeesEstimate>(price.ItemReference, price.ItemName, price.RequestedBy) { OptionalData = GetPreferredPrice(price.ItemData) ?? 0 }), token);
        var feeBatch = (await feeTask).ToDictionary(x => x.ItemReference, x => x);
        var eligibilityBatch = (await eligibilityTask).ToDictionary(x => x.ItemReference, x => x);

        foreach (var productData in productDetailsList)
        {
            AmazonProduct amazonProduct = default;
            try
            {
                GetOffersResult pricingData = pricingBatch.GetValueOrDefault(productData.Asin).ItemData;
                AmazonEligibility eligibilityData = eligibilityBatch.GetValueOrDefault(productData.Asin).ItemData;
                FeesEstimate feeData = feeBatch.GetValueOrDefault(productData.Asin).ItemData;

                decimal? lowestPreferredPrice = GetPreferredPrice(pricingData);

                decimal? highestPrice = pricingData?.Offers
                    ?.OrderByDescending(x => x.ListingPrice.Amount)
                    ?.Select(x => x.ListingPrice.Amount ?? default)
                    ?.FirstOrDefault();

                amazonProduct = new AmazonProduct
                {
                    Name = productData.Name,
                    ProductPricing = new AmazonPricing
                    {
                        LowestPreferredPrice = lowestPreferredPrice,
                        HighestPrice = highestPrice,
                        Fees = (lowestPreferredPrice > 0 ? feeData : new FeesEstimate())?.TotalFeesEstimate?.Amount,
                    },
                    AmazonStandardIdentificationNumber = productData.Asin,
                    ProductEligibility = eligibilityData,
                };

                BatchQueueItem<AmazonProduct> foundBatchItem = productRequest.FirstOrDefault(productItem => productData.ProductCodes.Contains(productItem.ItemReference));
                if (amazonProduct != default && foundBatchItem != default)
                {
                    foundBatchItem.ItemData = amazonProduct;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while enriching scraped data: {Asin}", productData.Asin);
            }
        }

        return productRequest.ToList();
    }

    private static async Task<List<ProductData>> GetProductData(IFbaDbContext dbContext, IBatchWorker<Item> itemWorker, IEnumerable<BatchQueueItem<AmazonProduct>> productRequests, CancellationToken token)
    {
        var requestedProductCodes = productRequests.Select(x => x.ItemReference).ToList();
        var existingProducts = await dbContext.ScrapedProducts
            .Include(x => x.ProductReference)
            .Where(x => x.ProductReference != default && !string.IsNullOrWhiteSpace(x.ProductReference.Reference))
            .Where(x => requestedProductCodes.Contains(x.ProductReference.Reference))
            .Include(x => x.AmazonProduct)
            .Select(x => x.AmazonProduct)
            .Where(x => x != default)
            .Distinct()
            .Include(x => x.ReferencedProducts)
            .ThenInclude(x => x.ProductReference)
            .Where(x => x.ReferencedProducts.Any(y => y.ProductReference != null && !string.IsNullOrWhiteSpace(y.ProductReference.Reference)))
            .ToListAsync(token);

        var missingProducts = productRequests
            .ExceptBy(existingProducts.SelectMany(x => x.ReferencedProducts.Select(y => y.ProductReference?.Reference)).Where(z => !string.IsNullOrWhiteSpace(z)), x => x.ItemReference);

        var missingByAsin = missingProducts.Where(x => x.OptionalData as ProductReferenceType? == ProductReferenceType.ASIN);

        var missingByDefault = missingProducts
            .ExceptBy(missingByAsin.Select(x => x.ItemReference), x => x.ItemReference);

        return await Task.Run(async () =>
        {
            var missingResultsAsin = await GetProductDataFromUPC(itemWorker, missingByDefault, token);
            var missingResultsDefault = GetProductDataFromExistingAsin(dbContext, missingByAsin);

            var existingResults = existingProducts.Select(x => new ProductData()
            {
                Asin = x.AmazonStandardIdentificationNumber,
                Name = x.Name,
                ProductCodes = x.ReferencedProducts.Select(y => y.ProductReference?.Reference).Where(z => !string.IsNullOrWhiteSpace(z)).ToList(),
                Source = productRequests.FirstOrDefault()?.RequestedBy,
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Asin))
            .ToList();

            List<ProductData> data = [.. missingResultsAsin, .. missingResultsDefault, .. existingResults];

            return data;
        });
    }

    private static async Task<List<ProductData>> GetProductDataFromUPC(IBatchWorker<Item> itemWorker, IEnumerable<BatchQueueItem<AmazonProduct>> productRequests, CancellationToken token)
    {
        if (!productRequests.Any())
        {
            return [];
        }

        var missingProducts = productRequests
            .Select(x => new BatchQueueItem<Item>(x.ItemReference, x.ItemName, x.RequestedBy));
        var missingProductsRequests = (await itemWorker.EnqueueItems(missingProducts, token)).Where(x => !string.IsNullOrWhiteSpace(x.ItemData?.Asin));

        var missingProductDetailsListResult = missingProductsRequests
            .Select(x => new ProductData()
            {
                Asin = x.ItemData.Asin,
                Name = x.ItemData.Summaries.FirstOrDefault()?.ItemName,
                ProductCodes = x.ItemData.Identifiers.SelectMany(y => y.Identifiers).Select(y => y.Identifier).ToList(),
                Source = x.RequestedBy,
            });

        return missingProductDetailsListResult
            .DistinctBy(x => x.Asin)
            .Where(x => !string.IsNullOrWhiteSpace(x.Asin))
            .ToList();
    }

    private static List<ProductData> GetProductDataFromExistingAsin(IFbaDbContext dbContext, IEnumerable<BatchQueueItem<AmazonProduct>> productRequests)
    {
        if (!productRequests.Any())
        {
            return [];
        }

        var missingProductDetailsListResult = productRequests
            .Select(x => new ProductData()
            {
                Asin = x.ItemReference,
                Name = dbContext.ScrapedProducts.First(y => y.ProductReference.Reference.Contains(x.ItemReference)).Name,
                ProductCodes = [x.ItemReference],
                Source = x.RequestedBy,
            });

        return missingProductDetailsListResult
            .DistinctBy(x => x.Asin)
            .Where(x => !string.IsNullOrWhiteSpace(x.Asin))
            .ToList();
    }

    private static decimal? GetPreferredPrice(GetOffersResult pricingData)
    {
        if (pricingData == default)
        {
            return default;
        }

        bool hasAnyFba = pricingData.Offers.Any(x => x.IsFulfilledByAmazon ?? false);

        return pricingData.Offers
            .OrderBy(x => x.ListingPrice.Amount)
            .Where(x => !hasAnyFba || (x.IsFulfilledByAmazon ?? false))
            .Select(x => x.ListingPrice.Amount ?? default)
            .FirstOrDefault();
    }

    internal class ProductData
    {
        public string Asin { get; set; }

        public string Name { get; set; }

        public List<string> ProductCodes { get; set; }

        public string Source { get; set; }
    }
}