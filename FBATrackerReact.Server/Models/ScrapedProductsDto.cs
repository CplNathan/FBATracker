// <copyright file="ScrapedProductsDto.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Models;

using FBATrackerReact.Models.Amazon.Dto;
using FBATrackerReact.Models.Product.Dto;

public class ScrapedProductsDto
{
    public Dictionary<string, AmazonProductDto> AmazonProducts { get; set; }

    public Dictionary<string, IEnumerable<ScrapedProductDto>> ScrapedProducts { get; set; }
}