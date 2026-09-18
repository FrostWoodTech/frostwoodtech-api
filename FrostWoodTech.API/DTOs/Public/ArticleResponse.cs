namespace FrostWoodTech.API.DTOs.Public;

/// <summary>Public shape: Featured and SortOrder are for the requested site; no admin fields.</summary>
public sealed record ArticleResponse
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required string Excerpt { get; init; }

    public string? Slug { get; init; }

    public DateTimeOffset? PublishedAt { get; init; }

    /// <summary>Fallback date when PublishedAt is null.</summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    public string? CoverImageKey { get; init; }

    /// <summary>Markdown with media:// tokens already resolved to URLs.</summary>
    public string? ContentMarkdown { get; init; }

    public required bool Featured { get; init; }

    public required int SortOrder { get; init; }

    public required IReadOnlyList<TagResponse> Tags { get; init; }
}
