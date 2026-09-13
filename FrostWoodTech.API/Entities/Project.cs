using FrostWoodTech.API.Entities.Common;

namespace FrostWoodTech.API.Entities;

public class Project : SiteVisibleEntity
{
    /// <summary>Stable once published; changing it breaks live links.</summary>
    public required string Slug { get; set; }

    public required string Title { get; set; }

    public int Year { get; set; }

    public required string ShortDescription { get; set; }

    public required string Description { get; set; }

    public string? WebsiteUrl { get; set; }

    public string? Problem { get; set; }

    public string? Solution { get; set; }

    public string? WhatWeDelivered { get; set; }

    public string? Proof { get; set; }

    public string? ClientName { get; set; }

    public bool IsPublished { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public string? SeoTitle { get; set; }

    public string? SeoDescription { get; set; }

    public ICollection<ProjectImage> Images { get; set; } = [];

    public ICollection<ProjectTag> ProjectTags { get; set; } = [];

    public ICollection<ServiceProject> ServiceProjects { get; set; } = [];
}
