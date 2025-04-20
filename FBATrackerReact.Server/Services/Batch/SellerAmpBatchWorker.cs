// <copyright file="SellerAmpBatchWorker.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Services.Batch;

using System.Text.Json;
using System.Text.Json.Nodes;
using FBATrackerReact.Models.Amazon;
using FBATrackerReact.Server.Models.ConcurrentQueue;
using FBATrackerReact.Server.Services;
using FBATrackerReact.Server.Services.Database;
using Flurl.Http;

public sealed class SellerAmpBatchWorker(SellerAmpWorker sellerAmpTokens, ILogger<IBatchWorker<SellerAmpData>> logger) : BaseBatchWorker<SellerAmpData>(logger, (requestItems, token) => GetSellerAmpDataImplementation(logger, sellerAmpTokens, requestItems, token), TimeSpan.MinValue, 20, true)
{
    private static async Task<List<BatchQueueItem<SellerAmpData>>> GetSellerAmpDataImplementation(ILogger<IBatchWorker<SellerAmpData>> logger, SellerAmpWorker sellerAmpTokens, IEnumerable<BatchQueueItem<SellerAmpData>> productRequests, CancellationToken token)
    {
        List<BatchQueueItem<SellerAmpData>> requestTasks = [];
        foreach (var request in productRequests)
        {
            var requestResult = await RetryPolicy.ExecuteAsync(() => GetKpr(logger, sellerAmpTokens, request, token));
            requestTasks.Add(requestResult);
        }

        return requestTasks;
    }

    private static async Task<BatchQueueItem<SellerAmpData>> GetKpr(ILogger<IBatchWorker<SellerAmpData>> logger, SellerAmpWorker sellerAmpTokens, BatchQueueItem<SellerAmpData> item, CancellationToken token)
    {
        var itemReference = item.ItemReference;
        logger.LogDebug("Fetching seller amp data for {itemReference}", itemReference);

        try
        {
            JsonObject requestData = new()
            {
                { "u", $"{sellerAmpTokens.ApiUser}" },
                { "api_token", sellerAmpTokens.ApiToken },
                { "asin", item.ItemReference },
                { "sl_asin", item.ItemReference },
                { "action", "506" },
                {
                    "payload",
                    new JsonObject()
                    {
                        { "keepa_mp_id", "2" },
                        { "asin_list", new JsonArray(item.ItemReference) },
                    }
                },
            };

            IFlurlResponse requestResult = await sellerAmpTokens.BaseRequest.AppendPathSegments("api", "do")
                .WithHeader("content-type", "application/x-www-form-urlencoded; charset=UTF-8")
                .WithHeader("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Safari/537.36 Edg/127.0.0.0")
                .WithHeader("x-csrf-token", sellerAmpTokens.CsrfToken)
                .WithHeader("x-requested-with", "XMLHttpRequest")
                .PostStringAsync($"data={JsonSerializer.Serialize(requestData)}", cancellationToken: token);

            var requestJson = await requestResult.GetJsonAsync<JsonNode>();
            if (requestJson?["success"]?.GetValue<bool>() ?? false)
            {
                JsonNode kplData = requestJson?["kpls"]?[item.ItemReference];

                if (kplData != default)
                {
                    int? productsInCategory = default;
                    string estimatedSales = default;
                    if (int.TryParse(kplData?["current"]?["3"]?.ToString(), out int salesRank) && salesRank > 0)
                    {
                        productsInCategory = kplData?["cat_product_count"]?.GetValue<int>();
                        estimatedSales = kplData?["estimated_sales"]?["monthly"]?.ToString();
                    }

                    item.ItemData = new SellerAmpData
                    {
                        SalesRank = salesRank,
                        ProductsInCategory = productsInCategory,
                        EstimatedSales = estimatedSales,
                        IntellectualProperty = kplData?["ip_issue"]?.GetValue<int>(),
                        IntellectualPropertyMessage = kplData?["ip_issue_message"]?.ToString(),
                        Oversized = kplData?["oversize"]?.GetValue<bool>(),
                        PrivateLabel = kplData?["private_label"]?.GetValue<int>(),
                        PrivateLabelMessage = kplData?["private_label_message"]?.ToString(),
                        BuyBox = (kplData?["bb_type"]?.GetValue<int>() ?? -1) >= 0,
                    };
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling SellerSAS with reference {itemReference}", itemReference);
        }

        return item;
    }
}