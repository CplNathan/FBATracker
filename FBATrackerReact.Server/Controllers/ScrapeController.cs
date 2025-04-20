// <copyright file="ScrapeController.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Controllers;

using System.Text;
using FBATrackerReact.Models.Amazon;
using FBATrackerReact.Models.Amazon.Dto;
using FBATrackerReact.Models.Enum;
using FBATrackerReact.Models.Product.Dto;
using FBATrackerReact.Models.Scrape;
using FBATrackerReact.Server.Extensions;
using FBATrackerReact.Server.Models;
using FBATrackerReact.Server.Services;
using FBATrackerReact.Server.Services.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("/api/scrape/")]
public class ScrapeController(IFbaDbContext dbContext, ILogger<ScrapeController> logger, IEnumerable<ScrapeService> scrapeServices) : Controller
{
    [HttpGet("latest")]
    [ResponseCache(Duration = 120, VaryByQueryKeys = ["*"], Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<IActionResult> FetchLatest([FromQuery] ProductListType? listType, CancellationToken token)
    {
        logger.LogDebug("Calling FetchLatest with {listType}", listType);

        ScrapedProductsDto productResults = await this.GetScrapeDto(true, listType ?? ProductListType.All, token);

        return this.Json(productResults);
    }

    [HttpGet("realtime")]
    [ResponseCache(Duration = 60, VaryByQueryKeys = ["*"], Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<IActionResult> FetchRealtime([FromQuery] ProductListType? listType, CancellationToken token)
    {
        logger.LogDebug("Calling FetchRealtime with {listType}", listType);

        ScrapedProductsDto productResults = await this.GetScrapeDto(false, listType ?? ProductListType.All, token);

        return this.Json(productResults);
    }

    [HttpGet("status")]
    public async Task<IActionResult> FetchStatus(CancellationToken token)
    {
        logger.LogDebug("Calling FetchStatus");

        IQueryable<IGrouping<string, ScrapeStatusDto>> scrapeJobs = dbContext
            .ScrapeJobs
            .Where(x => x.Abandoned == false)
            .OrderByDescending(x => x.Ended ?? x.Started)
            .Select(x => new ScrapeStatusDto()
            {
                Source = x.Source,
                ScrapedProducts = x.Products.Count(y => y.Source == x.Source),
                AmazonScrapedProducts = x.Products.Where(y => y.AmazonProduct != default).Count(y => y.Source == x.Source),
                SellerAmpScrapedProducts = x.Products.Where(y => y.AmazonProduct.SellerAmpData.SalesRank != null).Count(y => y.Source == x.Source),
                ScrapeStarted = x.Started,
                ScrapeEnded = x.Ended,
            })
            .GroupBy(x => x.Source);

        Dictionary<string, IEnumerable<ScrapeStatusDto>> scrapeResult = await scrapeJobs.ToDictionaryAsync(x => x.Key, x => x.OrderByDescending(x => x.ScrapeEnded ?? x.ScrapeStarted).Take(5).AsEnumerable(), cancellationToken: token);
        return this.Json(scrapeResult);
    }

    [HttpGet("manual/{source}")]
    public IActionResult ManualScrape(string source)
    {
        logger.LogDebug("Calling ManualScrape with source {source}", source);

        ScrapeService matchedService = scrapeServices.SingleOrDefault(x => x.Source.Equals(source, StringComparison.InvariantCultureIgnoreCase));
        if (matchedService == default)
        {
            logger.LogWarning("Called ManualScrape with source {source} but could not find source", source);
            return this.Json(false);
        }

        if (!matchedService.CanStart)
        {
            return this.Json(false);
        }

        _ = matchedService.Scrape(CancellationToken.None);

        return this.Json(true);
    }

    [HttpGet("download")]
    public async Task<FileContentResult> DownloadRealtime(CancellationToken token)
    {
        logger.LogDebug("Calling DownloadRealtime");

        StringBuilder csvData = new();
        csvData.AppendLine("Retailer, Product Name, EAN/UPC, Retailer Price, Resell Price, Fees, Profit");

        ScrapedProductsDto productResults = await this.GetScrapeDto(false, ProductListType.New, token);
        foreach (ScrapedProductDto product in productResults.ScrapedProducts.Values.SelectMany(x => x))
        {
            AmazonProductDto amazonProduct = productResults.AmazonProducts[product.AmazonStandardIdentificationNumber];
            AmazonPricingDto latestPricing = amazonProduct.ProductPricing;
            csvData.AppendLine($"{product.Source}, {amazonProduct.Name}, {product.ProductCode}, {product.Price}, {latestPricing.LowestPreferredPrice}, {latestPricing.Fees}, {product.Profit}");
        }

        return this.File(new UTF8Encoding().GetBytes(csvData.ToString()), "text/csv", "Report.csv");
    }

    [HttpPost("view")]
    public async Task<IActionResult> ViewedProduct([FromQuery] string productAsin, CancellationToken token)
    {
        logger.LogDebug("Calling ViewedProduct with Asin {productAsin}", productAsin);

        if (string.IsNullOrWhiteSpace(productAsin))
        {
            logger.LogWarning("Called ViewedProduct with Asin {productAsin} but Asin was invalid", productAsin);
            return this.Json(false);
        }

        var productFinder = dbContext.AmazonProducts
            .Where(x => x.AmazonStandardIdentificationNumber == productAsin)
            .Include(x => x.ProductPricing)
            .Include(x => x.ProductVisibility);

        AmazonProduct product = productFinder.FirstOrDefault();
        if (product == default)
        {
            logger.LogWarning("Called ViewedProduct with Asin {productAsin} but could not find Asin", productAsin);
        }
        else if (product.ProductVisibility.ProductList != ProductListType.Watched)
        {
            product.ProductVisibility.ProductList = ProductListType.Viewed;
            dbContext.AmazonProducts.Update(product);
            dbContext.SaveChanges();
        }

        return this.Json(await productFinder.AnyAsync(cancellationToken: token));
    }

    [HttpPost("watch")]
    public async Task<IActionResult> WatchlistProduct([FromQuery] string productAsin, CancellationToken token)
    {
        logger.LogDebug("Calling WatchlistProduct with Asin {productAsin}", productAsin);

        if (string.IsNullOrWhiteSpace(productAsin))
        {
            logger.LogWarning("Called WatchlistProduct with Asin {productAsin} but Asin was invalid", productAsin);
            return this.Json(false);
        }

        var productFinder = dbContext.AmazonProducts
            .Where(x => x.AmazonStandardIdentificationNumber == productAsin)
            .Include(x => x.ProductPricing);

        AmazonProduct product = productFinder.FirstOrDefault();
        if (product == default)
        {
            logger.LogWarning("Called WatchlistProduct with Asin {productAsin} but could not find Asin", productAsin);
        }
        else
        {
            product.ProductVisibility.ProductList = ProductListType.Watched;
            dbContext.AmazonProducts.Update(product);
            dbContext.SaveChanges();
        }

        return this.Json(await productFinder.AnyAsync(cancellationToken: token));
    }

    [HttpGet("lists")]
    public IActionResult GetAvailableLists()
    {
        var listOptions = Enum.GetValues<ProductListType>();

        var listDictionary = listOptions.ToDictionary(x => (int)x, x => x.ToString());
        return this.Json(listDictionary);
    }

    private async Task<ScrapedProductsDto> GetScrapeDto(bool whereEnded, ProductListType listType, CancellationToken token)
    {
        IQueryable<ScrapeJobSet> baseFilter = (whereEnded ? dbContext.CompletedJobs : dbContext.ScrapeJobs)
            .Where(x => x.Abandoned == false);

        var baseFilterJobs = await baseFilter
            .GroupBy(x => x.Source)
            .Select(x => x.OrderByDescending(x => x.Ended ?? x.Started).Take(5))
            .AsNoTracking()
            .ToListAsync(token);

        var baseFilterJobIds = baseFilterJobs
            .SelectMany(x => x.Select(y => y.Id))
            .ToList();

        var productBase = dbContext.ScrapedProducts
            .Where(x => baseFilterJobIds.Contains(x.Owner.Id))
            .Where(x => listType == ProductListType.All || x.AmazonProduct.ProductVisibility.ProductList == listType)
            .Where(x => x.Profit > 0)
            .ExcludeBadListings()
            .OrderByDescending(x => x.Owner.Ended ?? x.Owner.Started);

        var scrapedProducts = await productBase
            .Include(x => x.AmazonProduct)
            .OrderByDescending(x => x.Owner.Ended ?? x.Owner.Started)
            .AsSplitQuery()
            .Select(x => new ScrapedProductDto(x))
            .AsNoTracking()
            .ToListAsync(token);

        var amazonProducts = await productBase
            .Select(x => x.AmazonProduct)
            .Distinct()
            .Select(x => new AmazonProductDto(x))
            .AsNoTracking()
            .ToDictionaryAsync(x => x.AmazonStandardIdentificationNumber, token);

        return new ScrapedProductsDto()
        {
            AmazonProducts = amazonProducts,
            ScrapedProducts = scrapedProducts
                .AsParallel()
                .GroupBy(x => new { x.Source, x.ProductCode })
                .Select(x => x.First())
                .GroupBy(x => x.ProductCode)
                .ToDictionary(x => x.Key, x => x.AsEnumerable()),
        };
    }
}