using FrostWoodTech.API.DTOs.Public;

namespace FrostWoodTech.API.DTOs.Admin;

public sealed class AdminProjectResponse
{
    public required Guid Id { get; init; }

    public required string Slug { get; init; }

    public required string Title { get; init; }

    public required int Year { get; init; }

    public required string ShortDescription { get; init; }

    public required string Description { get; init; }

    public string? WebsiteUrl { get; init; }

    public string? Problem { get; init; }

    public string? Solution { get; init; }

    public string? WhatWeDelivered { get; init; }

    public string? Proof { get; init; }

    public string? ClientName { get; init; }

    public required bool IsPublished { get; init; }

    public DateTimeOffset? PublishedAt { get; init; }

    public string? SeoTitle { get; init; }

    public string? SeoDescription { get; init; }

    public required bool ShowOnAgency { get; init; }

    public required bool FeaturedOnAgency { get; init; }

    public required int AgencySortOrder { get; init; }

    public required bool ShowOnPersonal { get; init; }

    public required bool FeaturedOnPersonal { get; init; }

    public required int PersonalSortOrder { get; init; }

    public required IReadOnlyList<AdminTagResponse> Tags { get; init; }

    public required IReadOnlyList<ProjectImageResponse> Images { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
