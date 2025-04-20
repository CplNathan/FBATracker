// <copyright file="ServiceRegistrationExtensions.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Extensions.Program;

using FBATrackerReact.ArgosScraper;
using FBATrackerReact.BandQScraper;
using FBATrackerReact.BargainFoxScraper;
using FBATrackerReact.BootsScraper;
using FBATrackerReact.CurrysScraper;
using FBATrackerReact.JohnLewisScraper;
using FBATrackerReact.Models.Amazon;
using FBATrackerReact.OverclockersScraper;
using FBATrackerReact.SaversScraper;
using FBATrackerReact.Server.Services;
using FBATrackerReact.Server.Services.Batch;
using FBATrackerReact.Server.Services.Database;
using FBATrackerReact.SharedScraper.Models;
using FikaAmazonAPI.AmazonSpApiSDK.Models.CatalogItems.V20220401;
using FikaAmazonAPI.AmazonSpApiSDK.Models.ProductFees;
using FikaAmazonAPI.AmazonSpApiSDK.Models.ProductPricing;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection RegisterScrapers(this IServiceCollection sc)
    {
        sc.AddSingleton<IBatchWorker<GetOffersResult>, ItemPricingBatchWorker>();
        sc.AddSingleton<IBatchWorker<Item>, ItemCatalogBatchWorker>();
        sc.AddSingleton<IBatchWorker<AmazonProduct>, AmazonBatchWorker>();
        sc.AddSingleton<IBatchWorker<SellerAmpData>, SellerAmpBatchWorker>();
        sc.AddSingleton<IBatchWorker<AmazonEligibility>, ItemEligibilityBatchWorker>();
        sc.AddSingleton<IBatchWorker<FeesEstimate>, ItemFeesBatchWorker>();
        sc.AddSingleton<SellerAmpWorker>();
        sc.AddHostedService<ScrapeWorker>();
        sc.AddHostedService(sp => sp.GetRequiredService<SellerAmpWorker>());
        sc.AddHostedService(sp => sp.GetRequiredService<IBatchWorker<GetOffersResult>>());
        sc.AddHostedService(sp => sp.GetRequiredService<IBatchWorker<Item>>());
        sc.AddHostedService(sp => sp.GetRequiredService<IBatchWorker<AmazonProduct>>());
        sc.AddHostedService(sp => sp.GetRequiredService<IBatchWorker<SellerAmpData>>());
        sc.AddHostedService(sp => sp.GetRequiredService<IBatchWorker<AmazonEligibility>>());
        sc.AddHostedService(sp => sp.GetRequiredService<IBatchWorker<FeesEstimate>>());
        sc.AddSingleton<BandQScraper>();
        sc.AddSingleton<JohnLewisScraper>();
        sc.AddSingleton<ArgosScraper>();
        sc.AddSingleton<BootsScraper>();
        sc.AddSingleton<CurrysScraper>();
        sc.AddSingleton<OverclockersScraper>();
        sc.AddSingleton<SaversScraper>();
        sc.AddSingleton<BargainFoxScraper>();

        // Anti scraping measures unable to bypass.
        // sc.AddSingleton<SmythsScraper>();
        // Does not provide GTIN/EAN only MPN which is not useful.
        // sc.AddSingleton<EBuyerScraper>();
        foreach (ServiceDescriptor scraper in sc
            .Where(x => !x.IsKeyedService)
            .Where(x => x?.ImplementationType?.GetInterfaces()?.Any(x => x == typeof(IScraper)) ?? false)
            .ToList())
        {
            var implementationName = scraper.ImplementationType.Name;

            ScrapeService ScrapeProvider(IServiceProvider sp, object _)
            {
                IScraper scraperImplementation = sp.GetRequiredService(scraper.ImplementationType) as IScraper;
                IFbaDbContext dbContext = sp.GetRequiredService<IFbaDbContext>();
                ILogger<ScrapeService> logger = sp.GetRequiredService<ILogger<ScrapeService>>();

                var scrapeService = new ScrapeService(scraperImplementation, dbContext, logger);
                ScrapeAmazonService scrapeAmazonService = sp.GetRequiredKeyedService<ScrapeAmazonService>(implementationName);
                scrapeService.PostProcess += scrapeAmazonService.Process;

                return scrapeService;
            }

            ScrapeAmazonService ScrapeAmazonProvider(IServiceProvider sp, object _)
            {
                IFbaDbContext dbContext = sp.GetRequiredService<IFbaDbContext>();
                IBatchWorker<AmazonProduct> amazonBatch = sp.GetRequiredService<IBatchWorker<AmazonProduct>>();
                ILogger<ScrapeAmazonService> logger = sp.GetRequiredService<ILogger<ScrapeAmazonService>>();

                var amazonService = new ScrapeAmazonService(dbContext, amazonBatch, logger);
                ScrapeSellerAmpService scrapeSellerAmpService = sp.GetRequiredKeyedService<ScrapeSellerAmpService>(implementationName);
                amazonService.PostProcessed += scrapeSellerAmpService.Process;

                return amazonService;
            }

            ScrapeSellerAmpService ScrapeSellerAmpProvider(IServiceProvider sp, object _)
            {
                IFbaDbContext dbContext = sp.GetRequiredService<IFbaDbContext>();
                IBatchWorker<SellerAmpData> sellerAmpBatch = sp.GetRequiredService<IBatchWorker<SellerAmpData>>();
                ILogger<ScrapeAmazonService> logger = sp.GetRequiredService<ILogger<ScrapeAmazonService>>();
                ScrapeSellerAmpService sellerAmpService = new(dbContext, sellerAmpBatch, logger);

                return sellerAmpService;
            }

            sc.AddKeyedSingleton(implementationName, ScrapeProvider);
            sc.AddKeyedSingleton(implementationName, ScrapeAmazonProvider);
            sc.AddKeyedSingleton(implementationName, ScrapeSellerAmpProvider);
            sc.AddSingleton<ScrapeService>(sp => sp.GetRequiredKeyedService<ScrapeService>(implementationName));
        }

        return sc;
    }
}
