// <copyright file="ScrapeService.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Services;

using FBATrackerReact.Models.Product;
using FBATrackerReact.Models.Scrape;
using FBATrackerReact.Server.Helpers.Database;
using FBATrackerReact.Server.Models.EventHandler;
using FBATrackerReact.Server.Services.Database;
using FBATrackerReact.SharedScraper.Models;

public class ScrapeService(IScraper scraper, IFbaDbContext dbContext, ILogger<ScrapeService> logger)
{
    private Task currentScrape = Task.CompletedTask;

    public event AsyncEventHandler<(bool IsResumed, Guid JobId)> PostProcess;

    public string Source { get => scraper.Source; }

    public bool CanStart
    {
        get
        {
            var hasIncomplete = dbContext.ScrapeJobs
                .Where(x => x.Source == scraper.Source)
                .Where(x => x.Abandoned == false)
                .Where(x => x.Ended == default)
                .Any();

            return !hasIncomplete && (this.currentScrape.IsCompleted || this.currentScrape.IsFaulted);
        }
    }

    public bool IsScrapeDue
    {
        get
        {
            var startedTime = dbContext.ScrapeJobs
                .Where(x => x.Source == scraper.Source)
                .Where(x => x.Abandoned == false)
                .OrderByDescending(x => x.Started)
                .Select(x => x.Started)
                .FirstOrDefault();

            return (DateTime.UtcNow - (startedTime == default ? DateTime.MinValue : startedTime)) > TimeSpan.FromDays(1);
        }
    }

    public async Task Scrape(CancellationToken token)
    {
        this.currentScrape = (this.currentScrape.IsCompleted || this.currentScrape.IsFaulted) ? Task.Run(() => this.ScrapeImplementation(token), token) : this.currentScrape;

        await this.currentScrape;
    }

    public async Task ScrapeImplementation(CancellationToken token)
    {
        var scraperSource = scraper.Source;
        logger.LogInformation("Scrape job starting for {scraperSource}", scraperSource);

        ScrapeJobSet scrapeJob = new()
        {
            Id = Guid.NewGuid(),
            Source = scraper.Source,
            Started = DateTime.UtcNow,
        };

        await dbContext.ScrapeJobs.AddAsync(scrapeJob, token);

        var currentScraper = scraper.Scrape(token);
        using (var timedDbContext = TimedDataFactory<ScrapedProduct>.CreateInstance(dbContext, TimeSpan.FromSeconds(1)))
        {
            await foreach (ScrapedProduct currentProduct in currentScraper.ReadAllAsync(token))
            {
                timedDbContext.Add(currentProduct);
                scrapeJob.Products.Add(currentProduct);
            }
        }

        dbContext.ScrapeJobs.Entry(scrapeJob).Reload();

        if (scrapeJob.Products.Count == 0)
        {
            logger.LogWarning("Scrape ended for {scraperSource} but no products were found", scraperSource);
            scrapeJob.Abandoned = true;
            dbContext.ScrapeJobs.Update(scrapeJob);
        }
        else
        {
            scrapeJob.StartedPostProcessing = DateTime.UtcNow;
            dbContext.SaveChanges();

            await this.PostProcess(this, (false, scrapeJob.Id), token);
        }

        dbContext.ScrapeJobs.Entry(scrapeJob).Reload();
        scrapeJob.Ended = DateTime.UtcNow;
        dbContext.ScrapeJobs.Update(scrapeJob);
        dbContext.SaveChanges();

        logger.LogInformation("Scrape ended for {scraperSource}", scraperSource);
    }

    public async ValueTask<bool> ResumePostProcessing(CancellationToken token)
    {
        var lastIncompleteProcess = dbContext
            .IncompletedJobs
            .Where(x => x.Source == scraper.Source)
            .Where(x => x.StartedPostProcessing != default)
            .OrderByDescending(x => x.Started)
            .SingleOrDefault();

        if (lastIncompleteProcess != null)
        {
            logger.LogInformation("Scrape job resuming for {scraperSource}", lastIncompleteProcess.Source);

            await this.PostProcess(this, (true, lastIncompleteProcess.Id), token);

            dbContext.ScrapeJobs.Entry(lastIncompleteProcess).Reload();
            lastIncompleteProcess.Ended = DateTime.UtcNow;
            dbContext.ScrapeJobs.Update(lastIncompleteProcess);
            dbContext.SaveChanges();

            return true;
        }

        return false;
    }
}