// <copyright file="ScrapedProductReference.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Models.Product;

using FBATrackerReact.Models.Enum;

public class ScrapedProductReference
{
    public string Reference { get; set; }

    public ProductReferenceType Type { get; set; }
}
