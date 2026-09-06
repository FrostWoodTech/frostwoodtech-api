using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;

namespace FrostWoodTech.API.Interfaces;

public interface ICertificateService
{
    /// <summary>
    /// Public read: always scoped to published, non-deleted rows — there is no site to pick,
    /// certificates only ever exist for the personal site.
    /// </summary>
    Task<PagedResult<CertificateResponse>> GetPublicCertificatesAsync(
        bool? featured,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PagedResult<AdminCertificateResponse>> GetAdminCertificatesAsync(
        bool? isPublished,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminCertificateResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<AdminCertificateResponse>> CreateAsync(
        CreateCertificateRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminCertificateResponse>> UpdateAsync(
        Guid id,
        UpdateCertificateRequest request,
        CancellationToken cancellationToken);

    /// <summary>Soft delete.</summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Bulk sort_order update — certificates share one global order.</summary>
    Task<ServiceResult<bool>> ReorderAsync(CertificateReorderRequest request, CancellationToken cancellationToken);
}
