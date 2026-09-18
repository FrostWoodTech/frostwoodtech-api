namespace FrostWoodTech.API.DTOs.Public;

/// <summary>Anonymous submission; always lands unpublished.</summary>
public class CreateReviewRequest
{
    public string? Name { get; set; }

    public string? Country { get; set; }

    /// <summary>ISO 3166-1 alpha-2.</summary>
    public string? CountryCode { get; set; }

    public string? Position { get; set; }

    public int Rating { get; set; }

    public string? ReviewText { get; set; }
}
