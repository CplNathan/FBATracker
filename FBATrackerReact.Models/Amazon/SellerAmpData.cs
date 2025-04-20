// <copyright file="SellerAmpData.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Models.Amazon;

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

public class SellerAmpData
{
    public virtual AmazonProduct Owner { get; set; }

    public int? SalesRank { get; set; }

    public int? ProductsInCategory { get; set; }

    public string EstimatedSales { get; set; }

    public int? PrivateLabel { get; set; }

    public string PrivateLabelMessage { get; set; }

    public int? IntellectualProperty { get; set; }

    public string IntellectualPropertyMessage { get; set; }

    public bool? Oversized { get; set; }

    public bool? BuyBox { get; set; }
}
