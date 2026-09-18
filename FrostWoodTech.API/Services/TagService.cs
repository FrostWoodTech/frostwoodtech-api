using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using FrostWoodTech.API.Auth;
using FrostWoodTech.API.Common;
using FrostWoodTech.API.Data;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;
using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Services;

public class TagService : ITagService
{
    private readonly FrostWoodTechDbContext _db;
    private readonly CurrentUser _currentUser;

    public TagService(FrostWoodTechDbContext db, CurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<TagResponse>> GetPublicTagsAsync(
        bool? isTechnology,
        TechCategory? category,
        CancellationToken cancellationToken)
    {
        // Not paged: a small lookup set grouped client-side.
        return await FilterTags(_db.Tags.AsNoTracking(), isTechnology, category)
            .OrderBy(t => t.Name)
            .Select(t => new TagResponse
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                IsTechnology = t.IsTechnology,
                TechnologyCategory = t.TechnologyCategory
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<AdminTagResponse>> GetAdminTagsAsync(
        bool? isTechnology,
        TechCategory? category,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = FilterTags(_db.Tags.AsNoTracking(), isTechnology, category);

        if (search is not null)
        {
            query = query.Where(t => EF.Functions.ILike(t.Name, $"%{search}%"));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(AdminProjection)
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminTagResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<ServiceResult<AdminTagResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var tag = await _db.Tags
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(AdminProjection)
            .FirstOrDefaultAsync(cancellationToken);

        return tag is null ? NotFound(id) : ServiceResult<AdminTagResponse>.Success(tag);
    }

    public async Task<ServiceResult<AdminTagResponse>> CreateAsync(
        CreateTagRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim();

        var validationError = Validate(name, request.IsTechnology, request.TechnologyCategory);

        if (validationError is not null)
        {
            return ServiceResult<AdminTagResponse>.Validation(validationError);
        }

        var slug = ResolveSlug(request.Slug, name!);
        if (slug.Length == 0)
        {
            return ServiceResult<AdminTagResponse>.Validation("A slug could not be generated; provide one with letters or digits.");
        }

        if (await SlugExistsAsync(slug, excludingId: null, cancellationToken))
        {
            return SlugTaken(slug);
        }

        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            Name = name!,
            Slug = slug,
            IsTechnology = request.IsTechnology,
            TechnologyCategory = request.TechnologyCategory
        };

        _db.Tags.Add(tag);
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<AdminTagResponse>.Success(ToAdminResponse(tag));
    }

    public async Task<ServiceResult<AdminTagResponse>> UpdateAsync(
        Guid id,
        UpdateTagRequest request,
        CancellationToken cancellationToken)
    {
        var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tag is null)
        {
            return NotFound(id);
        }

        var name = request.Name?.Trim();

        var validationError = Validate(name, request.IsTechnology, request.TechnologyCategory);

        if (validationError is not null)
        {
            return ServiceResult<AdminTagResponse>.Validation(validationError);
        }

        var slug = ResolveSlug(request.Slug, name!);
        if (slug.Length == 0)
        {
            return ServiceResult<AdminTagResponse>.Validation("A slug could not be generated; provide one with letters or digits.");
        }

        if (await SlugExistsAsync(slug, excludingId: id, cancellationToken))
        {
            return SlugTaken(slug);
        }

        tag.Name = name!;
        tag.Slug = slug;
        tag.IsTechnology = request.IsTechnology;
        tag.TechnologyCategory = request.TechnologyCategory;

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<AdminTagResponse>.Success(ToAdminResponse(tag));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tag is null)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No tag with id {id}.");
        }

        // Counted through Projects/Articles so soft-deleted content doesn't block deletion.
        var projectCount = await _db.Projects
            .CountAsync(p => p.ProjectTags.Any(pt => pt.TagId == id), cancellationToken);

        var articleCount = await _db.Articles
            .CountAsync(a => a.ArticleTags.Any(at => at.TagId == id), cancellationToken);

        if (projectCount > 0 || articleCount > 0)
        {
            return ServiceResult<bool>.Conflict(
                "tag_in_use",
                $"Tag '{tag.Name}' is used by {projectCount} project(s) and {articleCount} article(s). "
                + "Remove it from them before deleting.");
        }

        tag.IsDeleted = true;
        tag.DeletedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<PagedResult<TrashedItemResponse>> GetTrashAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.Tags.Trashed().AsNoTracking();

        if (search is not null)
        {
            query = query.Where(t => EF.Functions.ILike(t.Name, $"%{search}%"));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.DeletedAt)
            .ThenByDescending(t => t.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TrashedItemResponse
            {
                Id = t.Id,
                Label = t.Name,
                DeletedAt = t.DeletedAt,
                DeletedBy = t.DeletedBy,
                DeletedByEmail = _db.Users.Where(u => u.Id == t.DeletedBy).Select(u => u.Email).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<TrashedItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<ServiceResult<AdminTagResponse>> RestoreAsync(Guid id, CancellationToken cancellationToken)
    {
        var tag = await _db.Tags.FindTrashedAsync(id, cancellationToken);
        if (tag is null)
        {
            return ServiceResult<AdminTagResponse>.NotFound("not_found", $"No deleted tag with id {id}.");
        }

        tag.IsDeleted = false;
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ServiceResult<bool>> PurgeAsync(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.RequireSuperAdmin<bool>() is { } denied)
        {
            return denied;
        }

        var tag = await _db.Tags.FindTrashedAsync(id, cancellationToken);
        if (tag is null)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No deleted tag with id {id}.");
        }

        // Unlike the soft delete, deleted content counts: its links are Restrict, and dropping one would
        // strip the tag on restore.
        var projectCount = await _db.ProjectTags.CountAsync(pt => pt.TagId == id, cancellationToken);
        var articleCount = await _db.ArticleTags.CountAsync(at => at.TagId == id, cancellationToken);

        if (projectCount > 0 || articleCount > 0)
        {
            return ServiceResult<bool>.Conflict(
                "tag_in_use",
                $"Tag '{tag.Name}' is still linked to {projectCount} project(s) and {articleCount} article(s), "
                + "including deleted ones. Remove it from them before deleting permanently.");
        }

        _db.Tags.Remove(tag);
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    private static IQueryable<Tag> FilterTags(IQueryable<Tag> query, bool? isTechnology, TechCategory? category)
    {
        if (isTechnology is not null)
        {
            query = query.Where(t => t.IsTechnology == isTechnology);
        }

        if (category is not null)
        {
            query = query.Where(t => t.TechnologyCategory == category);
        }

        return query;
    }

    private static string? Validate(string? name, bool isTechnology, TechCategory? technologyCategory)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required.";
        }

        if (isTechnology)
        {
            if (technologyCategory is null)
            {
                return "technologyCategory is required when isTechnology is true.";
            }
        }
        else if (technologyCategory is not null)
        {
            return "technologyCategory must be null when isTechnology is false.";
        }

        return null;
    }

    private static string ResolveSlug(string? requestedSlug, string name) =>
        SlugGenerator.Generate(string.IsNullOrWhiteSpace(requestedSlug) ? name : requestedSlug);

    private Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken) =>
        _db.Tags.IgnoreQueryFilters().AnyAsync(t => t.Slug == slug && (excludingId == null || t.Id != excludingId), cancellationToken);

    private static ServiceResult<AdminTagResponse> NotFound(Guid id) =>
        ServiceResult<AdminTagResponse>.NotFound("not_found", $"No tag with id {id}.");

    private static ServiceResult<AdminTagResponse> SlugTaken(string slug) =>
        ServiceResult<AdminTagResponse>.Conflict("slug_taken", $"Slug '{slug}' is already in use.");

    private static readonly Expression<Func<Tag, AdminTagResponse>> AdminProjection = tag => new AdminTagResponse
    {
        Id = tag.Id,
        Name = tag.Name,
        Slug = tag.Slug,
        IsTechnology = tag.IsTechnology,
        TechnologyCategory = tag.TechnologyCategory,
        CreatedAt = tag.CreatedAt,
        UpdatedAt = tag.UpdatedAt
    };

    private static readonly Func<Tag, AdminTagResponse> ToAdminResponse = AdminProjection.Compile();
}
