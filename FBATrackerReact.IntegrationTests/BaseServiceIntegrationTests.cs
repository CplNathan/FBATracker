namespace FBATrackerReact.IntegrationTests;

using FBATrackerReact.Server.Services.Database;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Testcontainers.PostgreSql;
using Testcontainers.Xunit;
using NSubstitute;

public abstract class BaseServiceIntegrationTests(ITestOutputHelper testOutputHelper) : ContainerTest<PostgreSqlBuilder, PostgreSqlContainer>(testOutputHelper), IAsyncLifetime
{
    private FbaDbContext? fbaDbContext = null;

    protected string ConnectionString
    {
        get => this.Container.GetConnectionString();
    }

    protected IFbaDbContext DbContext
    {
        get
        {
            var dbContext = Substitute.For<IFbaDbContext>();
            dbContext.AmazonProducts.Returns(x => this.fbaDbContext!.AmazonProducts);
            dbContext.ScrapedProducts.Returns(x => this.fbaDbContext!.ScrapedProducts);
            dbContext.ScrapeJobs.Returns(x => this.fbaDbContext!.ScrapeJobs);
            dbContext.SaveChanges().Returns(x => this.fbaDbContext!.SaveChanges());

            return dbContext;
        }
    }

    protected override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        this.fbaDbContext = new(new DbContextOptionsBuilder<FbaDbContext>().UseNpgsql(this.ConnectionString).Options);
        await this.fbaDbContext.Database.MigrateAsync();
    }
}
