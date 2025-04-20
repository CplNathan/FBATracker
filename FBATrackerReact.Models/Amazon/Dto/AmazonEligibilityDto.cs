// <copyright file="AmazonEligibilityDto.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Models.Amazon.Dto;

public class AmazonEligibilityDto(AmazonEligibility amazonEligibility)
{
    public bool IsGateLocked { get; set; } = amazonEligibility.IsGateLocked;

    public bool IsRestricted { get; set; } = amazonEligibility.IsRestricted;

    public List<string> RestrictedReason { get; set; } = amazonEligibility.RestrictedReason;

    public string GatedOnwardUrl { get; set; } = amazonEligibility.GatedOnwardUrl;
}
