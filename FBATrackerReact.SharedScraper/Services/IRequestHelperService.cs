// <copyright file="IRequestHelperService.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.SharedScraper.Services;

using FBATrackerReact.SharedScraper.Models;
using FBATrackerReact.SharedScraper.Models.RequestHelper;

public interface IRequestHelperService
{
    public Task<Stream> PerformGet<TType>(BaseRequestContext request, CancellationToken token)
        where TType : IScraper;

    public Task<Stream> PerformPost<TType>(BaseRequestContext request, CancellationToken token)
        where TType : IScraper;

    public Task Clear(Type instance);
}