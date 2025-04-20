// <copyright file="LoggingController.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Controllers;

using FBATrackerReact.Server.Services.Logging;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("/api/logs/")]
public class LoggingController : Controller
{
    [HttpGet("recent")]
    public IActionResult GetRecent()
    {
        return this.Json(PreviewAppLogger.LatestLogs.Reverse());
    }
}