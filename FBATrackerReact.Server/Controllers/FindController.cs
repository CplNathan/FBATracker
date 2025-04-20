// <copyright file="FindController.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Controllers;

using FBATrackerReact.Server.Services.Search;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("/api/find/")]
public class FindController(CatalogSearchService searchService, PricingSearchService pricingService) : Controller
{
    [HttpGet("itemfromidentifier")]
    [ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any, VaryByQueryKeys = ["*"], NoStore = false)]
    public async Task<IActionResult> ItemFromIdentifier(string productEan, CancellationToken token)
    {
        return this.Json(await searchService.FindProductFromEan(productEan, token));
    }

    [HttpGet("pricingfromidentifier")]
    [ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any, VaryByQueryKeys = ["*"], NoStore = false)]
    public async Task<IActionResult> PricingFromIdentifier(string productAsin, CancellationToken token)
    {
        return this.Json(await pricingService.GetPricing(productAsin, token));
    }
}