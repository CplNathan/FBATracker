// <copyright file="SellerAmpWorker.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Services;

using System.Text.Json.Nodes;
using AngleSharp;
using FBATrackerReact.Server.Constants;
using FBATrackerReact.Server.Models.Configuration;
using Flurl.Http;

public class SellerAmpWorker(IFlurlClient client, [FromKeyedServices("SellerAmp")] ConfiguredService serviceConfiguration) : BackgroundService
{
    public IFlurlRequest BaseRequest { get => this.CookieSession.Request("https://sas.selleramp.com/"); }

    public string CsrfToken { get; private set; } = string.Empty;

    public string ApiToken { get; private set; } = string.Empty;

    public int ApiUser { get; private set; } = 0;

    protected CookieSession CookieSession { get; } = new CookieSession(client);

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        var stringStreamGet = await this.BaseRequest.AppendPathSegments("site", "login")
            .GetStreamAsync(cancellationToken: token);

        using var browsingContext = BrowsingContext.New(AngleSharp.Configuration.Default);
        using var getPageDocument = await browsingContext.OpenAsync(req => req.Content(stringStreamGet, true), cancel: token);

        var csrfNode = getPageDocument.QuerySelector("meta[name=\"csrf-token\"]");
        this.CsrfToken = csrfNode.GetAttribute("content");

        var stringStreamPost = await (await this.BaseRequest.AppendPathSegments("site", "login")
            .WithAutoRedirect(true)
            .PostUrlEncodedAsync(
                new Dictionary<string, string>
                {
                    { "_csrf-sasend", this.CsrfToken },
                    { "LoginForm[email]", serviceConfiguration.Username },
                    { "LoginForm[password]", serviceConfiguration.Password },
                },
                cancellationToken: token)).GetStreamAsync();

        using var postPageDocument = await browsingContext.OpenAsync(req => req.Content(stringStreamPost, true), cancel: token);

        csrfNode = postPageDocument.QuerySelector("meta[name=\"csrf-token\"]");
        this.CsrfToken = csrfNode.GetAttribute("content");

        var apiNode = postPageDocument.QuerySelectorAll("head > script");
        JsonNode apiData = JsonNode.Parse(RegexConstants.SellerAmp.ScriptExtractRegex().Match(apiNode.Select(x => x.TextContent).Single(RegexConstants.SellerAmp.ScriptExtractRegex().IsMatch)).Groups[1].Value);
        this.ApiToken = apiData?["api_token"]?.GetValue<string>();
        this.ApiUser = (int)apiData?["id"]?.GetValue<int>();
    }
}
