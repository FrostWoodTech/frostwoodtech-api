using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;

namespace FrostWoodTech.API.Interfaces;

/// <summary>
/// Pricing plans and their feature rows. Per-service tiers and combo packs share one table, so
/// they share this service too — the public surface splits them into two explicit endpoints.
/// </summary>
public interface IPricingService
{
    /// <summary>Combo packs: the plans with no owning service.</summary>
    Task<PagedResult<PricingPlanResponse>> GetPublicComboPlansAsync(
        bool? featured,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>The tiers of one service.</summary>
    Task<PagedResult<PricingPlanResponse>> GetPublicPlansForServiceAsync(
        Guid serviceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PagedResult<AdminPricingPlanResponse>> GetAdminPlansAsync(
        Guid? serviceId,
        bool comboOnly,
        bool tiersOnly,
        bool? isPublished,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminPricingPlanResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<AdminPricingPlanResponse>> CreateAsync(
        CreatePricingPlanRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminPricingPlanResponse>> UpdateAsync(
        Guid id,
        UpdatePricingPlanRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminPricingPlanResponse>> SetPublishedAsync(
        Guid id,
        SetPublishedRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<bool>> ReorderAsync(PricingReorderRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<PricingPlanFeatureResponse>> AddFeatureAsync(
        Guid planId,
        AddPricingPlanFeatureRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<PricingPlanFeatureResponse>> UpdateFeatureAsync(
        Guid planId,
        Guid featureId,
        UpdatePricingPlanFeatureRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> DeleteFeatureAsync(
        Guid planId,
        Guid featureId,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> ReorderFeaturesAsync(
        Guid planId,
        FeatureReorderRequest request,
        CancellationToken cancellationToken);
}
