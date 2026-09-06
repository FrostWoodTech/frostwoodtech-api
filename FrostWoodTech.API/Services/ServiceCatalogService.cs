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

public class ServiceCatalogService : IServiceCatalogService
{
    private readonly FrostWoodTechDbContext _db;

    public ServiceCatalogService(FrostWoodTechDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ServiceResponse>> GetPublicServicesAsync(
        Site site,
        bool? featured,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // is_deleted comes from the global query filter; is_published and the site flag are not optional.
        var query = ForSite(_db.Services.AsNoTracking().Where(s => s.IsPublished), site);

        if (featured is not null)
        {
            query = site == Site.Agency
                ? query.Where(s => s.FeaturedOnAgency == featured)
                : query.Where(s => s.FeaturedOnPersonal == featured);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await OrderForSite(query, site)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(PublicProjection(site, withDetail: false))
            .ToListAsync(cancellationToken);

        return new PagedResult<ServiceResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<ServiceResult<ServiceResponse>> GetPublicServiceBySlugAsync(
        Site site,
        string slug,
        CancellationToken cancellationToken)
    {
        var service = await ForSite(_db.Services.AsNoTracking().Where(s => s.IsPublished), site)
            .Where(s => s.Slug == slug)
            .Select(PublicProjection(site, withDetail: true))
            .FirstOrDefaultAsync(cancellationToken);

        return service is null
            ? ServiceResult<ServiceResponse>.NotFound(
                "not_found",
                $"No published service with slug '{slug}' on this site.")
            : ServiceResult<ServiceResponse>.Success(service);
    }

    public async Task<PagedResult<AdminServiceResponse>> GetAdminServicesAsync(
        Site? site,
        bool? isPublished,
        string? search,
        bool includeHidden,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.Services.AsNoTracking();

        // The reorder/visibility screen needs every published service in both
        // columns — including ones not yet shown anywhere — so it can be the
        // place that turns showing on in the first place. `includeHidden` is
        // how that screen opts out of the show-on-site filter while still
        // passing `site` (kept for the response's per-site sort order and to
        // keep the two columns' query-cache entries distinct on the client).
        if (site is not null && !includeHidden)
        {
            query = ForSite(query, site.Value);
        }

        if (isPublished is not null)
        {
            query = query.Where(s => s.IsPublished == isPublished);
        }

        if (search is not null)
        {
            query = query.Where(s => EF.Functions.ILike(s.Name, $"%{search}%"));
        }

        var total = await query.CountAsync(cancellationToken);

        // Drafts have no meaningful site order, so the admin list is alphabetical instead.
        var items = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(AdminProjection)
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminServiceResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<ServiceResult<AdminServiceResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var service = await _db.Services
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(AdminProjection)
            .FirstOrDefaultAsync(cancellationToken);

        return service is null ? NotFound(id) : ServiceResult<AdminServiceResponse>.Success(service);
    }

    public async Task<ServiceResult<AdminServiceResponse>> CreateAsync(
        CreateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var name = Blank(request.Name);
        var shortDescription = Blank(request.ShortDescription);

        var validationError = Validate(name, shortDescription, request);
        if (validationError is not null)
        {
            return ServiceResult<AdminServiceResponse>.Validation(validationError);
        }

        var projectIds = Distinct(request.ProjectIds);

        var unknownProjects = await UnknownProjectIdsAsync(projectIds, cancellationToken);
        if (unknownProjects is not null)
        {
            return ServiceResult<AdminServiceResponse>.Validation(unknownProjects);
        }

        var slug = ResolveSlug(request.Slug, name!);
        if (await SlugExistsAsync(slug, excludingId: null, cancellationToken))
        {
            return SlugTaken(slug);
        }

        var service = new ServiceOffering
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Name = name!,
            ShortDescription = shortDescription!,
            Eyebrow = Blank(request.Eyebrow),
            Headline = Blank(request.Headline),
            Deck = Blank(request.Deck),
            WhoThisIsFor = Blank(request.WhoThisIsFor),
            Outcomes = Blank(request.Outcomes),
            Capabilities = Blank(request.Capabilities),
            InDepth = Blank(request.InDepth),
            PrimaryCtaLabel = Blank(request.PrimaryCtaLabel),
            PrimaryCtaUrl = Blank(request.PrimaryCtaUrl),
            SecondaryCtaLabel = Blank(request.SecondaryCtaLabel),
            SecondaryCtaUrl = Blank(request.SecondaryCtaUrl),
            SeoTitle = Blank(request.SeoTitle),
            SeoDescription = Blank(request.SeoDescription),
            ServiceProjects = [.. projectIds.Select(projectId => new ServiceProject { ProjectId = projectId })],
            IconObjectKey = Blank(request.IconObjectKey),
            IconUrl = Blank(request.IconUrl),
            IconWidth = request.IconWidth,
            IconHeight = request.IconHeight,
            IconAltText = Blank(request.IconAltText),
            HeroImageObjectKey = Blank(request.HeroImageObjectKey),
            HeroImageUrl = Blank(request.HeroImageUrl),
            HeroImageWidth = request.HeroImageWidth,
            HeroImageHeight = request.HeroImageHeight,
            HeroImageAltText = Blank(request.HeroImageAltText),
            DepthImageObjectKey = Blank(request.DepthImageObjectKey),
            DepthImageUrl = Blank(request.DepthImageUrl),
            DepthImageWidth = request.DepthImageWidth,
            DepthImageHeight = request.DepthImageHeight,
            DepthImageAltText = Blank(request.DepthImageAltText),
            ShowOnAgency = request.ShowOnAgency,
            FeaturedOnAgency = request.FeaturedOnAgency,
            AgencySortOrder = request.AgencySortOrder,
            ShowOnPersonal = request.ShowOnPersonal,
            FeaturedOnPersonal = request.FeaturedOnPersonal,
            PersonalSortOrder = request.PersonalSortOrder
        };

        ApplyPublished(service, request.IsPublished);

        _db.Services.Add(service);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(service.Id, cancellationToken);
    }

    public async Task<ServiceResult<AdminServiceResponse>> UpdateAsync(
        Guid id,
        UpdateServiceRequest request,
        CancellationToken cancellationToken)
    {
        // Includes the links because SyncProjects diffs against them.
        var service = await _db.Services
            .Include(s => s.ServiceProjects)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (service is null)
        {
            return NotFound(id);
        }

        var name = Blank(request.Name);
        var shortDescription = Blank(request.ShortDescription);

        var validationError = Validate(name, shortDescription, request);
        if (validationError is not null)
        {
            return ServiceResult<AdminServiceResponse>.Validation(validationError);
        }

        var projectIds = Distinct(request.ProjectIds);

        var unknownProjects = await UnknownProjectIdsAsync(projectIds, cancellationToken);
        if (unknownProjects is not null)
        {
            return ServiceResult<AdminServiceResponse>.Validation(unknownProjects);
        }

        var slug = ResolveSlug(request.Slug, name!);
        if (await SlugExistsAsync(slug, excludingId: id, cancellationToken))
        {
            return SlugTaken(slug);
        }

        service.Slug = slug;
        service.Name = name!;
        service.ShortDescription = shortDescription!;
        service.Eyebrow = Blank(request.Eyebrow);
        service.Headline = Blank(request.Headline);
        service.Deck = Blank(request.Deck);
        service.WhoThisIsFor = Blank(request.WhoThisIsFor);
        service.Outcomes = Blank(request.Outcomes);
        service.Capabilities = Blank(request.Capabilities);
        service.InDepth = Blank(request.InDepth);
        service.PrimaryCtaLabel = Blank(request.PrimaryCtaLabel);
        service.PrimaryCtaUrl = Blank(request.PrimaryCtaUrl);
        service.SecondaryCtaLabel = Blank(request.SecondaryCtaLabel);
        service.SecondaryCtaUrl = Blank(request.SecondaryCtaUrl);
        service.SeoTitle = Blank(request.SeoTitle);
        service.SeoDescription = Blank(request.SeoDescription);
        service.IconObjectKey = Blank(request.IconObjectKey);
        service.IconUrl = Blank(request.IconUrl);
        service.IconWidth = request.IconWidth;
        service.IconHeight = request.IconHeight;
        service.IconAltText = Blank(request.IconAltText);
        service.HeroImageObjectKey = Blank(request.HeroImageObjectKey);
        service.HeroImageUrl = Blank(request.HeroImageUrl);
        service.HeroImageWidth = request.HeroImageWidth;
        service.HeroImageHeight = request.HeroImageHeight;
        service.HeroImageAltText = Blank(request.HeroImageAltText);
        service.DepthImageObjectKey = Blank(request.DepthImageObjectKey);
        service.DepthImageUrl = Blank(request.DepthImageUrl);
        service.DepthImageWidth = request.DepthImageWidth;
        service.DepthImageHeight = request.DepthImageHeight;
        service.DepthImageAltText = Blank(request.DepthImageAltText);
        service.ShowOnAgency = request.ShowOnAgency;
        service.FeaturedOnAgency = request.FeaturedOnAgency;
        service.AgencySortOrder = request.AgencySortOrder;
        service.ShowOnPersonal = request.ShowOnPersonal;
        service.FeaturedOnPersonal = request.FeaturedOnPersonal;
        service.PersonalSortOrder = request.PersonalSortOrder;

        ApplyPublished(service, request.IsPublished);
        SyncProjects(service, projectIds);

        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(service.Id, cancellationToken);
    }

    public async Task<ServiceResult<AdminServiceResponse>> SetPublishedAsync(
        Guid id,
        SetPublishedRequest request,
        CancellationToken cancellationToken)
    {
        var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (service is null)
        {
            return NotFound(id);
        }

        // A first publish makes the service visible on both sites by default — the
        // reorder/visibility screen is where an editor dials that back down
        // afterwards. Re-publishing, and the full edit form (which has its own
        // explicit show checkboxes), never overrides an existing choice.
        if (request.IsPublished && !service.IsPublished)
        {
            service.ShowOnAgency = true;
            service.ShowOnPersonal = true;
        }

        ApplyPublished(service, request.IsPublished);

        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(service.Id, cancellationToken);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (service is null)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No service with id {id}.");
        }

        // Soft delete: the pricing plans stay put so restoring the row keeps them.
        service.IsDeleted = true;
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
            return ServiceResult<bool>.Validation("The same service id appears more than once.");
        }

        var services = await _db.Services
            .Where(s => ids.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        var missing = ids.Where(id => !services.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No service with id {string.Join(", ", missing)}.");
        }

        foreach (var item in items)
        {
            var service = services[item.Id];

            if (request.Site == Site.Agency)
            {
                service.AgencySortOrder = item.SortOrder;
            }
            else
            {
                service.PersonalSortOrder = item.SortOrder;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    private static IQueryable<ServiceOffering> ForSite(IQueryable<ServiceOffering> query, Site site) =>
        site == Site.Agency
            ? query.Where(s => s.ShowOnAgency)
            : query.Where(s => s.ShowOnPersonal);

    private static IOrderedQueryable<ServiceOffering> OrderForSite(IQueryable<ServiceOffering> query, Site site) =>
        site == Site.Agency
            ? query.OrderBy(s => s.AgencySortOrder).ThenByDescending(s => s.PublishedAt)
            : query.OrderBy(s => s.PersonalSortOrder).ThenByDescending(s => s.PublishedAt);

    /// <summary>Null when the service is valid, otherwise the message to hand back.</summary>
    private static string? Validate(
        string? name,
        string? shortDescription,
        CreateServiceRequest request)
    {
        if (name is null)
        {
            return "Name is required.";
        }

        if (shortDescription is null)
        {
            return "shortDescription is required.";
        }

        if (request.FeaturedOnAgency && !request.ShowOnAgency)
        {
            return "featuredOnAgency requires showOnAgency.";
        }

        if (request.FeaturedOnPersonal && !request.ShowOnPersonal)
        {
            return "featuredOnPersonal requires showOnPersonal.";
        }

        if (request.IconObjectKey is not null && string.IsNullOrWhiteSpace(request.IconAltText))
        {
            return "iconAltText is required whenever an icon is set.";
        }

        if (request.HeroImageObjectKey is not null && string.IsNullOrWhiteSpace(request.HeroImageAltText))
        {
            return "heroImageAltText is required whenever a hero image is set.";
        }

        if (request.DepthImageObjectKey is not null && string.IsNullOrWhiteSpace(request.DepthImageAltText))
        {
            return "depthImageAltText is required whenever a depth image is set.";
        }

        // A label with no URL renders a dead button, a URL with no label renders nothing at all.
        if (string.IsNullOrWhiteSpace(request.PrimaryCtaLabel) != string.IsNullOrWhiteSpace(request.PrimaryCtaUrl))
        {
            return "primaryCtaLabel and primaryCtaUrl must be set together.";
        }

        if (string.IsNullOrWhiteSpace(request.SecondaryCtaLabel) != string.IsNullOrWhiteSpace(request.SecondaryCtaUrl))
        {
            return "secondaryCtaLabel and secondaryCtaUrl must be set together.";
        }

        return null;
    }

    /// <summary>Null when every id resolves to a live project, otherwise the message to hand back.</summary>
    private async Task<string?> UnknownProjectIdsAsync(
        IReadOnlyList<Guid> projectIds,
        CancellationToken cancellationToken)
    {
        if (projectIds.Count == 0)
        {
            return null;
        }

        var found = await _db.Projects
            .Where(p => projectIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var missing = projectIds.Except(found).ToList();

        return missing.Count == 0 ? null : $"Unknown project id(s): {string.Join(", ", missing)}.";
    }

    /// <summary>Adds and removes only what changed, so untouched links keep their row.</summary>
    private void SyncProjects(ServiceOffering service, IReadOnlyList<Guid> projectIds)
    {
        foreach (var link in service.ServiceProjects.Where(sp => !projectIds.Contains(sp.ProjectId)).ToList())
        {
            service.ServiceProjects.Remove(link);
            _db.Remove(link);
        }

        foreach (var projectId in projectIds.Where(id => service.ServiceProjects.All(sp => sp.ProjectId != id)))
        {
            service.ServiceProjects.Add(new ServiceProject { ServiceId = service.Id, ProjectId = projectId });
        }
    }

    private static IReadOnlyList<Guid> Distinct(List<Guid>? projectIds) =>
        projectIds is null ? [] : [.. projectIds.Distinct()];

    /// <summary>
    /// Going live stamps published_at the first time only; unpublishing never clears it, so a
    /// service that comes back keeps its original date.
    /// </summary>
    private static void ApplyPublished(ServiceOffering service, bool isPublished)
    {
        if (isPublished && service.PublishedAt is null)
        {
            service.PublishedAt = DateTimeOffset.UtcNow;
        }

        service.IsPublished = isPublished;
    }

    private static string ResolveSlug(string? requestedSlug, string name) =>
        SlugGenerator.Generate(string.IsNullOrWhiteSpace(requestedSlug) ? name : requestedSlug);

    private Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken) =>
        _db.Services.AnyAsync(s => s.Slug == slug && (excludingId == null || s.Id != excludingId), cancellationToken);

    private static ServiceResult<AdminServiceResponse> NotFound(Guid id) =>
        ServiceResult<AdminServiceResponse>.NotFound("not_found", $"No service with id {id}.");

    private static ServiceResult<AdminServiceResponse> SlugTaken(string slug) =>
        ServiceResult<AdminServiceResponse>.Conflict("slug_taken", $"Slug '{slug}' is already in use.");

    /// <summary>Trimmed, or null when the caller sent nothing meaningful.</summary>
    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// The public shape for one site. <paramref name="withDetail"/> adds the linked projects and
    /// this service's own FAQs — only the single-service endpoint asks for those, since the list
    /// cards render neither and loading them per row would be an N+1 for nothing.
    /// </summary>
    private static Expression<Func<ServiceOffering, ServiceResponse>> PublicProjection(Site site, bool withDetail)
    {
        // Captured, so EF parameterises the site choice instead of this method needing one
        // near-identical copy of the projection per site.
        var isAgency = site == Site.Agency;

        if (!withDetail)
        {
            return s => new ServiceResponse
            {
                Id = s.Id,
                Slug = s.Slug,
                Name = s.Name,
                ShortDescription = s.ShortDescription,
                Eyebrow = s.Eyebrow,
                Headline = s.Headline,
                Deck = s.Deck,
                WhoThisIsFor = s.WhoThisIsFor,
                Outcomes = s.Outcomes,
                Capabilities = s.Capabilities,
                InDepth = s.InDepth,
                PrimaryCtaLabel = s.PrimaryCtaLabel,
                PrimaryCtaUrl = s.PrimaryCtaUrl,
                SecondaryCtaLabel = s.SecondaryCtaLabel,
                SecondaryCtaUrl = s.SecondaryCtaUrl,
                IconUrl = s.IconUrl,
                IconWidth = s.IconWidth,
                IconHeight = s.IconHeight,
                IconAltText = s.IconAltText,
                HeroImageUrl = s.HeroImageUrl,
                HeroImageWidth = s.HeroImageWidth,
                HeroImageHeight = s.HeroImageHeight,
                HeroImageAltText = s.HeroImageAltText,
                DepthImageUrl = s.DepthImageUrl,
                DepthImageWidth = s.DepthImageWidth,
                DepthImageHeight = s.DepthImageHeight,
                DepthImageAltText = s.DepthImageAltText,
                SeoTitle = s.SeoTitle,
                SeoDescription = s.SeoDescription,
                PublishedAt = s.PublishedAt,
                Featured = isAgency ? s.FeaturedOnAgency : s.FeaturedOnPersonal,
                SortOrder = isAgency ? s.AgencySortOrder : s.PersonalSortOrder
            };
        }

        return s => new ServiceResponse
        {
            Id = s.Id,
            Slug = s.Slug,
            Name = s.Name,
            ShortDescription = s.ShortDescription,
            Eyebrow = s.Eyebrow,
            Headline = s.Headline,
            Deck = s.Deck,
            WhoThisIsFor = s.WhoThisIsFor,
            Outcomes = s.Outcomes,
            Capabilities = s.Capabilities,
            InDepth = s.InDepth,
            PrimaryCtaLabel = s.PrimaryCtaLabel,
            PrimaryCtaUrl = s.PrimaryCtaUrl,
            SecondaryCtaLabel = s.SecondaryCtaLabel,
            SecondaryCtaUrl = s.SecondaryCtaUrl,
            IconUrl = s.IconUrl,
            IconWidth = s.IconWidth,
            IconHeight = s.IconHeight,
            IconAltText = s.IconAltText,
            HeroImageUrl = s.HeroImageUrl,
            HeroImageWidth = s.HeroImageWidth,
            HeroImageHeight = s.HeroImageHeight,
            HeroImageAltText = s.HeroImageAltText,
            DepthImageUrl = s.DepthImageUrl,
            DepthImageWidth = s.DepthImageWidth,
            DepthImageHeight = s.DepthImageHeight,
            DepthImageAltText = s.DepthImageAltText,
            SeoTitle = s.SeoTitle,
            SeoDescription = s.SeoDescription,
            PublishedAt = s.PublishedAt,
            Featured = isAgency ? s.FeaturedOnAgency : s.FeaturedOnPersonal,
            SortOrder = isAgency ? s.AgencySortOrder : s.PersonalSortOrder,
            // ServiceProject is not an AuditableEntity, so the global soft-delete filter skips it
            // — the project's own published/deleted/site state has to be checked here.
            Projects = s.ServiceProjects
                .Where(sp => !sp.Project.IsDeleted
                    && sp.Project.IsPublished
                    && (isAgency ? sp.Project.ShowOnAgency : sp.Project.ShowOnPersonal))
                .OrderBy(sp => isAgency ? sp.Project.AgencySortOrder : sp.Project.PersonalSortOrder)
                .ThenByDescending(sp => sp.Project.Year)
                .Select(sp => new ServiceProjectResponse
                {
                    Id = sp.Project.Id,
                    Slug = sp.Project.Slug,
                    Title = sp.Project.Title,
                    ShortDescription = sp.Project.ShortDescription,
                    Year = sp.Project.Year,
                    ImageUrl = sp.Project.Images
                        .OrderByDescending(i => i.IsPrimary)
                        .ThenBy(i => i.SortOrder)
                        .Select(i => i.Url)
                        .FirstOrDefault(),
                    ImageAltText = sp.Project.Images
                        .OrderByDescending(i => i.IsPrimary)
                        .ThenBy(i => i.SortOrder)
                        .Select(i => i.AltText)
                        .FirstOrDefault()
                })
                .ToList(),
            Faqs = s.Faqs
                .Where(f => !f.IsDeleted
                    && f.IsPublished
                    && (isAgency ? f.ShowOnAgency : f.ShowOnPersonal))
                .OrderBy(f => f.SortOrder)
                .Select(f => new FaqResponse
                {
                    Id = f.Id,
                    Question = f.Question,
                    Answer = f.Answer,
                    SortOrder = f.SortOrder
                })
                .ToList()
        };
    }

    private static readonly Expression<Func<ServiceOffering, AdminServiceResponse>> AdminProjection = s => new AdminServiceResponse
    {
        Id = s.Id,
        Slug = s.Slug,
        Name = s.Name,
        ShortDescription = s.ShortDescription,
        Eyebrow = s.Eyebrow,
        Headline = s.Headline,
        Deck = s.Deck,
        WhoThisIsFor = s.WhoThisIsFor,
        Outcomes = s.Outcomes,
        Capabilities = s.Capabilities,
        InDepth = s.InDepth,
        PrimaryCtaLabel = s.PrimaryCtaLabel,
        PrimaryCtaUrl = s.PrimaryCtaUrl,
        SecondaryCtaLabel = s.SecondaryCtaLabel,
        SecondaryCtaUrl = s.SecondaryCtaUrl,
        IconObjectKey = s.IconObjectKey,
        IconUrl = s.IconUrl,
        IconWidth = s.IconWidth,
        IconHeight = s.IconHeight,
        IconAltText = s.IconAltText,
        HeroImageObjectKey = s.HeroImageObjectKey,
        HeroImageUrl = s.HeroImageUrl,
        HeroImageWidth = s.HeroImageWidth,
        HeroImageHeight = s.HeroImageHeight,
        HeroImageAltText = s.HeroImageAltText,
        DepthImageObjectKey = s.DepthImageObjectKey,
        DepthImageUrl = s.DepthImageUrl,
        DepthImageWidth = s.DepthImageWidth,
        DepthImageHeight = s.DepthImageHeight,
        DepthImageAltText = s.DepthImageAltText,
        Projects = s.ServiceProjects
            .Where(sp => !sp.Project.IsDeleted)
            .OrderByDescending(sp => sp.Project.Year)
            .Select(sp => new ServiceProjectSummary
            {
                Id = sp.Project.Id,
                Slug = sp.Project.Slug,
                Title = sp.Project.Title,
                Year = sp.Project.Year,
                IsPublished = sp.Project.IsPublished
            })
            .ToList(),
        SeoTitle = s.SeoTitle,
        SeoDescription = s.SeoDescription,
        IsPublished = s.IsPublished,
        PublishedAt = s.PublishedAt,
        ShowOnAgency = s.ShowOnAgency,
        FeaturedOnAgency = s.FeaturedOnAgency,
        AgencySortOrder = s.AgencySortOrder,
        ShowOnPersonal = s.ShowOnPersonal,
        FeaturedOnPersonal = s.FeaturedOnPersonal,
        PersonalSortOrder = s.PersonalSortOrder,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
    };
}
