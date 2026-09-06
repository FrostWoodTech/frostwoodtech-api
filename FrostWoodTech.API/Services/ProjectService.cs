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

public class ProjectService : IProjectService
{
    /// <summary>Anything older is a typo, not a project.</summary>
    private const int EarliestYear = 1990;

    private readonly FrostWoodTechDbContext _db;
    private readonly IMediaService _mediaService;

    public ProjectService(FrostWoodTechDbContext db, IMediaService mediaService)
    {
        _db = db;
        _mediaService = mediaService;
    }

    public async Task<PagedResult<ProjectResponse>> GetPublicProjectsAsync(
        Site site,
        string? tagSlug,
        string? categorySlug,
        bool? featured,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // is_deleted comes from the global query filter; is_published and the site flag are not optional.
        var query = ForSite(_db.Projects.AsNoTracking().Where(p => p.IsPublished), site);

        if (tagSlug is not null)
        {
            query = query.Where(p => p.ProjectTags.Any(pt => pt.Tag.Slug == tagSlug && !pt.Tag.IsDeleted));
        }

        if (categorySlug is not null)
        {
            // A category tag, not a technology one — both live in the same table.
            query = query.Where(p => p.ProjectTags.Any(pt =>
                pt.Tag.Slug == categorySlug && !pt.Tag.IsTechnology && !pt.Tag.IsDeleted));
        }

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

        return new PagedResult<ProjectResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<ServiceResult<ProjectResponse>> GetPublicProjectBySlugAsync(
        Site site,
        string slug,
        CancellationToken cancellationToken)
    {
        var project = await ForSite(_db.Projects.AsNoTracking().Where(p => p.IsPublished), site)
            .Where(p => p.Slug == slug)
            .Select(PublicProjection(site))
            .FirstOrDefaultAsync(cancellationToken);

        return project is null
            ? ServiceResult<ProjectResponse>.NotFound(
                "not_found",
                $"No published project with slug '{slug}' on this site.")
            : ServiceResult<ProjectResponse>.Success(project);
    }

    public async Task<PagedResult<AdminProjectResponse>> GetAdminProjectsAsync(
        Site? site,
        bool? isPublished,
        string? search,
        bool includeHidden,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.Projects.AsNoTracking();

        // The reorder/visibility screen needs every published project in both
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
            query = query.Where(p => p.IsPublished == isPublished);
        }

        if (search is not null)
        {
            query = query.Where(p => EF.Functions.ILike(p.Title, $"%{search}%"));
        }

        var total = await query.CountAsync(cancellationToken);

        // Drafts have no meaningful site order, so the admin list is newest-first instead.
        var items = await query
            .OrderByDescending(p => p.Year)
            .ThenBy(p => p.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(AdminProjection)
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminProjectResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<ServiceResult<AdminProjectResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var project = await _db.Projects
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(AdminProjection)
            .FirstOrDefaultAsync(cancellationToken);

        return project is null ? NotFound(id) : ServiceResult<AdminProjectResponse>.Success(project);
    }

    public async Task<ServiceResult<AdminProjectResponse>> CreateAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var title = Blank(request.Title);
        var shortDescription = Blank(request.ShortDescription);
        var description = Blank(request.Description);
        var websiteUrl = Blank(request.WebsiteUrl);

        var validationError = Validate(title, shortDescription, description, websiteUrl, request);
        if (validationError is not null)
        {
            return ServiceResult<AdminProjectResponse>.Validation(validationError);
        }

        var tagIds = Distinct(request.TagIds);

        var unknownTags = await UnknownTagIdsAsync(tagIds, cancellationToken);
        if (unknownTags is not null)
        {
            return ServiceResult<AdminProjectResponse>.Validation(unknownTags);
        }

        var slug = ResolveSlug(request.Slug, title!);
        if (await SlugExistsAsync(slug, excludingId: null, cancellationToken))
        {
            return SlugTaken(slug);
        }

        // Sort order is never taken from the client — it only changes via ReorderAsync, so a new
        // project is simply appended to the end of each site's shared order.
        var nextAgencySortOrder = await _db.Projects.MaxAsync(p => (int?)p.AgencySortOrder, cancellationToken) + 1 ?? 0;
        var nextPersonalSortOrder = await _db.Projects.MaxAsync(p => (int?)p.PersonalSortOrder, cancellationToken) + 1 ?? 0;

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Title = title!,
            Year = request.Year,
            ShortDescription = shortDescription!,
            Description = description!,
            WebsiteUrl = websiteUrl,
            Problem = Blank(request.Problem),
            Solution = Blank(request.Solution),
            WhatWeDelivered = Blank(request.WhatWeDelivered),
            Proof = Blank(request.Proof),
            ClientName = Blank(request.ClientName),
            SeoTitle = Blank(request.SeoTitle),
            SeoDescription = Blank(request.SeoDescription),
            ShowOnAgency = request.ShowOnAgency,
            FeaturedOnAgency = request.FeaturedOnAgency,
            AgencySortOrder = nextAgencySortOrder,
            ShowOnPersonal = request.ShowOnPersonal,
            FeaturedOnPersonal = request.FeaturedOnPersonal,
            PersonalSortOrder = nextPersonalSortOrder,
            ProjectTags = [.. tagIds.Select(tagId => new ProjectTag { TagId = tagId })]
        };

        ApplyPublished(project, request.IsPublished);

        _db.Projects.Add(project);
        await _db.SaveChangesAsync(cancellationToken);

        // Re-read: links were added by tag id, so their Tag navigations are not loaded yet.
        return await GetByIdAsync(project.Id, cancellationToken);
    }

    public async Task<ServiceResult<AdminProjectResponse>> UpdateAsync(
        Guid id,
        UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var project = await _db.Projects
            .Include(p => p.ProjectTags)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (project is null)
        {
            return NotFound(id);
        }

        var title = Blank(request.Title);
        var shortDescription = Blank(request.ShortDescription);
        var description = Blank(request.Description);
        var websiteUrl = Blank(request.WebsiteUrl);

        var validationError = Validate(title, shortDescription, description, websiteUrl, request);
        if (validationError is not null)
        {
            return ServiceResult<AdminProjectResponse>.Validation(validationError);
        }

        var tagIds = Distinct(request.TagIds);

        var unknownTags = await UnknownTagIdsAsync(tagIds, cancellationToken);
        if (unknownTags is not null)
        {
            return ServiceResult<AdminProjectResponse>.Validation(unknownTags);
        }

        var slug = ResolveSlug(request.Slug, title!);
        if (await SlugExistsAsync(slug, excludingId: id, cancellationToken))
        {
            return SlugTaken(slug);
        }

        project.Slug = slug;
        project.Title = title!;
        project.Year = request.Year;
        project.ShortDescription = shortDescription!;
        project.Description = description!;
        project.WebsiteUrl = websiteUrl;
        project.Problem = Blank(request.Problem);
        project.Solution = Blank(request.Solution);
        project.WhatWeDelivered = Blank(request.WhatWeDelivered);
        project.Proof = Blank(request.Proof);
        project.ClientName = Blank(request.ClientName);
        project.SeoTitle = Blank(request.SeoTitle);
        project.SeoDescription = Blank(request.SeoDescription);
        project.ShowOnAgency = request.ShowOnAgency;
        project.FeaturedOnAgency = request.FeaturedOnAgency;
        project.ShowOnPersonal = request.ShowOnPersonal;
        project.FeaturedOnPersonal = request.FeaturedOnPersonal;

        ApplyPublished(project, request.IsPublished);
        SyncTags(project, tagIds);

        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(project.Id, cancellationToken);
    }

    public async Task<ServiceResult<AdminProjectResponse>> SetPublishedAsync(
        Guid id,
        SetPublishedRequest request,
        CancellationToken cancellationToken)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (project is null)
        {
            return NotFound(id);
        }

        // A first publish makes the project visible on both sites by default — the
        // reorder/visibility screen is where an editor dials that back down
        // afterwards. Re-publishing, and the full edit form (which has its own
        // explicit show checkboxes), never overrides an existing choice.
        if (request.IsPublished && !project.IsPublished)
        {
            project.ShowOnAgency = true;
            project.ShowOnPersonal = true;
        }

        ApplyPublished(project, request.IsPublished);

        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(project.Id, cancellationToken);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (project is null)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No project with id {id}.");
        }

        // Soft delete: the tag links and image rows stay put so restoring the row keeps them.
        project.IsDeleted = true;
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
            return ServiceResult<bool>.Validation("The same project id appears more than once.");
        }

        var projects = await _db.Projects
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var missing = ids.Where(id => !projects.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No project with id {string.Join(", ", missing)}.");
        }

        foreach (var item in items)
        {
            var project = projects[item.Id];

            if (request.Site == Site.Agency)
            {
                project.AgencySortOrder = item.SortOrder;
            }
            else
            {
                project.PersonalSortOrder = item.SortOrder;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<ProjectImageResponse>> AddImageAsync(
        Guid projectId,
        AddProjectImageRequest request,
        CancellationToken cancellationToken)
    {
        var project = await LoadWithImagesAsync(projectId, cancellationToken);
        if (project is null)
        {
            return ImageProjectNotFound(projectId);
        }

        var objectKey = Blank(request.ObjectKey);
        var url = Blank(request.Url);
        var altText = Blank(request.AltText);

        var validationError = ValidateImage(objectKey, url, altText, request);
        if (validationError is not null)
        {
            return ServiceResult<ProjectImageResponse>.Validation(validationError);
        }

        // First image is always primary, whatever the caller asked — a project has exactly one.
        var isPrimary = request.IsPrimary || project.Images.Count == 0;

        var image = new ProjectImage
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            ObjectKey = objectKey!,
            Url = url!,
            AltText = altText!,
            Width = request.Width,
            Height = request.Height,
            IsPrimary = isPrimary,
            SortOrder = request.SortOrder
        };

        List<ProjectImage> demoted = isPrimary ? OtherPrimaries(project, image.Id) : [];

        await SaveAsync(
            demote: demoted,
            then: () => _db.ProjectImages.Add(image),
            cancellationToken);

        return ServiceResult<ProjectImageResponse>.Success(ToImageResponse(image));
    }

    public async Task<ServiceResult<ProjectImageResponse>> UpdateImageAsync(
        Guid projectId,
        Guid imageId,
        UpdateProjectImageRequest request,
        CancellationToken cancellationToken)
    {
        var project = await LoadWithImagesAsync(projectId, cancellationToken);
        if (project is null)
        {
            return ImageProjectNotFound(projectId);
        }

        var image = project.Images.FirstOrDefault(i => i.Id == imageId);
        if (image is null)
        {
            return ImageNotFound(projectId, imageId);
        }

        var objectKey = Blank(request.ObjectKey);
        var url = Blank(request.Url);
        var altText = Blank(request.AltText);

        var validationError = ValidateImage(objectKey, url, altText, request);
        if (validationError is not null)
        {
            return ServiceResult<ProjectImageResponse>.Validation(validationError);
        }

        // Clearing it on the only image would leave no primary, so it stays put.
        var isPrimary = request.IsPrimary || project.Images.Count == 1;

        image.ObjectKey = objectKey!;
        image.Url = url!;
        image.AltText = altText!;
        image.Width = request.Width;
        image.Height = request.Height;
        image.SortOrder = request.SortOrder;

        List<ProjectImage> demoted = isPrimary ? OtherPrimaries(project, image.Id) : [];

        await SaveAsync(
            demote: demoted,
            then: () => image.IsPrimary = isPrimary,
            cancellationToken);

        return ServiceResult<ProjectImageResponse>.Success(ToImageResponse(image));
    }

    public async Task<ServiceResult<bool>> DeleteImageAsync(
        Guid projectId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        var project = await LoadWithImagesAsync(projectId, cancellationToken);
        if (project is null)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No project with id {projectId}.");
        }

        var image = project.Images.FirstOrDefault(i => i.Id == imageId);
        if (image is null)
        {
            return ServiceResult<bool>.NotFound(
                "not_found",
                $"No image with id {imageId} on project {projectId}.");
        }

        // Hard delete — no soft-delete flag, so the storage asset goes too (below, once the row is gone).
        project.Images.Remove(image);
        _db.ProjectImages.Remove(image);

        // Promote the next image so exactly one primary remains. Delete must land first, or the
        // partial unique index sees two primaries.
        var successor = image.IsPrimary
            ? project.Images.OrderBy(i => i.SortOrder).FirstOrDefault()
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

        // After the commit, never before: a destroy against a rolled-back delete leaves a row
        // pointing at nothing. The reverse only orphans an asset, so it must not fail the request.
        await _mediaService.DeleteFileAsync(image.ObjectKey, cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> ReorderImagesAsync(
        Guid projectId,
        ImageReorderRequest request,
        CancellationToken cancellationToken)
    {
        var project = await LoadWithImagesAsync(projectId, cancellationToken);
        if (project is null)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No project with id {projectId}.");
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

        var images = project.Images.ToDictionary(i => i.Id);

        var missing = ids.Where(id => !images.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            return ServiceResult<bool>.NotFound(
                "not_found",
                $"No image with id {string.Join(", ", missing)} on project {projectId}.");
        }

        foreach (var item in items)
        {
            images[item.Id].SortOrder = item.SortOrder;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    private static IQueryable<Project> ForSite(IQueryable<Project> query, Site site) =>
        site == Site.Agency
            ? query.Where(p => p.ShowOnAgency)
            : query.Where(p => p.ShowOnPersonal);

    private static IOrderedQueryable<Project> OrderForSite(IQueryable<Project> query, Site site) =>
        site == Site.Agency
            ? query.OrderBy(p => p.AgencySortOrder)
                .ThenByDescending(p => p.Year)
                .ThenByDescending(p => p.PublishedAt)
            : query.OrderBy(p => p.PersonalSortOrder)
                .ThenByDescending(p => p.Year)
                .ThenByDescending(p => p.PublishedAt);

    /// <summary>Null when the project is valid, otherwise the message to hand back.</summary>
    private static string? Validate(
        string? title,
        string? shortDescription,
        string? description,
        string? websiteUrl,
        CreateProjectRequest request)
    {
        if (title is null)
        {
            return "Title is required.";
        }

        if (shortDescription is null)
        {
            return "shortDescription is required.";
        }

        if (description is null)
        {
            return "Description is required.";
        }

        if (websiteUrl is not null && !IsAbsoluteHttpUrl(websiteUrl))
        {
            return "websiteUrl must be an absolute http(s) URL.";
        }

        var latestYear = DateTimeOffset.UtcNow.Year + 1;
        if (request.Year < EarliestYear || request.Year > latestYear)
        {
            return $"Year must be between {EarliestYear} and {latestYear}.";
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

    /// <summary>Null when the image is valid, otherwise the message to hand back.</summary>
    private static string? ValidateImage(
        string? objectKey,
        string? url,
        string? altText,
        AddProjectImageRequest request)
    {
        if (objectKey is null)
        {
            return "objectKey is required.";
        }

        if (url is null || !IsAbsoluteHttpUrl(url))
        {
            return "url is required and must be an absolute http(s) URL.";
        }

        // Every image is described — a blank alt text fails the request rather than warning.
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

    /// <summary>
    /// Going live stamps published_at the first time only; unpublishing never clears it, so a
    /// project that comes back keeps its original date.
    /// </summary>
    private static void ApplyPublished(Project project, bool isPublished)
    {
        if (isPublished && project.PublishedAt is null)
        {
            project.PublishedAt = DateTimeOffset.UtcNow;
        }

        project.IsPublished = isPublished;
    }

    /// <summary>Null when every id resolves to a live tag, otherwise the message to hand back.</summary>
    private async Task<string?> UnknownTagIdsAsync(IReadOnlyList<Guid> tagIds, CancellationToken cancellationToken)
    {
        if (tagIds.Count == 0)
        {
            return null;
        }

        var found = await _db.Tags
            .Where(t => tagIds.Contains(t.Id))
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        var missing = tagIds.Except(found).ToList();

        return missing.Count == 0 ? null : $"Unknown tag id(s): {string.Join(", ", missing)}.";
    }

    /// <summary>Adds and removes only what changed, so untouched links keep their row.</summary>
    private void SyncTags(Project project, IReadOnlyList<Guid> tagIds)
    {
        foreach (var link in project.ProjectTags.Where(pt => !tagIds.Contains(pt.TagId)).ToList())
        {
            project.ProjectTags.Remove(link);
            _db.Remove(link);
        }

        foreach (var tagId in tagIds.Where(id => project.ProjectTags.All(pt => pt.TagId != id)))
        {
            project.ProjectTags.Add(new ProjectTag { ProjectId = project.Id, TagId = tagId });
        }
    }

    private Task<Project?> LoadWithImagesAsync(Guid projectId, CancellationToken cancellationToken) =>
        _db.Projects
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

    private static List<ProjectImage> OtherPrimaries(Project project, Guid keepId) =>
        [.. project.Images.Where(i => i.IsPrimary && i.Id != keepId)];

    /// <summary>
    /// Clears the images in <paramref name="demote"/> before applying <paramref name="then"/>.
    /// The partial unique index on <c>(project_id) where is_primary</c> is checked per statement,
    /// so the old primary has to be cleared in its own round trip — hence two saves in one
    /// transaction. With nothing to demote it is a single ordinary save.
    /// </summary>
    private Task SaveAsync(List<ProjectImage> demote, Action then, CancellationToken cancellationToken)
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

    /// <summary>
    /// Saves twice — once after <paramref name="first"/>, once after <paramref name="second"/> —
    /// inside a single transaction, so an ordering constraint can be respected without the caller
    /// seeing a half-applied change. Wrapped in the execution strategy because the connection
    /// retries on failure.
    /// </summary>
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

    private static IReadOnlyList<Guid> Distinct(List<Guid>? tagIds) =>
        tagIds is null ? [] : [.. tagIds.Distinct()];

    private static string ResolveSlug(string? requestedSlug, string title) =>
        SlugGenerator.Generate(string.IsNullOrWhiteSpace(requestedSlug) ? title : requestedSlug);

    private Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken) =>
        _db.Projects.AnyAsync(p => p.Slug == slug && (excludingId == null || p.Id != excludingId), cancellationToken);

    private static ServiceResult<AdminProjectResponse> NotFound(Guid id) =>
        ServiceResult<AdminProjectResponse>.NotFound("not_found", $"No project with id {id}.");

    private static ServiceResult<AdminProjectResponse> SlugTaken(string slug) =>
        ServiceResult<AdminProjectResponse>.Conflict("slug_taken", $"Slug '{slug}' is already in use.");

    private static ServiceResult<ProjectImageResponse> ImageProjectNotFound(Guid projectId) =>
        ServiceResult<ProjectImageResponse>.NotFound("not_found", $"No project with id {projectId}.");

    private static ServiceResult<ProjectImageResponse> ImageNotFound(Guid projectId, Guid imageId) =>
        ServiceResult<ProjectImageResponse>.NotFound(
            "not_found",
            $"No image with id {imageId} on project {projectId}.");

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ProjectImageResponse ToImageResponse(ProjectImage image) => new()
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

    /// <summary>
    /// Projected inside the query, tags and images and all, so a list page is one round trip
    /// rather than one query per project. The site picks which visibility pair is exposed.
    /// </summary>
    private static Expression<Func<Project, ProjectResponse>> PublicProjection(Site site)
    {
        if (site == Site.Agency)
        {
            return p => new ProjectResponse
            {
                Id = p.Id,
                Slug = p.Slug,
                Title = p.Title,
                Year = p.Year,
                ShortDescription = p.ShortDescription,
                Description = p.Description,
                WebsiteUrl = p.WebsiteUrl,
                Problem = p.Problem,
                Solution = p.Solution,
                WhatWeDelivered = p.WhatWeDelivered,
                Proof = p.Proof,
                ClientName = p.ClientName,
                PublishedAt = p.PublishedAt,
                SeoTitle = p.SeoTitle,
                SeoDescription = p.SeoDescription,
                Featured = p.FeaturedOnAgency,
                SortOrder = p.AgencySortOrder,
                Tags = p.ProjectTags
                    .Where(pt => !pt.Tag.IsDeleted)
                    .OrderBy(pt => pt.Tag.Name)
                    .Select(pt => new TagResponse
                    {
                        Id = pt.Tag.Id,
                        Name = pt.Tag.Name,
                        Slug = pt.Tag.Slug,
                        IsTechnology = pt.Tag.IsTechnology,
                        TechnologyCategory = pt.Tag.TechnologyCategory
                    })
                    .ToList(),
                Images = p.Images
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.SortOrder)
                    .Select(i => new ProjectImageResponse
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

        return p => new ProjectResponse
        {
            Id = p.Id,
            Slug = p.Slug,
            Title = p.Title,
            Year = p.Year,
            ShortDescription = p.ShortDescription,
            Description = p.Description,
            WebsiteUrl = p.WebsiteUrl,
            Problem = p.Problem,
            Solution = p.Solution,
            WhatWeDelivered = p.WhatWeDelivered,
            Proof = p.Proof,
            ClientName = p.ClientName,
            PublishedAt = p.PublishedAt,
            SeoTitle = p.SeoTitle,
            SeoDescription = p.SeoDescription,
            Featured = p.FeaturedOnPersonal,
            SortOrder = p.PersonalSortOrder,
            Tags = p.ProjectTags
                .Where(pt => !pt.Tag.IsDeleted)
                .OrderBy(pt => pt.Tag.Name)
                .Select(pt => new TagResponse
                {
                    Id = pt.Tag.Id,
                    Name = pt.Tag.Name,
                    Slug = pt.Tag.Slug,
                    IsTechnology = pt.Tag.IsTechnology,
                    TechnologyCategory = pt.Tag.TechnologyCategory
                })
                .ToList(),
            Images = p.Images
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.SortOrder)
                .Select(i => new ProjectImageResponse
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

    private static readonly Expression<Func<Project, AdminProjectResponse>> AdminProjection = p => new AdminProjectResponse
    {
        Id = p.Id,
        Slug = p.Slug,
        Title = p.Title,
        Year = p.Year,
        ShortDescription = p.ShortDescription,
        Description = p.Description,
        WebsiteUrl = p.WebsiteUrl,
        Problem = p.Problem,
        Solution = p.Solution,
        WhatWeDelivered = p.WhatWeDelivered,
        Proof = p.Proof,
        ClientName = p.ClientName,
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
        Tags = p.ProjectTags
            .Where(pt => !pt.Tag.IsDeleted)
            .OrderBy(pt => pt.Tag.Name)
            .Select(pt => new AdminTagResponse
            {
                Id = pt.Tag.Id,
                Name = pt.Tag.Name,
                Slug = pt.Tag.Slug,
                IsTechnology = pt.Tag.IsTechnology,
                TechnologyCategory = pt.Tag.TechnologyCategory,
                CreatedAt = pt.Tag.CreatedAt,
                UpdatedAt = pt.Tag.UpdatedAt
            })
            .ToList(),
        Images = p.Images
            .OrderByDescending(i => i.IsPrimary)
            .ThenBy(i => i.SortOrder)
            .Select(i => new ProjectImageResponse
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
