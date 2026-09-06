using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Interfaces;

/// <summary>The <c>services</c> aggregate.</summary>
public interface IServiceCatalogService
{
    /// <summary>
    /// Public read: always scoped to one site and to published, non-deleted rows. There is no
    /// overload that lets a caller skip those filters.
    /// </summary>
    Task<PagedResult<ServiceResponse>> GetPublicServicesAsync(
        Site site,
        bool? featured,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ServiceResult<ServiceResponse>> GetPublicServiceBySlugAsync(
        Site site,
        string slug,
        CancellationToken cancellationToken);

    Task<PagedResult<AdminServiceResponse>> GetAdminServicesAsync(
        Site? site,
        bool? isPublished,
        string? search,
        bool includeHidden,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminServiceResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<AdminServiceResponse>> CreateAsync(
        CreateServiceRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminServiceResponse>> UpdateAsync(
        Guid id,
        UpdateServiceRequest request,
        CancellationToken cancellationToken);

    /// <summary>Flips the draft flag on its own, stamping published_at the first time it goes live.</summary>
    Task<ServiceResult<AdminServiceResponse>> SetPublishedAsync(
        Guid id,
        SetPublishedRequest request,
        CancellationToken cancellationToken);

    /// <summary>Soft delete.</summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Bulk sort_order update for one site.</summary>
    Task<ServiceResult<bool>> ReorderAsync(ReorderRequest request, CancellationToken cancellationToken);
}
