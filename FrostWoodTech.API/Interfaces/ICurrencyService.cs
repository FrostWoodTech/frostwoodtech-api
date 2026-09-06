using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;

namespace FrostWoodTech.API.Interfaces;

public interface ICurrencyService
{
    /// <summary>
    /// Public read: active rows only. Not paged and not site-scoped — currencies are a small
    /// shared lookup set, same as tags. There is no overload that returns inactive rows.
    /// </summary>
    /// Never a currency with neither a manual override nor a fetched live rate — see
    /// `Currency.EffectiveRateFromUsd`.
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

    /// <summary>Soft delete. Refused for USD, and while a pricing plan still prices in it.</summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Fetches every rate the provider knows in one call and updates each currency's live rate.
    /// A currency whose code the provider doesn't recognise is left untouched.
    /// </summary>
    Task<ServiceResult<RefreshCurrencyRatesResponse>> RefreshLiveRatesAsync(CancellationToken cancellationToken);
}
