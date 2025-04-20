// <copyright file="Configuration.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Models.Configuration;

using FikaAmazonAPI;

public class Configuration
{
    public AmazonCredential AmazonCredentials { get; init; }

    public Dictionary<string, string> ConfiguredHosts { get; init; }

    public Dictionary<string, ConfiguredService> ConfiguredServices { get; init; }
}