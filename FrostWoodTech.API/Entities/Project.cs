using FrostWoodTech.API.Entities.Common;

namespace FrostWoodTech.API.Entities;

public class Project : SiteVisibleEntity
{
    /// <summary>From the title. Editable, but stable once published — changing it breaks live links.</summary>
    public required string Slug { get; set; }

    public required string Title { get; set; }

    public int Year { get; set; }

    /// <summary>Card / list blurb.</summary>
    public required string ShortDescription { get; set; }

    /// <summary>Markdown, long form.</summary>
    public required string Description { get; set; }

    public string? WebsiteUrl { get; set; }

    /// <summary>Markdown.</summary>
    public string? Problem { get; set; }

    /// <summary>Markdown.</summary>
    public string? Solution { get; set; }

    /// <summary>Markdown.</summary>
    public string? WhatWeDelivered { get; set; }

    /// <summary>Metrics / results, markdown.</summary>
    public string? Proof { get; set; }

    public string? ClientName { get; set; }

    public bool IsPublished { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public string? SeoTitle { get; set; }

    public string? SeoDescription { get; set; }

    public ICollection<ProjectImage> Images { get; set; } = [];

    public ICollection<ProjectTag> ProjectTags { get; set; } = [];

    /// <summary>Services that show this project as a case study.</summary>
    public ICollection<ServiceProject> ServiceProjects { get; set; } = [];
}
