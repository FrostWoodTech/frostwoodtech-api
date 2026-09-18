using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Interfaces;

public interface IFaqService
{
    /// <summary>Always filtered to one site and published, non-deleted, global FAQs.</summary>
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

    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<bool>> ReorderAsync(FaqReorderRequest request, CancellationToken cancellationToken);
}
