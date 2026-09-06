using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;

namespace FrostWoodTech.API.Interfaces;

/// <summary>
/// The only place that knows the Neon Object Storage credentials. Image bytes never reach the
/// API — this hands out presigned upload URLs and deletes objects that no row points at any more.
/// </summary>
public interface IMediaService
{
    /// <summary>Presigns a direct-to-Neon-Object-Storage PUT for the folder the target implies.</summary>
    ServiceResult<PresignedUploadResponse> CreatePresignedUpload(PresignedUploadRequest request);

    /// <summary>
    /// Builds the public delivery URL for an object key. Pure string building, no request leaves
    /// the process — this is the one place that knows how a key maps to a URL, so a storage
    /// provider change only touches this method.
    /// </summary>
    string GetPublicUrl(string objectKey);

    /// <summary>
    /// The delivery URL prefix with no object key appended — what a <c>media://</c> token's
    /// "media://" needs replaced with to become a loadable URL. Exposed to the admin SPA via
    /// <c>GetMediaConfig</c> so it can render tokens it round-trips without a per-image resolve.
    /// </summary>
    string GetPublicBaseUrl();

    /// <summary>
    /// Deletes the object behind a key. Returns false rather than throwing when Neon Object
    /// Storage is unreachable — an orphaned object must never fail the request that removed its
    /// row. An object that is already gone counts as success.
    /// </summary>
    Task<bool> DeleteFileAsync(string objectKey, CancellationToken cancellationToken);
}
