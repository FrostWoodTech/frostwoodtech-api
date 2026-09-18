using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Data;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;
using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Services;

public class ProductService : IProductService
{
    private readonly FrostWoodTechDbContext _db;
    private readonly IMediaService _mediaService;

    public ProductService(FrostWoodTechDbContext db, IMediaService mediaService)
    {
        _db = db;
        _mediaService = mediaService;
    }

    public async Task<PagedResult<ProductResponse>> GetPublicProductsAsync(
        Site site,
        bool? featured,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // is_deleted comes from the global filter; is_published and the site flag are mandatory.
        var query = ForSite(_db.Products.AsNoTracking().Where(p => p.IsPublished), site);

        if (featured is not null)
        {
            query = site == Site.Agency
                ? query.Where(p => p.FeaturedOnAgency == featured)
                : query.Where(p => p.FeaturedOnPersonal == featured);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await OrderForSite(query, site)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(PublicProjection(site))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<ServiceResult<ProductResponse>> GetPublicProductBySlugAsync(
        Site site,
        string slug,
        CancellationToken cancellationToken)
    {
        var product = await ForSite(_db.Products.AsNoTracking().Where(p => p.IsPublished), site)
            .Where(p => p.Slug == slug)
            .Select(PublicProjection(site))
            .FirstOrDefaultAsync(cancellationToken);

        return product is null
            ? ServiceResult<ProductResponse>.NotFound(
                "not_found",
                $"No published product with slug '{slug}' on this site.")
            : ServiceResult<ProductResponse>.Success(product);
    }

    public async Task<PagedResult<AdminProductResponse>> GetAdminProductsAsync(
        Site? site,
        bool? isPublished,
        string? search,
        bool includeHidden,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.Products.AsNoTracking();

        // includeHidden: the visibility screen passes site (for sort order) but must see rows not shown yet.
        if (site is not null && !includeHidden)
        {
            query = ForSite(query, site.Value);
        }

        if (isPublished is not null)
        {
            query = query.Where(p => p.IsPublished == isPublished);
        }

        if (search is not null)
        {
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.UpdatedAt)
            .ThenBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(AdminProjection)
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminProductResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<ServiceResult<AdminProductResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(AdminProjection)
            .FirstOrDefaultAsync(cancellationToken);

        return product is null ? NotFound(id) : ServiceResult<AdminProductResponse>.Success(product);
    }

    public async Task<ServiceResult<AdminProductResponse>> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var name = Blank(request.Name);
        var tagline = Blank(request.Tagline);
        var description = Blank(request.Description);
        var productUrl = Blank(request.ProductUrl);

        var validationError = Validate(name, tagline, description, productUrl, request);
        if (validationError is not null)
        {
            return ServiceResult<AdminProductResponse>.Validation(validationError);
        }

        var slug = ResolveSlug(request.Slug, name!);
        if (slug.Length == 0)
        {
            return ServiceResult<AdminProductResponse>.Validation("A slug could not be generated; provide one with letters or digits.");
        }

        if (await SlugExistsAsync(slug, excludingId: null, cancellationToken))
        {
            return SlugTaken(slug);
        }

        // Sort order never comes from the client; new rows go to the end of each site's order.
        var nextAgencySortOrder = await _db.Products.MaxAsync(p => (int?)p.AgencySortOrder, cancellationToken) + 1 ?? 0;
        var nextPersonalSortOrder = await _db.Products.MaxAsync(p => (int?)p.PersonalSortOrder, cancellationToken) + 1 ?? 0;

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Name = name!,
            Tagline = tagline!,
            Description = description!,
            PriceDetails = Blank(request.PriceDetails),
            ProductUrl = productUrl,
            SeoTitle = Blank(request.SeoTitle),
            SeoDescription = Blank(request.SeoDescription),
            ShowOnAgency = request.ShowOnAgency,
            FeaturedOnAgency = request.FeaturedOnAgency,
            AgencySortOrder = nextAgencySortOrder,
            ShowOnPersonal = request.ShowOnPersonal,
            FeaturedOnPersonal = request.FeaturedOnPersonal,
            PersonalSortOrder = nextPersonalSortOrder
        };

        ApplyPublished(product, request.IsPublished);

        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(product.Id, cancellationToken);
    }

    public async Task<ServiceResult<AdminProductResponse>> UpdateAsync(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return NotFound(id);
        }

        var name = Blank(request.Name);
        var tagline = Blank(request.Tagline);
        var description = Blank(request.Description);
        var productUrl = Blank(request.ProductUrl);

        var validationError = Validate(name, tagline, description, productUrl, request);
        if (validationError is not null)
        {
            return ServiceResult<AdminProductResponse>.Validation(validationError);
        }

        var slug = ResolveSlug(request.Slug, name!);
        if (slug.Length == 0)
        {
            return ServiceResult<AdminProductResponse>.Validation("A slug could not be generated; provide one with letters or digits.");
        }

        if (await SlugExistsAsync(slug, excludingId: id, cancellationToken))
        {
            return SlugTaken(slug);
        }

        product.Slug = slug;
        product.Name = name!;
        product.Tagline = tagline!;
        product.Description = description!;
        product.PriceDetails = Blank(request.PriceDetails);
        product.ProductUrl = productUrl;
        product.SeoTitle = Blank(request.SeoTitle);
        product.SeoDescription = Blank(request.SeoDescription);
        product.ShowOnAgency = request.ShowOnAgency;
        product.FeaturedOnAgency = request.FeaturedOnAgency;
        product.ShowOnPersonal = request.ShowOnPersonal;
        product.FeaturedOnPersonal = request.FeaturedOnPersonal;

        ApplyPublished(product, request.IsPublished);

        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(product.Id, cancellationToken);
    }

    public async Task<ServiceResult<AdminProductResponse>> SetPublishedAsync(
        Guid id,
        SetPublishedRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return NotFound(id);
        }

        // First publish only: show on both sites. Republishing keeps the editor's choice.
        if (request.IsPublished && product.PublishedAt is null)
        {
            product.ShowOnAgency = true;
            product.ShowOnPersonal = true;
        }

        ApplyPublished(product, request.IsPublished);

        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(product.Id, cancellationToken);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No product with id {id}.");
        }

        product.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> ReorderAsync(ReorderRequest request, CancellationToken cancellationToken)
    {
        if (request.Site is null)
        {
            return ServiceResult<bool>.Validation("site is required — sort order is kept per site.");
        }

        var items = request.Items;
        if (items is null || items.Count == 0)
        {
            return ServiceResult<bool>.Validation("At least one item is required.");
        }

        var ids = items.Select(i => i.Id).ToList();
        if (ids.Distinct().Count() != ids.Count)
        {
            return ServiceResult<bool>.Validation("The same product id appears more than once.");
        }

        var products = await _db.Products
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var missing = ids.Where(id => !products.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No product with id {string.Join(", ", missing)}.");
        }

        foreach (var item in items)
        {
            var product = products[item.Id];

            if (request.Site == Site.Agency)
            {
                product.AgencySortOrder = item.SortOrder;
            }
            else
            {
                product.PersonalSortOrder = item.SortOrder;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<ProductImageResponse>> AddImageAsync(
        Guid productId,
        AddProductImageRequest request,
        CancellationToken cancellationToken)
    {
        var product = await LoadWithImagesAsync(productId, cancellationToken);
        if (product is null)
        {
            return ImageProductNotFound(productId);
        }

        var objectKey = Blank(request.ObjectKey);
        var url = Blank(request.Url);
        var altText = Blank(request.AltText);

        var validationError = ValidateImage(objectKey, url, altText, request);
        if (validationError is not null)
        {
            return ServiceResult<ProductImageResponse>.Validation(validationError);
        }

        // The first image is always primary.
        var isPrimary = request.IsPrimary || product.Images.Count == 0;

        var image = new ProductImage
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            ObjectKey = objectKey!,
            Url = url!,
            AltText = altText!,
            Width = request.Width,
            Height = request.Height,
            IsPrimary = isPrimary,
            SortOrder = request.SortOrder
        };

        List<ProductImage> demoted = isPrimary ? OtherPrimaries(product, image.Id) : [];

        await SaveAsync(
            demote: demoted,
            then: () => _db.ProductImages.Add(image),
            cancellationToken);

        return ServiceResult<ProductImageResponse>.Success(ToImageResponse(image));
    }

    public async Task<ServiceResult<ProductImageResponse>> UpdateImageAsync(
        Guid productId,
        Guid imageId,
        UpdateProductImageRequest request,
        CancellationToken cancellationToken)
    {
        var product = await LoadWithImagesAsync(productId, cancellationToken);
        if (product is null)
        {
            return ImageProductNotFound(productId);
        }

        var image = product.Images.FirstOrDefault(i => i.Id == imageId);
        if (image is null)
        {
            return ImageNotFound(productId, imageId);
        }

        var objectKey = Blank(request.ObjectKey);
        var url = Blank(request.Url);
        var altText = Blank(request.AltText);

        var validationError = ValidateImage(objectKey, url, altText, request);
        if (validationError is not null)
        {
            return ServiceResult<ProductImageResponse>.Validation(validationError);
        }

        // Primary only moves by promoting another image; clearing it would leave none.
        var isPrimary = request.IsPrimary || image.IsPrimary || product.Images.Count == 1;

        image.ObjectKey = objectKey!;
        image.Url = url!;
        image.AltText = altText!;
        image.Width = request.Width;
        image.Height = request.Height;
        image.SortOrder = request.SortOrder;

        List<ProductImage> demoted = isPrimary ? OtherPrimaries(product, image.Id) : [];

        await SaveAsync(
            demote: demoted,
            then: () => image.IsPrimary = isPrimary,
            cancellationToken);

        return ServiceResult<ProductImageResponse>.Success(ToImageResponse(image));
    }

    public async Task<ServiceResult<bool>> DeleteImageAsync(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        var product = await LoadWithImagesAsync(productId, cancellationToken);
        if (product is null)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No product with id {productId}.");
        }

        var image = product.Images.FirstOrDefault(i => i.Id == imageId);
        if (image is null)
        {
            return ServiceResult<bool>.NotFound(
                "not_found",
                $"No image with id {imageId} on product {productId}.");
        }

        product.Images.Remove(image);
        _db.ProductImages.Remove(image);

        // Delete must save before promoting, or the partial unique index sees two primaries.
        var successor = image.IsPrimary
            ? product.Images.OrderBy(i => i.SortOrder).FirstOrDefault()
            : null;

        if (successor is null)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            await InOneTransactionAsync(
                first: () => { },
                second: () => successor.IsPrimary = true,
                cancellationToken);
        }

        // After the commit, never before: a failed delete only orphans the file.
        await _mediaService.DeleteFileAsync(image.ObjectKey, cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> ReorderImagesAsync(
        Guid productId,
        ImageReorderRequest request,
        CancellationToken cancellationToken)
    {
        var product = await LoadWithImagesAsync(productId, cancellationToken);
        if (product is null)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No product with id {productId}.");
        }

        var items = request.Items;
        if (items is null || items.Count == 0)
        {
            return ServiceResult<bool>.Validation("At least one item is required.");
        }

        var ids = items.Select(i => i.Id).ToList();
        if (ids.Distinct().Count() != ids.Count)
        {
            return ServiceResult<bool>.Validation("The same image id appears more than once.");
        }

        var images = product.Images.ToDictionary(i => i.Id);

        var missing = ids.Where(id => !images.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            return ServiceResult<bool>.NotFound(
                "not_found",
                $"No image with id {string.Join(", ", missing)} on product {productId}.");
        }

        foreach (var item in items)
        {
            images[item.Id].SortOrder = item.SortOrder;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    private static IQueryable<Product> ForSite(IQueryable<Product> query, Site site) =>
        site == Site.Agency
            ? query.Where(p => p.ShowOnAgency)
            : query.Where(p => p.ShowOnPersonal);

    private static IOrderedQueryable<Product> OrderForSite(IQueryable<Product> query, Site site) =>
        site == Site.Agency
            ? query.OrderBy(p => p.AgencySortOrder).ThenByDescending(p => p.PublishedAt)
            : query.OrderBy(p => p.PersonalSortOrder).ThenByDescending(p => p.PublishedAt);

    private static string? Validate(
        string? name,
        string? tagline,
        string? description,
        string? productUrl,
        CreateProductRequest request)
    {
        if (name is null)
        {
            return "Name is required.";
        }

        if (tagline is null)
        {
            return "Tagline is required.";
        }

        if (description is null)
        {
            return "Description is required.";
        }

        if (productUrl is not null && !IsAbsoluteHttpUrl(productUrl))
        {
            return "productUrl must be an absolute http(s) URL.";
        }

        if (request.FeaturedOnAgency && !request.ShowOnAgency)
        {
            return "featuredOnAgency requires showOnAgency.";
        }

        if (request.FeaturedOnPersonal && !request.ShowOnPersonal)
        {
            return "featuredOnPersonal requires showOnPersonal.";
        }

        return null;
    }

    private static string? ValidateImage(
        string? objectKey,
        string? url,
        string? altText,
        AddProductImageRequest request)
    {
        if (objectKey is null)
        {
            return "objectKey is required.";
        }

        if (url is null || !IsAbsoluteHttpUrl(url))
        {
            return "url is required and must be an absolute http(s) URL.";
        }

        if (altText is null)
        {
            return "altText is required on every image.";
        }

        if (request.Width <= 0 || request.Height <= 0)
        {
            return "width and height must be greater than zero.";
        }

        return null;
    }

    private static bool IsAbsoluteHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    // published_at is stamped on first publish and never cleared.
    private static void ApplyPublished(Product product, bool isPublished)
    {
        if (isPublished && product.PublishedAt is null)
        {
            product.PublishedAt = DateTimeOffset.UtcNow;
        }

        product.IsPublished = isPublished;
    }

    private Task<Product?> LoadWithImagesAsync(Guid productId, CancellationToken cancellationToken) =>
        _db.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

    private static List<ProductImage> OtherPrimaries(Product product, Guid keepId) =>
        [.. product.Images.Where(i => i.IsPrimary && i.Id != keepId)];

    // The partial unique index is checked per statement, so demote in its own save first.
    private Task SaveAsync(List<ProductImage> demote, Action then, CancellationToken cancellationToken)
    {
        if (demote.Count == 0)
        {
            then();

            return _db.SaveChangesAsync(cancellationToken);
        }

        return InOneTransactionAsync(
            first: () =>
            {
                foreach (var image in demote)
                {
                    image.IsPrimary = false;
                }
            },
            second: then,
            cancellationToken);
    }

    // Two saves in one transaction, inside the retrying execution strategy.
    private Task InOneTransactionAsync(Action first, Action second, CancellationToken cancellationToken)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            first();
            await _db.SaveChangesAsync(cancellationToken);

            second();
            await _db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });
    }

    private static string ResolveSlug(string? requestedSlug, string name) =>
        SlugGenerator.Generate(string.IsNullOrWhiteSpace(requestedSlug) ? name : requestedSlug);

    private Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken) =>
        _db.Products.IgnoreQueryFilters().AnyAsync(p => p.Slug == slug && (excludingId == null || p.Id != excludingId), cancellationToken);

    private static ServiceResult<AdminProductResponse> NotFound(Guid id) =>
        ServiceResult<AdminProductResponse>.NotFound("not_found", $"No product with id {id}.");

    private static ServiceResult<AdminProductResponse> SlugTaken(string slug) =>
        ServiceResult<AdminProductResponse>.Conflict("slug_taken", $"Slug '{slug}' is already in use.");

    private static ServiceResult<ProductImageResponse> ImageProductNotFound(Guid productId) =>
        ServiceResult<ProductImageResponse>.NotFound("not_found", $"No product with id {productId}.");

    private static ServiceResult<ProductImageResponse> ImageNotFound(Guid productId, Guid imageId) =>
        ServiceResult<ProductImageResponse>.NotFound(
            "not_found",
            $"No image with id {imageId} on product {productId}.");

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ProductImageResponse ToImageResponse(ProductImage image) => new()
    {
        Id = image.Id,
        ObjectKey = image.ObjectKey,
        Url = image.Url,
        AltText = image.AltText,
        Width = image.Width,
        Height = image.Height,
        IsPrimary = image.IsPrimary,
        SortOrder = image.SortOrder
    };

    private static Expression<Func<Product, ProductResponse>> PublicProjection(Site site)
    {
        if (site == Site.Agency)
        {
            return p => new ProductResponse
            {
                Id = p.Id,
                Slug = p.Slug,
                Name = p.Name,
                Tagline = p.Tagline,
                Description = p.Description,
                PriceDetails = p.PriceDetails,
                ProductUrl = p.ProductUrl,
                PublishedAt = p.PublishedAt,
                SeoTitle = p.SeoTitle,
                SeoDescription = p.SeoDescription,
                Featured = p.FeaturedOnAgency,
                SortOrder = p.AgencySortOrder,
                Images = p.Images
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.SortOrder)
                    .Select(i => new ProductImageResponse
                    {
                        Id = i.Id,
                        ObjectKey = i.ObjectKey,
                        Url = i.Url,
                        AltText = i.AltText,
                        Width = i.Width,
                        Height = i.Height,
                        IsPrimary = i.IsPrimary,
                        SortOrder = i.SortOrder
                    })
                    .ToList()
            };
        }

        return p => new ProductResponse
        {
            Id = p.Id,
            Slug = p.Slug,
            Name = p.Name,
            Tagline = p.Tagline,
            Description = p.Description,
            PriceDetails = p.PriceDetails,
            ProductUrl = p.ProductUrl,
            PublishedAt = p.PublishedAt,
            SeoTitle = p.SeoTitle,
            SeoDescription = p.SeoDescription,
            Featured = p.FeaturedOnPersonal,
            SortOrder = p.PersonalSortOrder,
            Images = p.Images
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.SortOrder)
                .Select(i => new ProductImageResponse
                {
                    Id = i.Id,
                    ObjectKey = i.ObjectKey,
                    Url = i.Url,
                    AltText = i.AltText,
                    Width = i.Width,
                    Height = i.Height,
                    IsPrimary = i.IsPrimary,
                    SortOrder = i.SortOrder
                })
                .ToList()
        };
    }

    private static readonly Expression<Func<Product, AdminProductResponse>> AdminProjection = p => new AdminProductResponse
    {
        Id = p.Id,
        Slug = p.Slug,
        Name = p.Name,
        Tagline = p.Tagline,
        Description = p.Description,
        PriceDetails = p.PriceDetails,
        ProductUrl = p.ProductUrl,
        IsPublished = p.IsPublished,
        PublishedAt = p.PublishedAt,
        SeoTitle = p.SeoTitle,
        SeoDescription = p.SeoDescription,
        ShowOnAgency = p.ShowOnAgency,
        FeaturedOnAgency = p.FeaturedOnAgency,
        AgencySortOrder = p.AgencySortOrder,
        ShowOnPersonal = p.ShowOnPersonal,
        FeaturedOnPersonal = p.FeaturedOnPersonal,
        PersonalSortOrder = p.PersonalSortOrder,
        Images = p.Images
            .OrderByDescending(i => i.IsPrimary)
            .ThenBy(i => i.SortOrder)
            .Select(i => new ProductImageResponse
            {
                Id = i.Id,
                ObjectKey = i.ObjectKey,
                Url = i.Url,
                AltText = i.AltText,
                Width = i.Width,
                Height = i.Height,
                IsPrimary = i.IsPrimary,
                SortOrder = i.SortOrder
            })
            .ToList(),
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
