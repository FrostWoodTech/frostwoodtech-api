using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Interfaces;

public interface IProductService
{
    /// <summary>Always filtered to one site and published, non-deleted rows.</summary>
    Task<PagedResult<ProductResponse>> GetPublicProductsAsync(
        Site site,
        bool? featured,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProductResponse>> GetPublicProductBySlugAsync(
        Site site,
        string slug,
        CancellationToken cancellationToken);

    Task<PagedResult<AdminProductResponse>> GetAdminProductsAsync(
        Site? site,
        bool? isPublished,
        string? search,
        bool includeHidden,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminProductResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<AdminProductResponse>> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminProductResponse>> UpdateAsync(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken);

    /// <summary>First publish stamps published_at and shows the product on both sites.</summary>
    Task<ServiceResult<AdminProductResponse>> SetPublishedAsync(
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
    Task<ServiceResult<AdminProductResponse>> RestoreAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Super admin only; stored files are deleted after the commit.</summary>
    Task<ServiceResult<bool>> PurgeAsync(Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<bool>> ReorderAsync(ReorderRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<ProductImageResponse>> AddImageAsync(
        Guid productId,
        AddProductImageRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProductImageResponse>> UpdateImageAsync(
        Guid productId,
        Guid imageId,
        UpdateProductImageRequest request,
        CancellationToken cancellationToken);

    /// <summary>Hard delete; also deletes the stored file.</summary>
    Task<ServiceResult<bool>> DeleteImageAsync(Guid productId, Guid imageId, CancellationToken cancellationToken);

    Task<ServiceResult<bool>> ReorderImagesAsync(
        Guid productId,
        ImageReorderRequest request,
        CancellationToken cancellationToken);
}
