using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Interfaces;

public interface IReviewService
{
    /// <summary>Anonymous; rate limited per IP; lands unpublished.</summary>
    Task<ServiceResult<ReviewSubmissionResponse>> SubmitAsync(
        DTOs.Public.CreateReviewRequest request,
        string? ipAddress,
        CancellationToken cancellationToken);

    /// <summary>Published, non-deleted rows only.</summary>
    Task<PagedResult<ReviewResponse>> GetPublicReviewsAsync(
        ReviewSortOption sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ReviewResponse>> GetFeaturedForHomeAsync(int take, CancellationToken cancellationToken);

    Task<PagedResult<AdminReviewResponse>> GetAdminReviewsAsync(
        bool? isPublished,
        bool? isFeatured,
        string? country,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminReviewResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<AdminReviewResponse>> CreateAsync(
        DTOs.Admin.CreateReviewRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminReviewResponse>> UpdateAsync(
        Guid id,
        UpdateReviewRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<bool>> ReorderAsync(ReviewReorderRequest request, CancellationToken cancellationToken);
}
