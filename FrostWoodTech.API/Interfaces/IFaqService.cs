using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Interfaces;

public interface IFaqService
{
    /// <summary>
    /// Public read: always scoped to one site and to published, non-deleted rows. There is no
    /// overload that lets a caller skip those filters.
    /// </summary>
    Task<IReadOnlyList<FaqResponse>> GetPublicFaqsAsync(
        Site site,
        CancellationToken cancellationToken);

    Task<PagedResult<AdminFaqResponse>> GetAdminFaqsAsync(
        Site? site,
        bool? isPublished,
        string? search,
        Guid? serviceId,
        bool globalOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminFaqResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<AdminFaqResponse>> CreateAsync(
        CreateFaqRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminFaqResponse>> UpdateAsync(
        Guid id,
        UpdateFaqRequest request,
        CancellationToken cancellationToken);

    /// <summary>Soft delete.</summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Bulk sort_order update — FAQs share one order across both sites.</summary>
    Task<ServiceResult<bool>> ReorderAsync(FaqReorderRequest request, CancellationToken cancellationToken);
}
