using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Interfaces;

public interface IProjectService
{
    /// <summary>Always filtered to one site and published, non-deleted rows.</summary>
    Task<PagedResult<ProjectResponse>> GetPublicProjectsAsync(
        Site site,
        string? tagSlug,
        string? categorySlug,
        bool? featured,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProjectResponse>> GetPublicProjectBySlugAsync(
        Site site,
        string slug,
        CancellationToken cancellationToken);

    Task<PagedResult<AdminProjectResponse>> GetAdminProjectsAsync(
        Site? site,
        bool? isPublished,
        string? search,
        bool includeHidden,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminProjectResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<AdminProjectResponse>> CreateAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminProjectResponse>> UpdateAsync(
        Guid id,
        UpdateProjectRequest request,
        CancellationToken cancellationToken);

    /// <summary>First publish stamps published_at and shows the project on both sites.</summary>
    Task<ServiceResult<AdminProjectResponse>> SetPublishedAsync(
        Guid id,
        SetPublishedRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<bool>> ReorderAsync(ReorderRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<ProjectImageResponse>> AddImageAsync(
        Guid projectId,
        AddProjectImageRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProjectImageResponse>> UpdateImageAsync(
        Guid projectId,
        Guid imageId,
        UpdateProjectImageRequest request,
        CancellationToken cancellationToken);

    /// <summary>Hard delete; also deletes the stored file.</summary>
    Task<ServiceResult<bool>> DeleteImageAsync(Guid projectId, Guid imageId, CancellationToken cancellationToken);

    Task<ServiceResult<bool>> ReorderImagesAsync(
        Guid projectId,
        ImageReorderRequest request,
        CancellationToken cancellationToken);
}
