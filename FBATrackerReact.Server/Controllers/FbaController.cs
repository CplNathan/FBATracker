// <copyright file="FbaController.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Controllers;

using FBATrackerReact.Models.Amazon;
using FBATrackerReact.Server.Services.Database;
using FBATrackerReact.Server.Services.Search;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("/api/fba/")]
public class FbaController(IFbaDbContext dbContext, FbaSearchService fbaService) : Controller
{
    [HttpGet("fees")]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, VaryByQueryKeys = ["*"], NoStore = false)]
    public async Task<IActionResult> GetFees(string productAsin, decimal priceGbp, CancellationToken token)
    {
        return this.Json(await fbaService.GetFees(productAsin, priceGbp, token));
    }

    [HttpGet("eligibility")]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, VaryByQueryKeys = ["*"], NoStore = false)]
    public async Task<IActionResult> GetEligibility(string productAsin, CancellationToken token)
    {
        AmazonEligibility eligibility = await fbaService.GetEligibility(productAsin, token);

        Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<AmazonProduct, AmazonEligibility> productFinder = dbContext.AmazonProducts
            .Where(x => x.AmazonStandardIdentificationNumber == productAsin)
            .Include(x => x.ProductEligibility);

        AmazonProduct amazonProduct = await productFinder.FirstOrDefaultAsync(cancellationToken: token);
        if (amazonProduct != default)
        {
            amazonProduct.ProductEligibility = eligibility;
            dbContext.AmazonProducts.Update(amazonProduct);
            dbContext.SaveChanges();
        }

        return this.Json(eligibility);
    }
}