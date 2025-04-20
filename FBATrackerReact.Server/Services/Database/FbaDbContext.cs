// <copyright file="FbaDbContext.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Services.Database;

using FBATrackerReact.Models.Amazon;
using FBATrackerReact.Models.Enum;
using FBATrackerReact.Models.Product;
using FBATrackerReact.Models.Scrape;
using Microsoft.EntityFrameworkCore;

public class FbaDbContext(DbContextOptions options) : DbContext(options), IFbaDbContext
{
    public DbSet<ScrapeJobSet> ScrapeJobs { get; init; }

    public DbSet<ScrapedProduct> ScrapedProducts { get; init; }

    public DbSet<AmazonProduct> AmazonProducts { get; init; }

    public IQueryable<ScrapeJobSet> CompletedJobs
    {
        get
        {
            return this.ScrapeJobs
                .Where(x => x.Abandoned == false)
                .Where(x => x.Ended != default);
        }
    }

    public IQueryable<ScrapeJobSet> IncompletedJobs
    {
        get
        {
            return this.ScrapeJobs
                .Where(x => x.Abandoned == false)
                .Except(this.CompletedJobs);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ScrapeJobSet>(b =>
        {
            b.ToTable("scrapes");
            b.HasMany(x => x.Products)
                .WithOne(x => x.Owner);
            b.HasIndex(x => x.Source);
            b.HasIndex(x => x.Started);
            b.HasIndex(x => x.Ended);
            b.HasIndex(x => x.Abandoned);
        });

        modelBuilder.Entity<ScrapedProduct>(b =>
        {
            b.ToTable("scraped_products");
            b.HasOne(x => x.AmazonProduct)
                .WithMany(x => x.ReferencedProducts);
            b.OwnsOne(x => x.ProductReference)
                .HasIndex(x => x.Reference);
            b.HasIndex(x => x.Profit);
        });

        modelBuilder.Entity<AmazonProduct>(b =>
        {
            b.ToTable("amazon_products");

            b.HasKey(x => x.AmazonStandardIdentificationNumber);

            b.OwnsOne(x => x.SellerAmpData)
                .ToTable("amazon_amp")
                .WithOwner(x => x.Owner);

            b.OwnsOne(x => x.ProductEligibility)
                .ToTable("amazon_eligibility")
                .WithOwner(x => x.Owner);

            b.OwnsOne(x => x.ProductPricing)
                .ToTable("amazon_pricing")
                .WithOwner(x => x.Owner);

            var v = b.OwnsOne(x => x.ProductVisibility)
                .ToTable("amazon_visibility");
            v.Property(x => x.ProductList)
                .HasDefaultValue(ProductListType.New);
            v.WithOwner(x => x.Owner);
        });
    }
}