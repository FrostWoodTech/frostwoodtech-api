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

public class PricingService : IPricingService
{
    private readonly FrostWoodTechDbContext _db;

    public PricingService(FrostWoodTechDbContext db)
    {
        _db = db;
    }

    public Task<PagedResult<PricingPlanResponse>> GetPublicComboPlansAsync(
        bool? featured,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // A combo pack is a plan that belongs to no single service.
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

        // Admin defaults to everything; these narrow it as explicit filters, not an absent parameter.
        // comboOnly wins over both if somehow sent together with tiersOnly/serviceId.
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
            // Every service's tiers, regardless of which one — distinct from picking one serviceId.
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

        // Drafts have no meaningful order, so the admin list is alphabetical instead.
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

        // Sort order is never taken from the client — it's only ever changed via ReorderAsync,
        // so a new plan is appended to the end of its own group's order (combo packs together,
        // a service's tiers together), not the whole table's.
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

        // Re-read so the response shape matches every other admin read (feature list empty here).
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

        // Unlike services, pricing_plans has no published_at column — there is nothing to stamp.
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

        // Soft delete: the feature rows stay put so restoring the plan keeps them.
        plan.IsDeleted = true;
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

        // Scoped to the parent, so a feature id borrowed from another plan is a 404.
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

        // Hard delete — the row carries no soft-delete flag.
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

    /// <summary>
    /// The only entry point the public routes use: is_deleted is handled by the DbContext's
    /// global filter, and is_published is applied here and is not optional.
    /// </summary>
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

    /// <summary>Null when the plan is valid, otherwise the message to hand back.</summary>
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

        // char(3) column — a longer value would surface as a database error, not a readable one.
        if (currency.Length != 3 || !currency.All(char.IsAsciiLetter))
        {
            return "Currency must be a 3-letter ISO 4217 code, e.g. 'LKR'.";
        }

        // Null is meaningful here ("Custom / Contact us"), so only a supplied amount is checked.
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

    /// <summary>
    /// A bad service id is a validation failure, not a foreign key violation surfacing as a 500.
    /// Null is legal and means a combo pack.
    /// </summary>
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

    /// <summary>Trimmed, or null when the caller sent nothing meaningful.</summary>
    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static PricingPlanFeatureResponse ToFeatureResponse(PricingPlanFeature feature) => new()
    {
        Id = feature.Id,
        Text = feature.Text,
        IsIncluded = feature.IsIncluded,
        SortOrder = feature.SortOrder
    };

    /// <summary>
    /// Projected inside the query, features and all, so a pricing page is one round trip rather
    /// than one query per card.
    /// </summary>
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
