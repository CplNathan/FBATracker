// <copyright file="ScrapeJobSet.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Models.Scrape;

using System.Text.Json.Serialization;
using FBATrackerReact.Models.Product;

public class ScrapeJobSet
{
    [JsonIgnore]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Source { get; set; }

    public virtual List<ScrapedProduct> Products { get; set; } = [];

    public DateTime Started { get; set; }

    public DateTime? StartedPostProcessing { get; set; }

    public DateTime? Ended { get; set; }

    public bool Abandoned { get; set; }
}
