namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>Metadata for a file already uploaded to storage; bytes never pass through the API.</summary>
public class AddProductImageRequest
{
    public string? ObjectKey { get; set; }

    public string? Url { get; set; }

    public string? AltText { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    /// <summary>Setting this clears the previous primary.</summary>
    public bool IsPrimary { get; set; }

    public int SortOrder { get; set; }
}
