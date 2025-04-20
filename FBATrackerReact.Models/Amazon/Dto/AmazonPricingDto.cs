// <copyright file="AmazonPricingDto.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Models.Amazon.Dto;

public class AmazonPricingDto(AmazonPricing amazonPricing)
{
    public decimal? LowestPreferredPrice { get; set; } = amazonPricing.LowestPreferredPrice;

    public decimal? HighestPrice { get; set; } = amazonPricing.HighestPrice;

    public decimal? Fees { get; set; } = amazonPricing.Fees;
}
