namespace FrostWoodTech.API.DTOs.Public;

/// <summary>Public shape: Featured and SortOrder are for the requested site; no admin fields.</summary>
public sealed class ProjectResponse
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

    public DateTimeOffset? PublishedAt { get; init; }

    public string? SeoTitle { get; init; }

    public string? SeoDescription { get; init; }

    public required bool Featured { get; init; }

    public required int SortOrder { get; init; }

    public required IReadOnlyList<TagResponse> Tags { get; init; }

    public required IReadOnlyList<ProjectImageResponse> Images { get; init; }
}
