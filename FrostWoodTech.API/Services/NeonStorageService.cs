using Amazon.S3;
using Amazon.S3.Model;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Interfaces;
using FrostWoodTech.API.Media;

namespace FrostWoodTech.API.Services;

/// <summary>Neon Object Storage (S3-compatible). Presigning is local; deleting is a network call.</summary>
public class NeonStorageService : IMediaService
{
    private const int UploadExpiryMinutes = 15;

    private readonly IAmazonS3 _s3;
    private readonly NeonStorageOptions _options;
    private readonly ILogger<NeonStorageService> _logger;

    public NeonStorageService(IAmazonS3 s3, IOptions<NeonStorageOptions> options, ILogger<NeonStorageService> logger)
    {
        _s3 = s3;
        _options = options.Value;
        _logger = logger;
    }

    public ServiceResult<PresignedUploadResponse> CreatePresignedUpload(PresignedUploadRequest request)
    {
        if (!_options.IsConfigured)
        {
            return ServiceResult<PresignedUploadResponse>.Validation(
                "Neon Object Storage is not configured. Set NeonS3__Endpoint, NeonS3__AccessKey, " +
                "NeonS3__SecretKey and NeonS3__BucketName.");
        }

        if (request.Target is not { } target)
        {
            return ServiceResult<PresignedUploadResponse>.Validation(
                "target is required: projects, products, services, tags, articles or certificates.");
        }

        var folderResult = BuildFolder(target, request.Slug);
        if (!folderResult.IsSuccess)
        {
            return ServiceResult<PresignedUploadResponse>.Failure(folderResult.Error!);
        }

        var objectKey = string.IsNullOrWhiteSpace(request.ObjectKey)
            ? $"{folderResult.Value}{Guid.NewGuid():N}"
            : request.ObjectKey.Trim();

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(UploadExpiryMinutes);

        var uploadUrl = _s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = expiresAt.UtcDateTime
        });

        return ServiceResult<PresignedUploadResponse>.Success(new PresignedUploadResponse
        {
            UploadUrl = uploadUrl,
            ObjectKey = objectKey,
            PublicUrl = GetPublicUrl(objectKey),
            ExpiresAt = expiresAt
        });
    }

    public string GetPublicBaseUrl() =>
        $"{_options.Endpoint.TrimEnd('/')}/{_options.BucketName}";

    public string GetPublicUrl(string objectKey) =>
        $"{GetPublicBaseUrl()}/{objectKey.TrimStart('/')}";

    public async Task<bool> DeleteFileAsync(string objectKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(objectKey) || !_options.IsConfigured)
        {
            return false;
        }

        try
        {
            await _s3.DeleteObjectAsync(
                new DeleteObjectRequest { BucketName = _options.BucketName, Key = objectKey },
                cancellationToken);

            // S3 delete is idempotent.
            return true;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogWarning(ex, "Neon Object Storage delete of {ObjectKey} failed.", objectKey);

            return false;
        }
    }

    // Folder is built server-side so uploads can't escape the base folder.
    private ServiceResult<string> BuildFolder(MediaTarget target, string? slug)
    {
        var root = _options.BaseFolder.Trim('/');

        if (target is not (MediaTarget.Projects or MediaTarget.Products or MediaTarget.Articles))
        {
            return ServiceResult<string>.Success($"{root}/{target.ToString().ToLowerInvariant()}/");
        }

        // SlugGenerator strips everything but [a-z0-9-], so "../" can't survive.
        var safeSlug = string.IsNullOrWhiteSpace(slug) ? string.Empty : SlugGenerator.Generate(slug);
        var folderName = target.ToString().ToLowerInvariant();

        return safeSlug.Length == 0
            ? ServiceResult<string>.Validation($"slug is required when target is {folderName}.")
            : ServiceResult<string>.Success($"{root}/{folderName}/{safeSlug}/");
    }
}
