namespace FrostWoodTech.API.DTOs.Admin;

public class CreateProductRequest
{
    public string? Name { get; set; }

    /// <summary>Generated from the name when omitted.</summary>
    public string? Slug { get; set; }

    public string? Tagline { get; set; }

    public string? Description { get; set; }

    public string? PriceDetails { get; set; }

    public string? ProductUrl { get; set; }

    public bool IsPublished { get; set; }

    public string? SeoTitle { get; set; }

    public string? SeoDescription { get; set; }

    public bool ShowOnAgency { get; set; }

    public bool FeaturedOnAgency { get; set; }

    public bool ShowOnPersonal { get; set; }

    public bool FeaturedOnPersonal { get; set; }
}
