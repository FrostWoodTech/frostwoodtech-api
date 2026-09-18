namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>Lets the SPA resolve media:// tokens itself. Not sensitive: the bucket is public-read.</summary>
public sealed class MediaConfigResponse
{
    public required string PublicBaseUrl { get; init; }
}
