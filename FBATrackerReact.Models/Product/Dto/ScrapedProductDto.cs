// <copyright file="ScrapedProductDto.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Models.Product.Dto;

public class ScrapedProductDto(ScrapedProduct scrapedProduct)
{
    public string Source { get; set; } = scrapedProduct.Source;

    public string Url { get; set; } = scrapedProduct.Url;

    public decimal Price { get; set; } = scrapedProduct.Price;

    public decimal? Profit { get; set; } = scrapedProduct.Profit;

    public string AmazonStandardIdentificationNumber { get; set; } = scrapedProduct.AmazonProduct?.AmazonStandardIdentificationNumber;

    public string ProductCode { get; set; } = scrapedProduct.ProductReference?.Reference;
}
