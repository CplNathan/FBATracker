// <copyright file="ProductVisibilityDto.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Models.Product.Dto;

using FBATrackerReact.Models.Enum;

public class ProductVisibilityDto(ProductVisibility productVisibility)
{
    public ProductListType ProductList { get; set; } = productVisibility.ProductList;

    public bool IsWatchlisted { get => this.ProductList == ProductListType.Watched; }
}
