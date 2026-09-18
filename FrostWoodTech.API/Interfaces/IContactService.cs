using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Interfaces;

public interface IContactService
{
    /// <summary>Anonymous; rate limited per IP. A filled honeypot is stored as spam, not rejected.</summary>
    Task<ServiceResult<ContactSubmissionResponse>> SubmitAsync(
        CreateContactSubmissionRequest request,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task<PagedResult<AdminContactSubmissionResponse>> GetAdminSubmissionsAsync(
        ContactSubmissionStatus? status,
        Site? site,
        Guid? serviceId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminContactSubmissionResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Triage only. Moving to Replied stamps RepliedAt/RepliedBy.</summary>
    Task<ServiceResult<AdminContactSubmissionResponse>> UpdateAsync(
        Guid id,
        UpdateContactSubmissionRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<TrashedItemResponse>> GetTrashAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>404 unless the row is in the trash.</summary>
    Task<ServiceResult<AdminContactSubmissionResponse>> RestoreAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Super admin only.</summary>
    Task<ServiceResult<bool>> PurgeAsync(Guid id, CancellationToken cancellationToken);
}
