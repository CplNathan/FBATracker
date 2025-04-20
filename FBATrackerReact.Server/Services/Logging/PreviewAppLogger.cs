// <copyright file="PreviewAppLogger.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Services.Logging;

using System.Collections.ObjectModel;

public class PreviewAppLogger : ILogger
{
    private static readonly List<PreviewLog> Logs = [];

    public static ReadOnlyCollection<PreviewLog> LatestLogs { get => Logs.AsReadOnly(); }

    IDisposable ILogger.BeginScope<TState>(TState state)
    {
        return default;
    }

    bool ILogger.IsEnabled(LogLevel logLevel)
    {
        return logLevel == LogLevel.Information;
    }

    void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        var logMessage = formatter(state, exception);

        Logs.Add(new PreviewLog(DateTime.UtcNow, logMessage));
    }
}