using FrostWoodTech.API.Entities.Common;

namespace FrostWoodTech.API.Entities;

public class Article : SiteVisibleEntity
{
    public required string Title { get; set; }

    public required string Excerpt { get; set; }

    /// <summary>Stamped on first publish; never cleared.</summary>
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>Markdown; media uses media://articles/... tokens, never real URLs.</summary>
    public string? ContentMarkdown { get; set; }

    public string? CoverImageKey { get; set; }

    public string? Slug { get; set; }

    public bool IsPublished { get; set; }

    public ICollection<ArticleTag> ArticleTags { get; set; } = [];
}
