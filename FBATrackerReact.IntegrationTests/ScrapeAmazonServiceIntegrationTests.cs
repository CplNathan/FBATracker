namespace FBATrackerReact.IntegrationTests;

using FBATrackerReact.Models.Amazon;
using FBATrackerReact.Models.Product;
using FBATrackerReact.Models.Scrape;
using FBATrackerReact.Server.Models.ConcurrentQueue;
using FBATrackerReact.Server.Services;
using FBATrackerReact.Server.Services.Batch;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit.Abstractions;

public sealed class ScrapeAmazonServiceIntegrationTests : BaseServiceIntegrationTests
{
    private readonly IBatchWorker<AmazonProduct> amazonWorker;

    private readonly ScrapeAmazonService sut;

    public ScrapeAmazonServiceIntegrationTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
        this.amazonWorker = Substitute.For<IBatchWorker<AmazonProduct>>();
        this.amazonWorker.BatchSize.Returns(20);

        this.sut = new ScrapeAmazonService(this.DbContext, this.amazonWorker, Substitute.For<ILogger<ScrapeAmazonService>>());
    }

    [Fact]
    public async Task ScrapeService_Scrape_UpdatesExistingRows()
    {
        // Arrange
        var amazonProduct = new AmazonProduct()
        {
            AmazonStandardIdentificationNumber = "BP123",
            Name = "Test Product",
            ProductPricing = new AmazonPricing()
        };

        amazonProduct.ProductPricing = new AmazonPricing()
        {
            Owner = amazonProduct,
            Fees = 1,
            HighestPrice = 30,
            LowestPreferredPrice = 20,
        };

        this.DbContext.AmazonProducts.Add(amazonProduct);
        this.DbContext.AmazonProducts.Entry(amazonProduct).Reload();

        var scrapedProduct = new ScrapedProduct()
        {
            AmazonProduct = amazonProduct,
            ProductReference = new ScrapedProductReference() { Type = Models.Enum.ProductReferenceType.GTIN, Reference = "1234" },
            Price = 2,
        };

        this.DbContext.ScrapedProducts.Add(scrapedProduct);
        this.DbContext.ScrapedProducts.Entry(scrapedProduct).Reload();

        var scrapeJob = new ScrapeJobSet()
        {
            Products = [scrapedProduct],
        };

        this.DbContext.ScrapeJobs.Add(scrapeJob);
        this.DbContext.ScrapeJobs.Entry(scrapeJob).Reload();

        this.amazonWorker.EnqueueItemsAsync(Arg.Any<IEnumerable<BatchQueueItem<AmazonProduct>>>(), CancellationToken.None)
            .Returns((_) =>
            {
                var productPricing = amazonProduct.ProductPricing;
                productPricing.LowestPreferredPrice = 40;

                List<BatchQueueItem<AmazonProduct>> amazonBatch = [new BatchQueueItem<AmazonProduct>(amazonProduct.AmazonStandardIdentificationNumber, amazonProduct.Name) {
                    ItemData = new AmazonProduct() {
                        AmazonStandardIdentificationNumber = amazonProduct.AmazonStandardIdentificationNumber,
                        Name = amazonProduct.Name,
                        ProductPricing = productPricing
                    }
                }];

                return amazonBatch.ToAsyncEnumerable();
            });

        // Act
        this.DbContext.SaveChanges();

        await this.sut.ProcessImplementation(false, scrapeJob.Id, CancellationToken.None);

        // Assert
        var amazonProductOutput = this.DbContext.AmazonProducts.Single(x => x.AmazonStandardIdentificationNumber == amazonProduct.AmazonStandardIdentificationNumber);
        amazonProductOutput.ProductPricing.LowestPreferredPrice.Should().Be(40);
        amazonProductOutput.ReferencedProducts.First().Profit.Should().Be(37);
        // this.DbContext.AmazonProducts.Received().Update(amazonProduct);
    }

    [Fact]
    public async Task ScrapeService_Scrape_AddNewRows()
    {
        // Arrange
        var amazonProduct = new AmazonProduct()
        {
            AmazonStandardIdentificationNumber = "BP123",
            Name = "Test Product",
            ProductPricing = new AmazonPricing()
        };

        amazonProduct.ProductPricing = new AmazonPricing()
        {
            Owner = amazonProduct,
            Fees = 1,
            HighestPrice = 30,
            LowestPreferredPrice = 20,
        };

        this.amazonWorker.EnqueueItemsAsync(Arg.Any<IEnumerable<BatchQueueItem<AmazonProduct>>>(), CancellationToken.None)
            .Returns((_) =>
            {
                var productPricing = amazonProduct.ProductPricing;
                productPricing.LowestPreferredPrice = 40;

                List<BatchQueueItem<AmazonProduct>> amazonBatch = [new BatchQueueItem<AmazonProduct>(amazonProduct.AmazonStandardIdentificationNumber, amazonProduct.Name) {
                    ItemData = new AmazonProduct() {
                        AmazonStandardIdentificationNumber = amazonProduct.AmazonStandardIdentificationNumber,
                        Name = amazonProduct.Name,
                        ProductPricing = productPricing
                    }
                }];

                return amazonBatch.ToAsyncEnumerable();
            });

        var scrapedProduct = new ScrapedProduct()
        {
            AmazonProduct = amazonProduct,
            ProductReference = new ScrapedProductReference() { Type = Models.Enum.ProductReferenceType.GTIN, Reference = "1234" },
            Price = 2,
        };

        this.DbContext.ScrapedProducts.Add(scrapedProduct);
        this.DbContext.ScrapedProducts.Entry(scrapedProduct).Reload();

        var scrapeJob = new ScrapeJobSet()
        {
            Products = [scrapedProduct],
        };

        this.DbContext.ScrapeJobs.Add(scrapeJob);
        this.DbContext.ScrapeJobs.Entry(scrapeJob).Reload();

        // Act
        this.DbContext.SaveChanges();

        await this.sut.ProcessImplementation(false, scrapeJob.Id, CancellationToken.None);

        // Assert
        var amazonProductOutput = this.DbContext.AmazonProducts.Single(x => x.AmazonStandardIdentificationNumber == amazonProduct.AmazonStandardIdentificationNumber);
        amazonProductOutput.ProductPricing.LowestPreferredPrice.Should().Be(40);
        amazonProductOutput.ReferencedProducts.First().Profit.Should().Be(37);
        // this.DbContext.AmazonProducts.Received().Add(amazonProduct);
    }
}
