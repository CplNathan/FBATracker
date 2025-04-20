namespace FBATrackerReact.IntegrationTests;

using FBATrackerReact.Models.Amazon;
using FBATrackerReact.Models.Product;
using FBATrackerReact.Server.Services;
using FBATrackerReact.SharedScraper.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Testcontainers.PostgreSql;
using Xunit.Abstractions;

public sealed class ScrapeServiceIntegrationTests : BaseServiceIntegrationTests
{
    private readonly ScrapeService sut;

    public ScrapeServiceIntegrationTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
        var scraper = Substitute.For<IScraper>();
        this.sut = new ScrapeService(scraper, this.DbContext, Substitute.For<ILogger<ScrapeService>>());
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
            Price = 2,
        };

        this.DbContext.ScrapedProducts.Add(scrapedProduct);
        this.DbContext.ScrapedProducts.Entry(scrapedProduct).Reload();

        this.DbContext.SaveChanges();

        // Act
        await this.sut.Scrape(CancellationToken.None);

        // Assert
        // TEST NOT FINISHED - WILL FAIL
        // TODO: Setup mock returns from batch worker and test that EF fetch with given ID has correct updated values.
    }
}
