// <copyright file="BaseScraper.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.SharedScraper.Models;

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using AngleSharp;
using AngleSharp.Dom;
using FBATrackerReact.Models.Product;
using FBATrackerReact.SharedScraper.Services;

public abstract class BaseScraper(IRequestHelperService requestHelper) : IScraper, IDisposable, IAsyncDisposable
{
    public abstract string Source { get; }

    protected IRequestHelperService RequestHelper { get; } = requestHelper;

    protected IBrowsingContext HtmlContext { get => this.BackingHtmlContext ?? throw new InvalidOperationException("Tried to access context without a backing context"); }

    protected abstract string BaseUrl { get; }

    private IBrowsingContext? BackingHtmlContext { get; set; } = default;

    private Channel<ScrapedProduct>? ScraperChannel { get; set; } = default;

    private ChannelWriter<ScrapedProduct> ScraperWriter { get => this.ScraperChannel?.Writer ?? throw new InvalidOperationException("Tried to access writer without a channel"); }

    public void Dispose()
    {
        this.BackingHtmlContext?.Dispose();
        this.ScraperChannel?.Writer?.Complete();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await this.RequestHelper.Clear(this.GetType());
        GC.SuppressFinalize(this);
    }

    public ChannelReader<ScrapedProduct> Scrape(CancellationToken token)
    {
        this.BackingHtmlContext = BrowsingContext.New(Configuration.Default);

        this.ScraperChannel = Channel.CreateBounded<ScrapedProduct>(new BoundedChannelOptions(25)
        {
            FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = true,
            SingleWriter = true,
            SingleReader = true,
        });

        Task.Run(
            async () =>
            {
                try
                {
                    await this.ScrapeImplementation(token).ConfigureAwait(false);
                }
                finally
                {
                    this.BackingHtmlContext?.Dispose();
                    this.BackingHtmlContext = default;
                    this.ScraperChannel?.Writer?.Complete();
                    this.ScraperChannel = default;

                    await this.RequestHelper.Clear(this.GetType());
                }
            },
            token);

        return this.ScraperChannel.Reader;
    }

    protected static JsonNode? ExtractScript(IDocument? document, Regex scriptToMatch)
    {
        if (document == default)
        {
            return default;
        }

        var scripts = document.QuerySelectorAll("script");
        var matchedJson = scripts.Select(script => scriptToMatch.Match(script.InnerHtml)).Where(x => x.Success).FirstOrDefault()?.Groups?[1]?.Value;

        return string.IsNullOrWhiteSpace(matchedJson) ? null : JsonNode.Parse(matchedJson.Replace('\n', ' '));
    }

    protected abstract Task ScrapeImplementation(CancellationToken token);

    protected async Task<JsonNode?> ExtractScript(Stream stringStream, Regex scriptToMatch, CancellationToken token)
    {
        using var pageDocument = await this.HtmlContext.OpenAsync(req => req.Content(stringStream, true), cancel: token);

        return ExtractScript(pageDocument, scriptToMatch);
    }

    protected async ValueTask<ScrapedProduct?> CreateItem(string? productName, string? productPrice, string? productLink = default, ScrapedProductReference? productReference = default, Func<ScrapedProduct, Task<ScrapedProduct>>? customFormat = default)
    {
        if (string.IsNullOrWhiteSpace(productName) || string.IsNullOrWhiteSpace(productPrice))
        {
            return default;
        }

        if (decimal.TryParse(productPrice, out decimal price) == false || price == 0)
        {
            return default;
        }

        var product = new ScrapedProduct
        {
            Source = this.Source,
            Name = productName,
            Price = Math.Round(price, 2),
            ProductReference = productReference,
            Url = productLink,
        };

        if (customFormat != default)
        {
            try
            {
                product = await customFormat(product);
            }
            catch
            {
            }
        }

        if (product.ProductReference == default || string.IsNullOrWhiteSpace(product.ProductReference.Reference) || product.ProductReference.Reference == "0")
        {
            return default;
        }

        if (string.IsNullOrWhiteSpace(product.Url) || (product.Url?.Trim('/')?.Equals(this.BaseUrl) ?? false))
        {
            return default;
        }

        return product;
    }

    protected async ValueTask PublishItem(ScrapedProduct? item, CancellationToken token)
    {
        if (item == default)
        {
            return;
        }

        await this.ScraperWriter.WriteAsync(item, token);
    }
}