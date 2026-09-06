using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Interfaces;

public interface IArticleService
{
    /// <summary>
    /// Public read: always scoped to one site and to published, non-deleted rows. There is no
    /// overload that lets a caller skip those filters.
    /// </summary>
    Task<PagedResult<ArticleResponse>> GetPublicArticlesAsync(
        Site site,
        string? tagSlug,
        bool? featured,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ServiceResult<ArticleResponse>> GetPublicArticleBySlugAsync(
        Site site,
        string slug,
        CancellationToken cancellationToken);

    Task<PagedResult<AdminArticleResponse>> GetAdminArticlesAsync(
        Site? site,
        bool? isPublished,
        string? search,
        bool includeHidden,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminArticleResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<AdminArticleResponse>> CreateAsync(
        CreateArticleRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminArticleResponse>> UpdateAsync(
        Guid id,
        UpdateArticleRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Flips the draft flag, stamping published_at the first time only — same set-once pattern
    /// as projects and services.
    /// </summary>
    Task<ServiceResult<AdminArticleResponse>> SetPublishedAsync(
        Guid id,
        SetPublishedRequest request,
        CancellationToken cancellationToken);

    /// <summary>Soft delete.</summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Bulk sort_order update for one site.</summary>
    Task<ServiceResult<bool>> ReorderAsync(ReorderRequest request, CancellationToken cancellationToken);
}
