// <copyright file="IFbaDbContext.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Services.Database;

using FBATrackerReact.Models.Amazon;
using FBATrackerReact.Models.Product;
using FBATrackerReact.Models.Scrape;
using Microsoft.EntityFrameworkCore;

public interface IFbaDbContext
{
    public DbSet<ScrapeJobSet> ScrapeJobs { get; init; }

    public DbSet<ScrapedProduct> ScrapedProducts { get; init; }

    public DbSet<AmazonProduct> AmazonProducts { get; init; }

    public IQueryable<ScrapeJobSet> CompletedJobs { get; }

    public IQueryable<ScrapeJobSet> IncompletedJobs { get; }

    public int SaveChanges();

    public DbSet<TEntity> Set<TEntity>()
        where TEntity : class;
}
