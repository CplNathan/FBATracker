// <copyright file="IScraper.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.SharedScraper.Models;

using System.Threading.Channels;
using FBATrackerReact.Models.Product;

public interface IScraper
{
    public string Source { get; }

    public ChannelReader<ScrapedProduct> Scrape(CancellationToken token);
}