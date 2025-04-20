// <copyright file="ScrapeWorker.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Services;

using System.Threading;
using FBATrackerReact.Server.Services.Database;
using Microsoft.EntityFrameworkCore;

public class ScrapeWorker(IFbaDbContext dbContext, IEnumerable<ScrapeService> scrapeServices, ILogger<ScrapeWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.StartupClean();
        await this.ScrapeLoop(stoppingToken);
    }

    private void StartupClean()
    {
        var jobsToKeep = dbContext
            .IncompletedJobs
            .OrderByDescending(x => x.Started)
            .GroupBy(x => x.Source)
            .Select(x => x.FirstOrDefault(y => y.StartedPostProcessing != default).Id)
            .Where(x => x != default);

        var cleanedCount = dbContext
            .IncompletedJobs
            .Where(x => !jobsToKeep.Contains(x.Id))
            .ExecuteUpdate(setter => setter.SetProperty(job => job.Abandoned, true));

        this.CleanDatabase();

        logger.LogInformation("Scraper worker finished cleaning {cleanedCount} abandoned jobs", cleanedCount);
    }

    private async Task ScrapeLoop(CancellationToken token)
    {
        logger.LogInformation("Scraper worker entering worker loop");

        await this.ScrapeAll(true, token);

        while (!token.IsCancellationRequested)
        {
            TimeSpan timeUntilExecution = (DateTime.UtcNow.TimeOfDay - TimeSpan.FromDays(1)).Duration();

            await Task.Delay(timeUntilExecution, token);

            this.CleanDatabase();

            await this.ScrapeAll(false, token);
        }

        logger.LogCritical("Scraper worker loop stopped");
    }

    private async Task ScrapeAll(bool resumePartial, CancellationToken token)
    {
        List<Task> scrapeTasks = [];
        foreach (var scraper in scrapeServices)
        {
            scrapeTasks.Add(Task.Run(
                async () =>
                {
                    var resumedScrape = resumePartial && await scraper.ResumePostProcessing(token);

                    if (!resumedScrape && scraper.IsScrapeDue)
                    {
                        await scraper.Scrape(token);
                    }
                },
                token));
        }

        await Task.WhenAll(scrapeTasks);
    }

    private void CleanDatabase()
    {
        logger.LogInformation("Optimizing database");

        try
        {
            var expiredJobs = dbContext
                .ScrapeJobs
                .Where(x => x.Abandoned == true || DateTime.UtcNow - x.Started > TimeSpan.FromDays(7));

            var anyExpired = expiredJobs.Any();
            if (anyExpired)
            {
                expiredJobs
                    .SelectMany(x => x.Products)
                    .ExecuteDelete();

                expiredJobs
                    .ExecuteDelete();

                dbContext.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database optimization failed");
        }
    }
}