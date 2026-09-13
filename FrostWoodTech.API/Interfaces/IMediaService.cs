using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;

namespace FrostWoodTech.API.Interfaces;

public interface IMediaService
{
    ServiceResult<PresignedUploadResponse> CreatePresignedUpload(PresignedUploadRequest request);

    string GetPublicUrl(string objectKey);

    /// <summary>URL prefix that replaces "media://" in a token.</summary>
    string GetPublicBaseUrl();

    /// <summary>Returns false instead of throwing; an already-missing object counts as success.</summary>
    Task<bool> DeleteFileAsync(string objectKey, CancellationToken cancellationToken);
}
