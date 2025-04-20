// <copyright file="ScrapeExtensions.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Extensions;

using System.Text.RegularExpressions;
using FBATrackerReact.Models.Product;

public static class ScrapeExtensions
{
    private const string RegexPattern = $"{"Pack of \\d+"}|{"\\d+.Pack"}";

    public static IQueryable<ScrapedProduct> ExcludeBadListings(this IQueryable<ScrapedProduct> query)
    {
        return query
            .Where(x => x.ProductReference != default)
            .Where(x => !string.IsNullOrWhiteSpace(x.ProductReference.Reference))
            .Where(x => x.AmazonProduct != default)
            .Where(x => !string.IsNullOrWhiteSpace(x.AmazonProduct.Name))
            .Where(x => !string.IsNullOrWhiteSpace(x.AmazonProduct.AmazonStandardIdentificationNumber))
            .Where(x => !Regex.IsMatch(x.AmazonProduct.Name, RegexPattern, RegexOptions.IgnoreCase));
    }
}
