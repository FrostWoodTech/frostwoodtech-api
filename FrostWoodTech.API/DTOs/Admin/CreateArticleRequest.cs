namespace FrostWoodTech.API.DTOs.Admin;

public class CreateArticleRequest
{
    public string? Title { get; set; }

    public string? Excerpt { get; set; }

    /// <summary>Generated from the title when omitted.</summary>
    public string? Slug { get; set; }

    public string? CoverImageKey { get; set; }

    /// <summary>Markdown; embedded media uses media://articles/... tokens.</summary>
    public string? ContentMarkdown { get; set; }

    public bool IsPublished { get; set; }

    public bool ShowOnAgency { get; set; }

    public bool FeaturedOnAgency { get; set; }

    public bool ShowOnPersonal { get; set; }

    public bool FeaturedOnPersonal { get; set; }

    /// <summary>The full tag set; omitted or empty means none.</summary>
    public List<Guid>? TagIds { get; set; }
}
