namespace FrostWoodTech.API.DTOs.Admin;

public sealed class PresignedUploadResponse
{
    public required string UploadUrl { get; init; }

    /// <summary>Send this back with the image metadata request.</summary>
    public required string ObjectKey { get; init; }

    public required string PublicUrl { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }
}
