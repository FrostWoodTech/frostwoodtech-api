namespace FrostWoodTech.API.DTOs.Admin;

public sealed class AdminArticleResponse
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required string Excerpt { get; init; }

    public string? Slug { get; init; }

    public DateTimeOffset? PublishedAt { get; init; }

    public string? CoverImageKey { get; init; }

    /// <summary>Raw Markdown with media:// tokens unresolved, for the editor.</summary>
    public string? ContentMarkdown { get; init; }

    public required bool IsPublished { get; init; }

    public required bool ShowOnAgency { get; init; }

    public required bool FeaturedOnAgency { get; init; }

    public required int AgencySortOrder { get; init; }

    public required bool ShowOnPersonal { get; init; }

    public required bool FeaturedOnPersonal { get; init; }

    public required int PersonalSortOrder { get; init; }

    public required IReadOnlyList<AdminTagResponse> Tags { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
