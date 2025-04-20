// <copyright file="ScrapeStatusDto.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Models;

public class ScrapeStatusDto
{
    public string Source { get; set; }

    public int ScrapedProducts { get; set; }

    public int AmazonScrapedProducts { get; set; }

    public int SellerAmpScrapedProducts { get; set; }

    public DateTime ScrapeStarted { get; set; }

    public DateTime? ScrapeEnded { get; set; }

    public double ElapsedSeconds { get => ((this.ScrapeEnded ?? DateTime.UtcNow) - this.ScrapeStarted).TotalSeconds; }
}