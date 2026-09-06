namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>
/// Lets the admin SPA turn a round-tripped <c>media://</c> token back into a loadable URL itself,
/// without a per-image resolve request. Not sensitive — the bucket is already publicly readable.
/// </summary>
public sealed class MediaConfigResponse
{
    /// <summary>Prefix a token's key with this (in place of "media://") to get a loadable URL.</summary>
    public required string PublicBaseUrl { get; init; }
}
