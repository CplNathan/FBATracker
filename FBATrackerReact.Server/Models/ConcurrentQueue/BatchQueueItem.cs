// <copyright file="BatchQueueItem.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Models.ConcurrentQueue;

using System.Threading.Channels;

public class BatchQueueItem<TType>(string itemReference, string itemName, string requestedBy = "*")
    where TType : new()
{
    public string ItemReference { get; internal set; } = itemReference;

    public string ItemName { get; internal set; } = itemName;

    public TType ItemData { get; set; }

    public string RequestedBy { get; internal set; } = requestedBy;

    public object OptionalData { get; set; }

    internal RequestQueueInternal InternalBatch { get; private set; }

    internal BatchQueueItem<TType> SetChannel(Channel<BatchQueueItem<TType>> channel, Guid batchId)
    {
        this.InternalBatch = new RequestQueueInternal(channel, batchId);
        return this;
    }

    internal class RequestQueueInternal(Channel<BatchQueueItem<TType>> channel, Guid batchId)
    {
        public Channel<BatchQueueItem<TType>> Channel { get; private set; } = channel;

        public Guid BatchId { get; private set; } = batchId;
    }
}
