namespace FrostWoodTech.API.DTOs.Public;

/// <summary>
/// What the two public frontends see. The site's own visibility flags are already resolved into
/// <see cref="Featured"/> and <see cref="SortOrder"/> — the other site's flags, the draft state
/// and the audit metadata never cross this boundary.
/// </summary>
public sealed record ArticleResponse
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required string Excerpt { get; init; }

    public string? Slug { get; init; }

    /// <summary>When the article first went live. Null only for a row published before this field existed.</summary>
    public DateTimeOffset? PublishedAt { get; init; }

    /// <summary>Last edit — what the frontend falls back to when <see cref="PublishedAt"/> is null.</summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>Neon object key — the frontend builds the delivery URL.</summary>
    public string? CoverImageKey { get; init; }

    /// <summary>
    /// Markdown body with every <c>media://...</c> reference already resolved to a real URL —
    /// ready to hand to a Markdown renderer as-is.
    /// </summary>
    public string? ContentMarkdown { get; init; }

    /// <summary>Featured on the requested site.</summary>
    public required bool Featured { get; init; }

    /// <summary>Sort order for the requested site.</summary>
    public required int SortOrder { get; init; }

    public required IReadOnlyList<TagResponse> Tags { get; init; }
}
