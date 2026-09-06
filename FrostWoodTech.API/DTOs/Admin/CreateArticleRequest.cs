namespace FrostWoodTech.API.DTOs.Admin;

public class CreateArticleRequest
{
    public string? Title { get; set; }

    public string? Excerpt { get; set; }

    /// <summary>Optional — generated from the title when omitted.</summary>
    public string? Slug { get; set; }

    public string? CoverImageKey { get; set; }

    /// <summary>Raw Markdown, with embedded media as <c>media://articles/...</c> references.</summary>
    public string? ContentMarkdown { get; set; }

    public bool IsPublished { get; set; }

    public bool ShowOnAgency { get; set; }

    public bool FeaturedOnAgency { get; set; }

    public bool ShowOnPersonal { get; set; }

    public bool FeaturedOnPersonal { get; set; }

    /// <summary>The full set of tags for the article — omitted or empty means none.</summary>
    public List<Guid>? TagIds { get; set; }
}
