namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>Admin-entered testimonial; the public submission uses its own DTO.</summary>
public class CreateReviewRequest
{
    public string? Name { get; set; }

    public string? Country { get; set; }

    public string? CountryCode { get; set; }

    public string? Position { get; set; }

    public int Rating { get; set; }

    public string? ReviewText { get; set; }

    public bool IsPublished { get; set; }

    public bool IsFeatured { get; set; }
}
