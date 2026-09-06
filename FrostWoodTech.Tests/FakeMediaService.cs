using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.Tests;

/// <summary>
/// Keeps Neon Object Storage out of the test run. The rules the project tests care about — one
/// primary image, required alt text — are decided before anything is deleted, so recording the
/// calls is enough.
/// </summary>
public sealed class FakeMediaService : IMediaService
{
    public List<string> Deleted { get; } = [];

    public ServiceResult<PresignedUploadResponse> CreatePresignedUpload(PresignedUploadRequest request) =>
        throw new NotSupportedException("Presigning is not exercised by these tests.");

    public string GetPublicUrl(string objectKey) => $"https://fake-storage.test/{objectKey}";

    public string GetPublicBaseUrl() => "https://fake-storage.test";

    public Task<bool> DeleteFileAsync(string objectKey, CancellationToken cancellationToken)
    {
        Deleted.Add(objectKey);

        return Task.FromResult(true);
    }
}
