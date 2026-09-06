using FrostWoodTech.API.Entities.Common;

namespace FrostWoodTech.API.Entities;

/// <summary>
/// Articles have no dedicated page — they render on one list page. An article may still link
/// out to Medium, but its body can also live here as Markdown.
/// </summary>
public class Article : SiteVisibleEntity
{
    public required string Title { get; set; }

    public required string Excerpt { get; set; }

    /// <summary>Stamped the first time the article is published; never cleared.</summary>
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>
    /// Raw Markdown body. Embedded media is referenced with storage-independent
    /// <c>media://articles/...</c> tokens, never a real URL — see <see cref="Interfaces.IArticleMediaResolver"/>.
    /// </summary>
    public string? ContentMarkdown { get; set; }

    /// <summary>Neon object key.</summary>
    public string? CoverImageKey { get; set; }

    /// <summary>Optional, for internal linking.</summary>
    public string? Slug { get; set; }

    public bool IsPublished { get; set; }

    public ICollection<ArticleTag> ArticleTags { get; set; } = [];
}
