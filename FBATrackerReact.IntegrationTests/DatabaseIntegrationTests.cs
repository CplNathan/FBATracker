namespace FBATrackerReact.IntegrationTests;

using FBATrackerReact.Server.Services.Database;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Testcontainers.Xunit;
using Xunit.Abstractions;

public class DatabaseIntegrationTests(ITestOutputHelper testOutputHelper) : ContainerTest<PostgreSqlBuilder, PostgreSqlContainer>(testOutputHelper)
{
    protected override PostgreSqlBuilder Configure(PostgreSqlBuilder builder)
    {
        return builder;
    }

    [Fact]
    public async Task FbaDbContext_Migrate_CreatesTables()
    {
        // Arrange
        var connectionString = this.Container.GetConnectionString();
        var dbContext = new FbaDbContext(new DbContextOptionsBuilder<FbaDbContext>().UseNpgsql(connectionString).Options);

        // Act
        var migrateAction = () => dbContext.Database.MigrateAsync();

        // Assert
        await migrateAction.Should().NotThrowAsync();
        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();
        appliedMigrations.Count().Should().BeGreaterThan(0);
    }
}