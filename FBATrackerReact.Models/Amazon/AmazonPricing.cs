// <copyright file="AmazonPricing.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Models.Amazon;

using System.Text.Json.Serialization;

public class AmazonPricing
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public virtual AmazonProduct Owner { get; set; }

    public decimal? LowestPreferredPrice { get; set; }

    public decimal? HighestPrice { get; set; }

    public decimal? Fees { get; set; }
}
