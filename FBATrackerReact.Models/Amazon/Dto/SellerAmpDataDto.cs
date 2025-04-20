// <copyright file="SellerAmpDataDto.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Models.Amazon.Dto;

public class SellerAmpDataDto(SellerAmpData sellerAmpData)
{
    public int? SalesRank { get; set; } = sellerAmpData.SalesRank;

    public int? ProductsInCategory { get; set; } = sellerAmpData.ProductsInCategory;

    public string EstimatedSales { get; set; } = sellerAmpData.EstimatedSales;

    public int? PrivateLabel { get; set; } = sellerAmpData.PrivateLabel;

    public string PrivateLabelMessage { get; set; } = sellerAmpData.PrivateLabelMessage;

    public int? IntellectualProperty { get; set; } = sellerAmpData.IntellectualProperty;

    public string IntellectualPropertyMessage { get; set; } = sellerAmpData.IntellectualPropertyMessage;

    public bool? Oversized { get; set; } = sellerAmpData.Oversized;

    public bool? BuyBox { get; set; } = sellerAmpData.BuyBox;

    public decimal? BestSellingRate
    {
        get => this.SalesRank < 0 || !this.SalesRank.HasValue || !this.ProductsInCategory.HasValue ? null : decimal.Divide(this.SalesRank.Value, this.ProductsInCategory.Value) * 100;
    }
}