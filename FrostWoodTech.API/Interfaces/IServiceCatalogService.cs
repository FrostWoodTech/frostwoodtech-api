using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;

namespace FrostWoodTech.API.Interfaces;

public interface IServiceCatalogService
{
    /// <summary>Always filtered to published, non-deleted rows.</summary>
    Task<PagedResult<ServiceResponse>> GetPublicServicesAsync(
        bool? featured,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ServiceResult<ServiceResponse>> GetPublicServiceBySlugAsync(
        string slug,
        CancellationToken cancellationToken);

    Task<PagedResult<AdminServiceResponse>> GetAdminServicesAsync(
        bool? isPublished,
        string? search,
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

    /// <summary>First publish stamps published_at; republishing keeps it.</summary>
    Task<ServiceResult<AdminServiceResponse>> SetPublishedAsync(
        Guid id,
        SetPublishedRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<TrashedItemResponse>> GetTrashAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>404 unless the row is in the trash.</summary>
    Task<ServiceResult<AdminServiceResponse>> RestoreAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Super admin only; stored files are deleted after the commit.</summary>
    Task<ServiceResult<bool>> PurgeAsync(Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<bool>> ReorderAsync(ReorderRequest request, CancellationToken cancellationToken);
}
