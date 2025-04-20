// <copyright file="CustomLoggerProvider.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Services.Logging
{
    public class CustomLoggerProvider() : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new PreviewAppLogger();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}