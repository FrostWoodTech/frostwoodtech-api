using FrostWoodTech.API.Entities.Common;

namespace FrostWoodTech.API.Entities;

public class Product : SiteVisibleEntity
{
    /// <summary>Stable once published; changing it breaks live links.</summary>
    public required string Slug { get; set; }

    public required string Name { get; set; }

    public required string Tagline { get; set; }

    public required string Description { get; set; }

    public string? PriceDetails { get; set; }

    public string? ProductUrl { get; set; }

    public bool IsPublished { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public string? SeoTitle { get; set; }

    public string? SeoDescription { get; set; }

    public ICollection<ProductImage> Images { get; set; } = [];
}
