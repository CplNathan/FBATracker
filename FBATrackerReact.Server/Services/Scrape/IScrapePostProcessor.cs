// <copyright file="IScrapePostProcessor.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Services.Scrape;

public interface IScrapePostProcessor
{
    Task Process(object sender, (bool IsResumed, Guid JobId) data, CancellationToken token);
}