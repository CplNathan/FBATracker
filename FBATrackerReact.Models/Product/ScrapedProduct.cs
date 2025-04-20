// <copyright file="ScrapedProduct.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Models.Product;

using FBATrackerReact.Models.Amazon;
using FBATrackerReact.Models.Scrape;

public class ScrapedProduct
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public virtual ScrapeJobSet Owner { get; set; }

    public virtual AmazonProduct AmazonProduct { get; set; }

    public string Source { get; set; }

    public string Name { get; set; }

    public decimal Price { get; set; }

    public virtual ScrapedProductReference ProductReference { get; set; }

    public string Url { get; set; }

    public decimal? Profit
    {
        get
        {
            AmazonPricing latestPricing = this.AmazonProduct?.ProductPricing;

            return latestPricing != default ? latestPricing?.LowestPreferredPrice - latestPricing?.Fees - this.Price : default;
        }

        private set
        {
        }
    }
}
