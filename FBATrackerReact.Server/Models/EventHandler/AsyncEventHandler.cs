// <copyright file="AsyncEventHandler.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Models.EventHandler
{
    public delegate Task AsyncEventHandler<TType>(object sender, TType args, CancellationToken token);
}
