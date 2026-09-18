using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;

namespace FrostWoodTech.API.Interfaces;

public interface ICertificateService
{
    /// <summary>Published, non-deleted rows only; personal-site only, so no site parameter.</summary>
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

    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<TrashedItemResponse>> GetTrashAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>404 unless the row is in the trash.</summary>
    Task<ServiceResult<AdminCertificateResponse>> RestoreAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Super admin only; stored files are deleted after the commit.</summary>
    Task<ServiceResult<bool>> PurgeAsync(Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<bool>> ReorderAsync(CertificateReorderRequest request, CancellationToken cancellationToken);
}
