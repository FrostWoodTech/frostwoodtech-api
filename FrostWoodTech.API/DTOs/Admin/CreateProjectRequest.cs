namespace FrostWoodTech.API.DTOs.Admin;

public class CreateProjectRequest
{
    public string? Title { get; set; }

    /// <summary>Optional — generated from the title when omitted.</summary>
    public string? Slug { get; set; }

    public int Year { get; set; }

    /// <summary>Card / list blurb.</summary>
    public string? ShortDescription { get; set; }

    /// <summary>Markdown, long form.</summary>
    public string? Description { get; set; }

    /// <summary>Optional, but must be an absolute URL when given.</summary>
    public string? WebsiteUrl { get; set; }

    public string? Problem { get; set; }

    public string? Solution { get; set; }

    public string? WhatWeDelivered { get; set; }

    public string? Proof { get; set; }

    public string? ClientName { get; set; }

    public bool IsPublished { get; set; }

    public string? SeoTitle { get; set; }

    public string? SeoDescription { get; set; }

    public bool ShowOnAgency { get; set; }

    public bool FeaturedOnAgency { get; set; }

    public bool ShowOnPersonal { get; set; }

    public bool FeaturedOnPersonal { get; set; }

    /// <summary>The full set of tags for the project — omitted or empty means none.</summary>
    public List<Guid>? TagIds { get; set; }
}
