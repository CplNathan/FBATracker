// <copyright file="ScrapeSellerAmpService.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Services;

using FBATrackerReact.Models.Amazon;
using FBATrackerReact.Models.Product;
using FBATrackerReact.Server.Models.ConcurrentQueue;
using FBATrackerReact.Server.Services.Batch;
using FBATrackerReact.Server.Services.Database;
using FBATrackerReact.Server.Services.Scrape;
using Microsoft.EntityFrameworkCore;

public class ScrapeSellerAmpService(IFbaDbContext dbContext, IBatchWorker<SellerAmpData> sellerAmpWorker, ILogger<ScrapeAmazonService> logger) : ScrapePostProcessor(logger), IScrapePostProcessor
{
    public override async Task ProcessImplementation(bool isResumed, Guid jobId, CancellationToken token)
    {
        var scrapeJob = dbContext.ScrapeJobs
            .Include(x => x.Products)
            .ThenInclude(x => x.AmazonProduct)
            .Single(x => x.Id == jobId);

        List<ScrapedProduct> scrapedProducts = scrapeJob.Products
            .Where(x => x.Profit > 0)
            .ToList();

        logger.LogInformation("Enriching SellerAmp results for {scraperSource} enriching {results} results", scrapeJob.Source, scrapedProducts.Count);
        for (int i = 0; i < scrapedProducts.Count; i += sellerAmpWorker.BatchSize)
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            var currentBatch = scrapedProducts.Skip(i).Take(Math.Min(sellerAmpWorker.BatchSize, scrapedProducts.Count - i)).ToList();

            await this.EnrichSellerAmpData(currentBatch, token);
        }

        if (token.IsCancellationRequested)
        {
            dbContext.ScrapeJobs.Entry(scrapeJob).Reload();

            scrapeJob.Abandoned = true;

            dbContext.ScrapeJobs.Update(scrapeJob);
            dbContext.SaveChanges();
        }
    }

    private async Task EnrichSellerAmpData(IEnumerable<ScrapedProduct> currentBatch, CancellationToken token)
    {
        var requestResult = sellerAmpWorker.EnqueueItemsAsync(currentBatch.Select(x => new BatchQueueItem<SellerAmpData>(x.AmazonProduct.AmazonStandardIdentificationNumber, x.Name)), token);

        await foreach (BatchQueueItem<SellerAmpData> sellerAmpData in requestResult)
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            try
            {
                ScrapedProduct currentProduct = currentBatch.First(x => x.AmazonProduct.AmazonStandardIdentificationNumber == sellerAmpData.ItemReference);
                dbContext.ScrapedProducts.Entry(currentProduct).Reload();
                if (sellerAmpData.ItemData == default)
                {
                    continue;
                }

                currentProduct.AmazonProduct.SellerAmpData = sellerAmpData.ItemData;
                dbContext.AmazonProducts.Update(currentProduct.AmazonProduct);
                dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while saving seller amp data {amazonData}", sellerAmpData);
            }
        }
    }
}
