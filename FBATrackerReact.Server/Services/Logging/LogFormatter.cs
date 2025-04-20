// <copyright file="LogFormatter.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Services.Logging
{
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Logging.Console;
    using Microsoft.Extensions.Options;

    public sealed class LogFormatter : ConsoleFormatter, IDisposable
    {
        private readonly string defaultColor = "\x1b[39m";

        private readonly IDisposable optionsReloadToken;

        private ConsoleFormatterOptions formatterOptions;

        public LogFormatter(
            IOptionsMonitor<ConsoleFormatterOptions> options)
            : base(nameof(LogFormatter))
        {
            this.optionsReloadToken = options.OnChange(this.ReloadLoggerOptions);
            this.formatterOptions = options.CurrentValue;
        }

        public void Dispose() => this.optionsReloadToken?.Dispose();

        public override void Write<TState>(
            in LogEntry<TState> logEntry,
            IExternalScopeProvider scopeProvider,
            TextWriter textWriter)
        {
            string message =
                logEntry.Formatter(
                    logEntry.State, logEntry.Exception);

            if (message == default)
            {
                return;
            }

            this.WritePrefix(textWriter, logEntry.Category, logEntry.LogLevel);
            textWriter.Write(message);
            this.WriteSuffix(textWriter, logEntry.LogLevel);
            if (logEntry.Exception != default)
            {
                textWriter.WriteLine(logEntry.Exception.ToString());
            }
        }

        private static string GetColor(LogLevel level)
        {
            return Console.IsOutputRedirected ? string.Empty : level switch
            {
                LogLevel.Warning => "\x1b[93m",
                LogLevel.Debug => "\x1b[96m",
                LogLevel.Error or LogLevel.Critical => "\x1b[91m",
                LogLevel.Information or _ => string.Empty,
            };
        }

        private void ReloadLoggerOptions(ConsoleFormatterOptions options)
            => this.formatterOptions = options;

        private void WritePrefix(TextWriter textWriter, string category, LogLevel level)
        {
            DateTime now = this.formatterOptions.UseUtcTimestamp
                ? DateTime.UtcNow
                : DateTime.Now;

            var logColor = GetColor(level);

            textWriter.Write($"{logColor}{"|-<["}{this.defaultColor} ");
            textWriter.Write($"{now.ToString(this.formatterOptions.TimestampFormat)} {category} - ");
        }

        private void WriteSuffix(TextWriter textWriter, LogLevel level)
        {
            var logColor = GetColor(level);

            textWriter.WriteLine($" {logColor}{"]>-|"}{this.defaultColor}");
        }
    }
}