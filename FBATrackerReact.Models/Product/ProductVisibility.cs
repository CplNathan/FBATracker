// <copyright file="ProductVisibility.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Models.Product;

using FBATrackerReact.Models.Amazon;
using FBATrackerReact.Models.Enum;

public class ProductVisibility
{
    public virtual AmazonProduct Owner { get; set; }

    public ProductListType ProductList { get; set; } = ProductListType.New;
}
