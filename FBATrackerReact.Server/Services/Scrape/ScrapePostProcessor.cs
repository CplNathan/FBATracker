// <copyright file="ScrapePostProcessor.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Services.Scrape;

using FBATrackerReact.Server.Models.EventHandler;

public abstract class ScrapePostProcessor(ILogger logger)
{
    private CancellationTokenSource cancellationTokenSource = new();

    private Task currentTask = Task.CompletedTask;

    public event AsyncEventHandler<(bool IsResumed, Guid JobId)> PostProcessed;

    public async Task Process(object sender, (bool IsResumed, Guid JobId) data, CancellationToken token)
    {
        try
        {
            this.cancellationTokenSource.Cancel();

            try
            {
                await this.currentTask;
            }
            catch (TaskCanceledException)
            {
            }

            this.currentTask = Task.Run(
                async () =>
                {
                    this.cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

                    await this.ProcessImplementation(data.IsResumed, data.JobId, this.cancellationTokenSource.Token);

                    if (!this.cancellationTokenSource.IsCancellationRequested)
                    {
                        await (this.PostProcessed?.Invoke(this, data, token) ?? Task.CompletedTask);
                    }
                },
                token);

            await this.currentTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing scrape post processor");
        }
    }

    public abstract Task ProcessImplementation(bool isResumed, Guid jobId, CancellationToken token);
}
