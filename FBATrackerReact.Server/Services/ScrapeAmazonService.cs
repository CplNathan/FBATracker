// <copyright file="ScrapeAmazonService.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Services;

using FBATrackerReact.Models.Amazon;
using FBATrackerReact.Models.Enum;
using FBATrackerReact.Models.Product;
using FBATrackerReact.Server.Constants;
using FBATrackerReact.Server.Models.ConcurrentQueue;
using FBATrackerReact.Server.Services.Batch;
using FBATrackerReact.Server.Services.Database;
using FBATrackerReact.Server.Services.Scrape;
using Microsoft.EntityFrameworkCore;

public class ScrapeAmazonService(IFbaDbContext dbContext, IBatchWorker<AmazonProduct> amazonWorker, ILogger<ScrapeAmazonService> logger) : ScrapePostProcessor(logger), IScrapePostProcessor
{
    public override async Task ProcessImplementation(bool isResumed, Guid jobId, CancellationToken token)
    {
        var scrapeJob = dbContext.ScrapeJobs
            .Include(x => x.Products)
            .ThenInclude(x => x.AmazonProduct)
            .Single(x => x.Id == jobId);

        var scrapedCodes = scrapeJob.Products
            .Select(x => x.ProductReference?.Reference)
            .ToList();
        var wantedAmazonResults = dbContext.ScrapedProducts
            .Where(x => x.Owner.Abandoned == false)
            .Where(x => !string.IsNullOrWhiteSpace(x.ProductReference.Reference))
            .Where(x => scrapedCodes.Contains(x.ProductReference.Reference))
            .Select(x => new ScrapedItem()
            {
                Reference = x.ProductReference.Reference,
                HasAmazonItem = x.AmazonProduct != default,
                HasJobEnded = x.Owner.Ended.HasValue,
            })
            .GroupBy(x => x.Reference)
            .ToList()
            .AsParallel()
            .Select(x => new ScrapedItemCount()
            {
                Reference = x.Key,
                Count = x.Count(),
                AmazonCount = x.Count(y => y.HasAmazonItem),
                NoAmazonCount = x.Count(y => !y.HasAmazonItem && y.HasJobEnded),
            })
            .Where(x => x.Count == 0 || x.NoAmazonCount == 0 || x.AmazonCount > 0)
            .Select(x => x.Reference)
            .Distinct()
            .ToList();

        List<ScrapedProduct> scrapedProducts = scrapeJob.Products
            .AsParallel()
            .Where(x => !string.IsNullOrEmpty(x.ProductReference?.Reference))
            .DistinctBy(x => x.ProductReference.Reference)
            .Where(x => x.ProductReference.Type != ProductReferenceType.ASIN || (x.ProductReference.Type == ProductReferenceType.ASIN && RegexConstants.AsinPattern().IsMatch(x.ProductReference.Reference)))
            .Where(x => wantedAmazonResults.Contains(x.ProductReference.Reference))
            .Where(x => !isResumed || (isResumed && x.AmazonProduct == default))
            .ToList();

        logger.LogInformation("Enriching Amazon results for {scraperSource} enriching {results} results", scrapeJob.Source, scrapedProducts.Count);
        for (int i = 0; i < scrapedProducts.Count; i += amazonWorker.BatchSize)
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            var currentBatch = scrapedProducts.Skip(i).Take(Math.Min(amazonWorker.BatchSize, scrapedProducts.Count - i)).ToList();

            await this.EnrichAmazonData(currentBatch, token);
        }

        if (token.IsCancellationRequested)
        {
            dbContext.ScrapeJobs.Entry(scrapeJob).Reload();

            scrapeJob.Abandoned = true;

            dbContext.ScrapeJobs.Update(scrapeJob);
            dbContext.SaveChanges();
        }
    }

    private async Task EnrichAmazonData(IEnumerable<ScrapedProduct> currentBatch, CancellationToken token)
    {
        var requestResult = amazonWorker.EnqueueItemsAsync(currentBatch.Select(x => new BatchQueueItem<AmazonProduct>(x.ProductReference.Reference, x.Name, x.Source) { OptionalData = x.ProductReference.Type }), token);

        await foreach (BatchQueueItem<AmazonProduct> amazonData in requestResult)
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            try
            {
                ScrapedProduct currentProduct = currentBatch.First(x => x.ProductReference.Reference == amazonData.ItemReference);
                dbContext.ScrapedProducts.Entry(currentProduct).Reload();
                if (amazonData.ItemData == default)
                {
                    continue;
                }

                AmazonProduct amazonProduct = amazonData.ItemData;
                AmazonProduct existingAmazonProduct = dbContext.AmazonProducts.FirstOrDefault(x => x.AmazonStandardIdentificationNumber == amazonProduct.AmazonStandardIdentificationNumber);
                if (existingAmazonProduct != default)
                {
                    existingAmazonProduct.ProductPricing = amazonProduct.ProductPricing;
                    existingAmazonProduct.ProductEligibility = amazonProduct.ProductEligibility;
                    existingAmazonProduct.ReferencedProducts.Add(currentProduct);
                    dbContext.AmazonProducts.Update(existingAmazonProduct);
                }
                else
                {
                    amazonProduct.ReferencedProducts.Add(currentProduct);
                    dbContext.AmazonProducts.Add(amazonProduct);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while saving amazon product {amazonData}", amazonData);
            }
        }

        dbContext.SaveChanges();
    }

    internal class ScrapedItem
    {
        public string Reference { get; set; }

        public bool HasAmazonItem { get; set; }

        public bool HasJobEnded { get; set; }
    }

    internal class ScrapedItemCount
    {
        public string Reference { get; set; }

        public int Count { get; set; }

        public int AmazonCount { get; set; }

        public int NoAmazonCount { get; set; }
    }
}
