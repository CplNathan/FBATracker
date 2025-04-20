// <copyright file="RequestHelperService.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.SharedScraper.Services;

using System.Collections.Concurrent;
using System.Text;
using FBATrackerReact.SharedScraper.Models;
using FBATrackerReact.SharedScraper.Models.RequestHelper;
using FlareSolverrSharp.Solvers;
using Flurl.Http;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;

public class RequestHelperService(IFlurlClient client, [FromKeyedServices("Proxy")] IFlurlClient proxyClient, FlareSolverr flareSolverr, [FromKeyedServices("Proxy")] FlareSolverr proxyFlareSolverr) : IRequestHelperService
{
    protected static AsyncRetryPolicy RetryPolicy { get => Policy.Handle<FlurlHttpException>().RetryAsync(2); }

    protected ConcurrentDictionary<Type, (SemaphoreSlim Semaphore, CookieJar Cookies)> CookiePot { get; } = new ConcurrentDictionary<Type, (SemaphoreSlim, CookieJar)>();

    protected ConcurrentDictionary<Type, string> UserAgentPot { get; } = new ConcurrentDictionary<Type, string>();

    protected ConcurrentDictionary<Type, Task<string>> FlaresolverrPot { get; } = new ConcurrentDictionary<Type, Task<string>>();

    private static string DefaultUserAgent => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36";

    public Task<Stream> PerformGet<TType>(BaseRequestContext request, CancellationToken token)
        where TType : IScraper
    {
        return request switch
        {
            DefaultContext defaultContext => this.PerformDefault(defaultContext, HttpMethod.Get, typeof(TType), token),
            FlaresolverrContext flaresolverrContext => this.PerformFlaresolverr(flaresolverrContext, typeof(TType), token),
            _ => throw new NotImplementedException($"Request handler does not exist for {request.GetType()}"),
        };
    }

    public Task<Stream> PerformPost<TType>(BaseRequestContext request, CancellationToken token)
        where TType : IScraper
    {
        return request switch
        {
            DefaultContext defaultContext => this.PerformDefault(defaultContext, HttpMethod.Post, typeof(TType), token),
            _ => throw new NotImplementedException($"Request handler does not exist for {request.GetType()}"),
        };
    }

    public async Task Clear(Type instance)
    {
        try
        {
            this.CookiePot.Remove(instance, out _);
            this.FlaresolverrPot.Remove(instance, out var sessionKey);

            var sessionKeyResult = await (sessionKey ?? Task.FromResult(string.Empty));
            if (!string.IsNullOrWhiteSpace(sessionKeyResult))
            {
                await flareSolverr.DestroySession(sessionKeyResult);
            }
        }
        catch
        {
        }
    }

    private async Task<Stream> PerformDefault(DefaultContext context, HttpMethod requestMethod, Type key, CancellationToken token)
    {
        var cookiePot = this.CookiePot.GetOrAdd(key, key => (new SemaphoreSlim(1), new CookieJar()));
        var userAgentPot = this.UserAgentPot.GetOrAdd(key, key => DefaultUserAgent);
        var request = context.Request;

        request = request.WithHeader("User-Agent", userAgentPot);
        if (context.UseHeaders)
        {
            string[] referer = ["http://www.google.com/", "https://www.bing.com", "https://www.yahoo.com"];

            request = request
                .WithHeader("Referer", referer.ElementAt(Random.Shared.Next(0, referer.Length - 1)))
                .WithHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8")
                .WithHeader("Accept-Language", "en-US,en;q=0.5")
                .WithHeader("Accept-Encoding", "gzip, deflate, br")
                .WithHeader("Connection", "keep-alive")
                .WithHeader("Upgrade-Insecure-Requests", "1")
                .WithHeader("Sec-Fetch-Dest", "document")
                .WithHeader("Sec-Fetch-Mode", "navigate")
                .WithHeader("Sec-Fetch-Site", "none")
                .WithHeader("Sec-Fetch-User", "?1")
                .WithHeader("Cache-Control", "max-age=0");
        }

        request = request.WithCookies(cookiePot.Cookies.ToList());
        request.Client = context.UseProxy ? proxyClient : client;

        var result = await RetryPolicy.ExecuteAsync(async () =>
            requestMethod.Method switch
            {
                "POST" =>await (await request.PostStringAsync(context.Body, cancellationToken: token)).GetStreamAsync(),
                "GET" => await request.GetStreamAsync(cancellationToken: token),
                _ => throw new InvalidOperationException($"Request handler does not support {requestMethod.Method}"),
            });

        return result;
    }

    private async Task<Stream> PerformFlaresolverr(FlaresolverrContext context, Type key, CancellationToken token)
    {
        var client = context.UseProxy ? proxyFlareSolverr : flareSolverr;
        var uri = context.Request.Url.ToUri();

        var flaresolverrPot = await this.FlaresolverrPot.GetOrAdd(key, key => client.CreateSession().ContinueWith(session => session.Result.Session));
        var cookiePot = this.CookiePot.GetOrAdd(key, key => (new SemaphoreSlim(1), new CookieJar()));
        var solvedResult = await client.Solve(new HttpRequestMessage(HttpMethod.Get, uri), flaresolverrPot);
        token.ThrowIfCancellationRequested();

        this.UserAgentPot.AddOrUpdate(key, key => solvedResult.Solution.UserAgent, (key, existing) => solvedResult.Solution.UserAgent);

        await cookiePot.Semaphore.WaitAsync(token);
        foreach (var cookie in solvedResult.Solution.Cookies)
        {
            cookiePot.Cookies.AddOrReplace(cookie.Name, cookie.Value, uri.AbsoluteUri);
        }

        cookiePot.Semaphore.Release();

        return new MemoryStream(Encoding.UTF8.GetBytes(solvedResult.Solution.Response));
    }
}