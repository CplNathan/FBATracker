// <copyright file="AmazonEligibility.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Models.Amazon;

using System.Text.Json.Serialization;

public class AmazonEligibility
{
    [JsonIgnore]
    public virtual AmazonProduct Owner { get; set; }

    public bool IsGateLocked { get; set; }

    public bool IsRestricted { get; set; }

    public List<string> RestrictedReason { get; set; }

    public string GatedOnwardUrl { get; set; }
}
