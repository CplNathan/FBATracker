// <copyright file="BaseRequestContext.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.SharedScraper.Models.RequestHelper;

using Flurl.Http;

public abstract class BaseRequestContext
{
    public required IFlurlRequest Request { get; set; }

    public string? Body { get; set; }

    public bool UseProxy { get; set; }
}
