// <copyright file="AmazonProductDto.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Models.Amazon.Dto;

using FBATrackerReact.Models.Amazon;
using FBATrackerReact.Models.Product.Dto;

public class AmazonProductDto(AmazonProduct amazonProduct)
{
    public string AmazonStandardIdentificationNumber { get; set; } = amazonProduct.AmazonStandardIdentificationNumber;

    public string Url { get => $"https://amazon.co.uk/dp/{this.AmazonStandardIdentificationNumber}"; }

    public string Name { get; set; } = amazonProduct.Name;

    public ProductVisibilityDto ProductVisibility { get; set; } = amazonProduct.ProductVisibility != default ? new ProductVisibilityDto(amazonProduct.ProductVisibility) : null;

    public AmazonEligibilityDto ProductEligibility { get; set; } = amazonProduct.ProductEligibility != default ? new AmazonEligibilityDto(amazonProduct.ProductEligibility) : null;

    public SellerAmpDataDto SellerAmpData { get; set; } = amazonProduct.SellerAmpData != null ? new SellerAmpDataDto(amazonProduct.SellerAmpData) : null;

    public AmazonPricingDto ProductPricing { get; set; } = amazonProduct.ProductPricing != default ? new AmazonPricingDto(amazonProduct.ProductPricing) : null;
}
