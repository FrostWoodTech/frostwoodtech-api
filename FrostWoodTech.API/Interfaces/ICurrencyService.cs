using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;

namespace FrostWoodTech.API.Interfaces;

public interface ICurrencyService
{
    /// <summary>Active currencies that have a manual or live rate.</summary>
    Task<IReadOnlyList<CurrencyResponse>> GetPublicCurrenciesAsync(CancellationToken cancellationToken);

    Task<PagedResult<AdminCurrencyResponse>> GetAdminCurrenciesAsync(
        bool? isActive,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminCurrencyResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<AdminCurrencyResponse>> CreateAsync(
        CreateCurrencyRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminCurrencyResponse>> UpdateAsync(
        Guid id,
        UpdateCurrencyRequest request,
        CancellationToken cancellationToken);

    /// <summary>Refused for USD and while a pricing plan uses the currency.</summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<RefreshCurrencyRatesResponse>> RefreshLiveRatesAsync(CancellationToken cancellationToken);
}
