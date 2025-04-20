// <copyright file="DefaultContext.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.SharedScraper.Models.RequestHelper;

public class DefaultContext : BaseRequestContext
{
    public bool UseHeaders { get; set; } = true;
}
