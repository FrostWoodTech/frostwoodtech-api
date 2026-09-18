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

public class PricingService : IPricingService
{
    private readonly FrostWoodTechDbContext _db;
    private readonly CurrentUser _currentUser;

    public PricingService(FrostWoodTechDbContext db, CurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<PagedResult<PricingPlanResponse>> GetPublicComboPlansAsync(
        bool? featured,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // Combo pack = a plan with no service.
        var query = PublishedPlans().Where(p => p.ServiceId == null);

        if (featured is not null)
        {
            query = query.Where(p => p.Featured == featured);
        }

        return PublicPageAsync(query, page, pageSize, cancellationToken);
    }

    public Task<PagedResult<PricingPlanResponse>> GetPublicPlansForServiceAsync(
        Guid serviceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = PublishedPlans().Where(p => p.ServiceId == serviceId);

        return PublicPageAsync(query, page, pageSize, cancellationToken);
    }

    public async Task<PagedResult<AdminPricingPlanResponse>> GetAdminPlansAsync(
        Guid? serviceId,
        bool comboOnly,
        bool tiersOnly,
        bool? isPublished,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.PricingPlans.AsNoTracking();

        // Explicit filters; comboOnly wins over tiersOnly and serviceId.
        if (comboOnly)
        {
            query = query.Where(p => p.ServiceId == null);
        }
        else if (serviceId is not null)
        {
            query = query.Where(p => p.ServiceId == serviceId);
        }
        else if (tiersOnly)
        {
            query = query.Where(p => p.ServiceId != null);
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
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(AdminProjection)
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminPricingPlanResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<ServiceResult<AdminPricingPlanResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var plan = await _db.PricingPlans
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(AdminProjection)
            .FirstOrDefaultAsync(cancellationToken);

        return plan is null ? NotFound(id) : ServiceResult<AdminPricingPlanResponse>.Success(plan);
    }

    public async Task<ServiceResult<AdminPricingPlanResponse>> CreateAsync(
        CreatePricingPlanRequest request,
        CancellationToken cancellationToken)
    {
        var name = Blank(request.Name);
        var description = Blank(request.Description);
        var currency = Blank(request.Currency)?.ToUpperInvariant();

        var validationError = Validate(name, description, currency, request);
        if (validationError is not null)
        {
            return ServiceResult<AdminPricingPlanResponse>.Validation(validationError);
        }

        var serviceError = await ValidateServiceAsync(request.ServiceId, cancellationToken);
        if (serviceError is not null)
        {
            return ServiceResult<AdminPricingPlanResponse>.Validation(serviceError);
        }

        // New plans go to the end of their group's order (combos, or one service's tiers).
        var nextSortOrder = await _db.PricingPlans
            .Where(p => p.ServiceId == request.ServiceId)
            .MaxAsync(p => (int?)p.SortOrder, cancellationToken) + 1 ?? 0;

        var plan = new PricingPlan
        {
            Id = Guid.NewGuid(),
            ServiceId = request.ServiceId,
            Name = name!,
            Tagline = Blank(request.Tagline),
            PriceAmount = request.PriceAmount,
            Currency = currency!,
            PriceType = request.PriceType,
            DeliveryText = Blank(request.DeliveryText),
            Description = description!,
            IsPopular = request.IsPopular,
            CtaLabel = Blank(request.CtaLabel),
            CtaUrl = Blank(request.CtaUrl),
            IsPublished = request.IsPublished,
            Featured = request.Featured,
            SortOrder = nextSortOrder
        };

        _db.PricingPlans.Add(plan);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(plan.Id, cancellationToken);
    }

    public async Task<ServiceResult<AdminPricingPlanResponse>> UpdateAsync(
        Guid id,
        UpdatePricingPlanRequest request,
        CancellationToken cancellationToken)
    {
        var plan = await _db.PricingPlans.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (plan is null)
        {
            return NotFound(id);
        }

        var name = Blank(request.Name);
        var description = Blank(request.Description);
        var currency = Blank(request.Currency)?.ToUpperInvariant();

        var validationError = Validate(name, description, currency, request);
        if (validationError is not null)
        {
            return ServiceResult<AdminPricingPlanResponse>.Validation(validationError);
        }

        var serviceError = await ValidateServiceAsync(request.ServiceId, cancellationToken);
        if (serviceError is not null)
        {
            return ServiceResult<AdminPricingPlanResponse>.Validation(serviceError);
        }

        plan.ServiceId = request.ServiceId;
        plan.Name = name!;
        plan.Tagline = Blank(request.Tagline);
        plan.PriceAmount = request.PriceAmount;
        plan.Currency = currency!;
        plan.PriceType = request.PriceType;
        plan.DeliveryText = Blank(request.DeliveryText);
        plan.Description = description!;
        plan.IsPopular = request.IsPopular;
        plan.CtaLabel = Blank(request.CtaLabel);
        plan.CtaUrl = Blank(request.CtaUrl);
        plan.IsPublished = request.IsPublished;
        plan.Featured = request.Featured;

        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(plan.Id, cancellationToken);
    }

    public async Task<ServiceResult<AdminPricingPlanResponse>> SetPublishedAsync(
        Guid id,
        SetPublishedRequest request,
        CancellationToken cancellationToken)
    {
        var plan = await _db.PricingPlans.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (plan is null)
        {
            return NotFound(id);
        }

        // pricing_plans has no published_at column.
        plan.IsPublished = request.IsPublished;

        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(plan.Id, cancellationToken);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var plan = await _db.PricingPlans.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (plan is null)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No pricing plan with id {id}.");
        }

        // Soft delete keeps feature rows.
        plan.IsDeleted = true;
        plan.DeletedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<PagedResult<TrashedItemResponse>> GetTrashAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.PricingPlans.Trashed().AsNoTracking();

        if (search is not null)
        {
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.DeletedAt)
            .ThenByDescending(p => p.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new TrashedItemResponse
            {
                Id = p.Id,
                Label = p.Name,
                DeletedAt = p.DeletedAt,
                DeletedBy = p.DeletedBy,
                DeletedByEmail = _db.Users.Where(u => u.Id == p.DeletedBy).Select(u => u.Email).FirstOrDefault()
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

    public async Task<ServiceResult<AdminPricingPlanResponse>> RestoreAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var plan = await _db.PricingPlans.FindTrashedAsync(id, cancellationToken);
        if (plan is null)
        {
            return ServiceResult<AdminPricingPlanResponse>.NotFound(
                "not_found", $"No deleted pricing plan with id {id}.");
        }

        // Deleting a service keeps its plans, so a tier can outlive the page it belongs to.
        if (plan.ServiceId is { } serviceId
            && !await _db.Services.AnyAsync(s => s.Id == serviceId, cancellationToken))
        {
            return ServiceResult<AdminPricingPlanResponse>.Conflict(
                "service_deleted", "This plan belongs to a deleted service. Restore the service first.");
        }

        // Only a deleted currency row blocks this; a code with no row at all is what create allows too.
        if (await _db.Currencies
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Code == plan.Currency && c.IsDeleted, cancellationToken))
        {
            return ServiceResult<AdminPricingPlanResponse>.Conflict(
                "currency_deleted",
                $"Currency '{plan.Currency}' no longer exists. Restore it or move the plan first.");
        }

        plan.IsDeleted = false;
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ServiceResult<bool>> PurgeAsync(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.RequireSuperAdmin<bool>() is { } denied)
        {
            return denied;
        }

        var plan = await _db.PricingPlans.FindTrashedAsync(id, cancellationToken);
        if (plan is null)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No deleted pricing plan with id {id}.");
        }

        _db.PricingPlans.Remove(plan);
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> ReorderAsync(
        PricingReorderRequest request,
        CancellationToken cancellationToken)
    {
        var items = request.Items;
        if (items is null || items.Count == 0)
        {
            return ServiceResult<bool>.Validation("At least one item is required.");
        }

        var ids = items.Select(i => i.Id).ToList();
        if (ids.Distinct().Count() != ids.Count)
        {
            return ServiceResult<bool>.Validation("The same pricing plan id appears more than once.");
        }

        var plans = await _db.PricingPlans
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var missing = ids.Where(id => !plans.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            return ServiceResult<bool>.NotFound(
                "not_found",
                $"No pricing plan with id {string.Join(", ", missing)}.");
        }

        foreach (var item in items)
        {
            plans[item.Id].SortOrder = item.SortOrder;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<PricingPlanFeatureResponse>> AddFeatureAsync(
        Guid planId,
        AddPricingPlanFeatureRequest request,
        CancellationToken cancellationToken)
    {
        var plan = await LoadWithFeaturesAsync(planId, cancellationToken);
        if (plan is null)
        {
            return FeaturePlanNotFound(planId);
        }

        var text = Blank(request.Text);
        if (text is null)
        {
            return ServiceResult<PricingPlanFeatureResponse>.Validation("Text is required.");
        }

        var feature = new PricingPlanFeature
        {
            Id = Guid.NewGuid(),
            PricingPlanId = plan.Id,
            Text = text,
            IsIncluded = request.IsIncluded,
            SortOrder = request.SortOrder
        };

        _db.PricingPlanFeatures.Add(feature);
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<PricingPlanFeatureResponse>.Success(ToFeatureResponse(feature));
    }

    public async Task<ServiceResult<PricingPlanFeatureResponse>> UpdateFeatureAsync(
        Guid planId,
        Guid featureId,
        UpdatePricingPlanFeatureRequest request,
        CancellationToken cancellationToken)
    {
        var plan = await LoadWithFeaturesAsync(planId, cancellationToken);
        if (plan is null)
        {
            return FeaturePlanNotFound(planId);
        }

        // Scoped to the parent plan: another plan's feature id is a 404.
        var feature = plan.Features.FirstOrDefault(f => f.Id == featureId);
        if (feature is null)
        {
            return ServiceResult<PricingPlanFeatureResponse>.NotFound(
                "not_found",
                $"No feature with id {featureId} on pricing plan {planId}.");
        }

        var text = Blank(request.Text);
        if (text is null)
        {
            return ServiceResult<PricingPlanFeatureResponse>.Validation("Text is required.");
        }

        feature.Text = text;
        feature.IsIncluded = request.IsIncluded;
        feature.SortOrder = request.SortOrder;

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<PricingPlanFeatureResponse>.Success(ToFeatureResponse(feature));
    }

    public async Task<ServiceResult<bool>> DeleteFeatureAsync(
        Guid planId,
        Guid featureId,
        CancellationToken cancellationToken)
    {
        var plan = await LoadWithFeaturesAsync(planId, cancellationToken);
        if (plan is null)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No pricing plan with id {planId}.");
        }

        var feature = plan.Features.FirstOrDefault(f => f.Id == featureId);
        if (feature is null)
        {
            return ServiceResult<bool>.NotFound(
                "not_found",
                $"No feature with id {featureId} on pricing plan {planId}.");
        }

        plan.Features.Remove(feature);
        _db.PricingPlanFeatures.Remove(feature);

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> ReorderFeaturesAsync(
        Guid planId,
        FeatureReorderRequest request,
        CancellationToken cancellationToken)
    {
        var plan = await LoadWithFeaturesAsync(planId, cancellationToken);
        if (plan is null)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No pricing plan with id {planId}.");
        }

        var items = request.Items;
        if (items is null || items.Count == 0)
        {
            return ServiceResult<bool>.Validation("At least one item is required.");
        }

        var ids = items.Select(i => i.Id).ToList();
        if (ids.Distinct().Count() != ids.Count)
        {
            return ServiceResult<bool>.Validation("The same feature id appears more than once.");
        }

        var features = plan.Features.ToDictionary(f => f.Id);

        var missing = ids.Where(id => !features.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            return ServiceResult<bool>.NotFound(
                "not_found",
                $"No feature with id {string.Join(", ", missing)} on pricing plan {planId}.");
        }

        foreach (var item in items)
        {
            features[item.Id].SortOrder = item.SortOrder;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    // Only entry point for public reads; is_published is mandatory here.
    private IQueryable<PricingPlan> PublishedPlans() =>
        _db.PricingPlans.AsNoTracking().Where(p => p.IsPublished);

    private static async Task<PagedResult<PricingPlanResponse>> PublicPageAsync(
        IQueryable<PricingPlan> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(PublicProjection)
            .ToListAsync(cancellationToken);

        return new PagedResult<PricingPlanResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    private static string? Validate(
        string? name,
        string? description,
        string? currency,
        CreatePricingPlanRequest request)
    {
        if (name is null)
        {
            return "Name is required.";
        }

        if (description is null)
        {
            return "Description is required.";
        }

        if (currency is null)
        {
            return "Currency is required.";
        }

        // char(3) column: reject here instead of a database error.
        if (currency.Length != 3 || !currency.All(char.IsAsciiLetter))
        {
            return "Currency must be a 3-letter ISO 4217 code, e.g. 'LKR'.";
        }

        // Null means "Custom / Contact us"; only a supplied amount is checked.
        if (request.PriceAmount < 0)
        {
            return "priceAmount cannot be negative.";
        }

        if (request.PriceType == PriceType.Custom && request.PriceAmount is not null)
        {
            return "A custom price cannot carry a priceAmount — leave it null for 'Contact us'.";
        }

        return null;
    }

    // Unknown service is a validation error, not an FK 500. Null means a combo pack.
    private async Task<string?> ValidateServiceAsync(Guid? serviceId, CancellationToken cancellationToken)
    {
        if (serviceId is null)
        {
            return null;
        }

        var exists = await _db.Services.AnyAsync(s => s.Id == serviceId, cancellationToken);

        return exists ? null : $"No service with id {serviceId}. Leave serviceId null for a combo pack.";
    }

    private Task<PricingPlan?> LoadWithFeaturesAsync(Guid planId, CancellationToken cancellationToken) =>
        _db.PricingPlans
            .Include(p => p.Features)
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);

    private static ServiceResult<AdminPricingPlanResponse> NotFound(Guid id) =>
        ServiceResult<AdminPricingPlanResponse>.NotFound("not_found", $"No pricing plan with id {id}.");

    private static ServiceResult<PricingPlanFeatureResponse> FeaturePlanNotFound(Guid planId) =>
        ServiceResult<PricingPlanFeatureResponse>.NotFound("not_found", $"No pricing plan with id {planId}.");

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static PricingPlanFeatureResponse ToFeatureResponse(PricingPlanFeature feature) => new()
    {
        Id = feature.Id,
        Text = feature.Text,
        IsIncluded = feature.IsIncluded,
        SortOrder = feature.SortOrder
    };

    private static readonly Expression<Func<PricingPlan, PricingPlanResponse>> PublicProjection = p => new PricingPlanResponse
    {
        Id = p.Id,
        ServiceId = p.ServiceId,
        Name = p.Name,
        Tagline = p.Tagline,
        PriceAmount = p.PriceAmount,
        Currency = p.Currency,
        PriceType = p.PriceType,
        DeliveryText = p.DeliveryText,
        Description = p.Description,
        IsPopular = p.IsPopular,
        CtaLabel = p.CtaLabel,
        CtaUrl = p.CtaUrl,
        Featured = p.Featured,
        SortOrder = p.SortOrder,
        Features = p.Features
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Text)
            .Select(f => new PricingPlanFeatureResponse
            {
                Id = f.Id,
                Text = f.Text,
                IsIncluded = f.IsIncluded,
                SortOrder = f.SortOrder
            })
            .ToList()
    };

    private static readonly Expression<Func<PricingPlan, AdminPricingPlanResponse>> AdminProjection = p => new AdminPricingPlanResponse
    {
        Id = p.Id,
        ServiceId = p.ServiceId,
        Name = p.Name,
        Tagline = p.Tagline,
        PriceAmount = p.PriceAmount,
        Currency = p.Currency,
        PriceType = p.PriceType,
        DeliveryText = p.DeliveryText,
        Description = p.Description,
        IsPopular = p.IsPopular,
        CtaLabel = p.CtaLabel,
        CtaUrl = p.CtaUrl,
        IsPublished = p.IsPublished,
        Featured = p.Featured,
        SortOrder = p.SortOrder,
        Features = p.Features
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Text)
            .Select(f => new PricingPlanFeatureResponse
            {
                Id = f.Id,
                Text = f.Text,
                IsIncluded = f.IsIncluded,
                SortOrder = f.SortOrder
            })
            .ToList(),
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
