using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Interfaces;

public interface ITagService
{
    Task<IReadOnlyList<TagResponse>> GetPublicTagsAsync(
        bool? isTechnology,
        TechCategory? category,
        CancellationToken cancellationToken);

    Task<PagedResult<AdminTagResponse>> GetAdminTagsAsync(
        bool? isTechnology,
        TechCategory? category,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminTagResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<AdminTagResponse>> CreateAsync(CreateTagRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<AdminTagResponse>> UpdateAsync(Guid id, UpdateTagRequest request, CancellationToken cancellationToken);

    /// <summary>Refused with tag_in_use while a live project or article uses the tag.</summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<TrashedItemResponse>> GetTrashAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>404 unless the row is in the trash.</summary>
    Task<ServiceResult<AdminTagResponse>> RestoreAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Super admin only; stored files are deleted after the commit.</summary>
    Task<ServiceResult<bool>> PurgeAsync(Guid id, CancellationToken cancellationToken);
}
