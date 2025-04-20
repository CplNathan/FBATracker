// <copyright file="TimedDataFactory.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Helpers.Database;

using System.Collections.Concurrent;
using FBATrackerReact.Server.Services.Database;

public class TimedDataFactory<TType> : IDisposable
    where TType : class
{
    private readonly IFbaDbContext dbContext;

    private readonly TimeSpan saveInterval;

    private readonly ConcurrentBag<TType> pendingItems = [];

    private DateTime lastSaved = DateTime.UtcNow;

    internal TimedDataFactory(IFbaDbContext dbContext, TimeSpan saveInterval)
    {
        this.dbContext = dbContext;
        this.saveInterval = saveInterval;
    }

    private TimeSpan LastSaved => (DateTime.UtcNow - this.lastSaved).Duration();

    public static TimedDataFactory<TType> CreateInstance(IFbaDbContext dbContext, TimeSpan saveInterval)
        => new(dbContext, saveInterval);

    public void Add(TType value)
    {
        this.pendingItems.Add(value);

        if (this.LastSaved > this.saveInterval)
        {
            this.SaveItems();
        }
    }

    public void Dispose()
    {
        this.SaveItems();
        GC.SuppressFinalize(this);
    }

    private void SaveItems()
    {
        if (!this.pendingItems.IsEmpty)
        {
            this.dbContext.Set<TType>().AddRange(this.pendingItems);
            this.dbContext.SaveChanges();
            this.pendingItems.Clear();
        }

        this.lastSaved = DateTime.UtcNow;
    }
}
