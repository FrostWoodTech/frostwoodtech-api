using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Interfaces;

public interface IContactService
{
    /// <summary>
    /// Anonymous public submission. Rate limited per IP; a filled honeypot lands as spam instead
    /// of being rejected. Never public content — there is no matching public read.
    /// </summary>
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

    /// <summary>Triage only — status and admin notes. Stamps RepliedAt/RepliedBy when status moves to Replied.</summary>
    Task<ServiceResult<AdminContactSubmissionResponse>> UpdateAsync(
        Guid id,
        UpdateContactSubmissionRequest request,
        CancellationToken cancellationToken);

    /// <summary>Soft delete.</summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
