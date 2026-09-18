using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>Requests a presigned PUT URL; the SPA uploads straight to storage.</summary>
public class PresignedUploadRequest
{
    /// <summary>Picks the folder; the client never names one.</summary>
    public MediaTarget? Target { get; set; }

    /// <summary>Required for projects, products and articles.</summary>
    public string? Slug { get; set; }

    /// <summary>Optional: overwrite this exact object instead of creating a new one.</summary>
    public string? ObjectKey { get; set; }
}
