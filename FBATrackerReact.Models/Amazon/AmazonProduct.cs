// <copyright file="AmazonProduct.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Models.Amazon;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using FBATrackerReact.Models.Product;

public class AmazonProduct
{
    [Key]
    public string AmazonStandardIdentificationNumber { get; set; }

    public virtual List<ScrapedProduct> ReferencedProducts { get; set; } = [];

    [NotMapped]
    public string Url { get => $"https://amazon.co.uk/dp/{this.AmazonStandardIdentificationNumber}"; }

    public string Name { get; set; } = "Unknown";

    public ProductVisibility ProductVisibility { get; set; } = new();

    public AmazonEligibility ProductEligibility { get; set; } = new();

    public SellerAmpData SellerAmpData { get; set; } = new();

    public AmazonPricing ProductPricing { get; set; } = new();
}
