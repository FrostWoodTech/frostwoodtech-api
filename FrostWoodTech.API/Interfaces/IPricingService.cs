using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;

namespace FrostWoodTech.API.Interfaces;

public interface IPricingService
{
    Task<PagedResult<PricingPlanResponse>> GetPublicComboPlansAsync(
        bool? featured,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

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

    Task<PagedResult<TrashedItemResponse>> GetTrashAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>404 unless the row is in the trash.</summary>
    Task<ServiceResult<AdminPricingPlanResponse>> RestoreAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Super admin only; stored files are deleted after the commit.</summary>
    Task<ServiceResult<bool>> PurgeAsync(Guid id, CancellationToken cancellationToken);

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
