using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Data;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;
using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Services;

public class CurrencyService : ICurrencyService
{
    /// <summary>
    /// The base every rate is expressed against. Its own rate is pinned to 1 — a base that could
    /// be re-based would silently rescale every converted price on both public sites.
    /// </summary>
    public const string BaseCurrencyCode = "USD";

    private readonly FrostWoodTechDbContext _db;
    private readonly IExchangeRateProvider _exchangeRateProvider;

    public CurrencyService(FrostWoodTechDbContext db, IExchangeRateProvider exchangeRateProvider)
    {
        _db = db;
        _exchangeRateProvider = exchangeRateProvider;
    }

    public async Task<IReadOnlyList<CurrencyResponse>> GetPublicCurrenciesAsync(
        CancellationToken cancellationToken) =>
        await _db.Currencies
            .AsNoTracking()
            // Hide a currency with neither an override nor a fetched rate rather than show a
            // fabricated number — see Currency.EffectiveRateFromUsd.
            .Where(c => c.IsActive && (c.ManualRateFromUsd ?? c.LiveRateFromUsd) != null)
            .OrderBy(c => c.Code)
            .Select(PublicProjection)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<AdminCurrencyResponse>> GetAdminCurrenciesAsync(
        bool? isActive,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.Currencies.AsNoTracking();

        if (isActive is not null)
        {
            query = query.Where(c => c.IsActive == isActive);
        }

        if (search is not null)
        {
            query = query.Where(c =>
                EF.Functions.ILike(c.Code, $"%{search}%")
                || EF.Functions.ILike(c.Name, $"%{search}%"));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(AdminProjection)
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminCurrencyResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<ServiceResult<AdminCurrencyResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currency = await _db.Currencies
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(AdminProjection)
            .FirstOrDefaultAsync(cancellationToken);

        return currency is null ? NotFound(id) : ServiceResult<AdminCurrencyResponse>.Success(currency);
    }

    public async Task<ServiceResult<AdminCurrencyResponse>> CreateAsync(
        CreateCurrencyRequest request,
        CancellationToken cancellationToken)
    {
        var code = NormalizeCode(request.Code);
        var name = Blank(request.Name);
        var symbol = Blank(request.Symbol);

        var validationError = Validate(code, name, symbol, request.ManualRateFromUsd, request.IsActive);
        if (validationError is not null)
        {
            return ServiceResult<AdminCurrencyResponse>.Validation(validationError);
        }

        if (await CodeExistsAsync(code!, excludingId: null, cancellationToken))
        {
            return CodeTaken(code!);
        }

        var currency = new Currency
        {
            Id = Guid.NewGuid(),
            Code = code!,
            Name = name!,
            Symbol = symbol!,
            ManualRateFromUsd = request.ManualRateFromUsd,
            IsActive = request.IsActive
        };

        _db.Currencies.Add(currency);
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<AdminCurrencyResponse>.Success(ToAdminResponse(currency));
    }

    public async Task<ServiceResult<AdminCurrencyResponse>> UpdateAsync(
        Guid id,
        UpdateCurrencyRequest request,
        CancellationToken cancellationToken)
    {
        var currency = await _db.Currencies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (currency is null)
        {
            return NotFound(id);
        }

        var code = NormalizeCode(request.Code);
        var name = Blank(request.Name);
        var symbol = Blank(request.Symbol);

        var validationError = Validate(code, name, symbol, request.ManualRateFromUsd, request.IsActive);
        if (validationError is not null)
        {
            return ServiceResult<AdminCurrencyResponse>.Validation(validationError);
        }

        // Renaming the base away would leave the rates expressed against nothing.
        if (IsBase(currency.Code) && !IsBase(code!))
        {
            return ServiceResult<AdminCurrencyResponse>.Conflict(
                "cannot_rename_base_currency",
                $"{BaseCurrencyCode} is the base every rate is expressed against and cannot be renamed.");
        }

        if (await CodeExistsAsync(code!, excludingId: id, cancellationToken))
        {
            return CodeTaken(code!);
        }

        currency.Code = code!;
        currency.Name = name!;
        currency.Symbol = symbol!;
        currency.ManualRateFromUsd = request.ManualRateFromUsd;
        currency.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<AdminCurrencyResponse>.Success(ToAdminResponse(currency));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var currency = await _db.Currencies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (currency is null)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No currency with id {id}.");
        }

        if (IsBase(currency.Code))
        {
            return ServiceResult<bool>.Conflict(
                "cannot_delete_base_currency",
                $"{BaseCurrencyCode} is the base every rate is expressed against and cannot be deleted.");
        }

        // Counted through PricingPlans so the global soft-delete filter applies — a currency held
        // only by deleted plans is free to go.
        var planCount = await _db.PricingPlans
            .CountAsync(p => p.Currency == currency.Code, cancellationToken);

        if (planCount > 0)
        {
            return ServiceResult<bool>.Conflict(
                "currency_in_use",
                $"Currency '{currency.Code}' prices {planCount} pricing plan(s). "
                + "Move them to another currency before deleting.");
        }

        currency.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    /// <summary>
    /// One call to the provider updates every currency's live rate at once. A code the provider
    /// doesn't recognise (a typo, something exotic) is simply left with whatever it already had.
    /// </summary>
    public async Task<ServiceResult<RefreshCurrencyRatesResponse>> RefreshLiveRatesAsync(
        CancellationToken cancellationToken)
    {
        var rates = await _exchangeRateProvider.GetRatesAsync(cancellationToken);
        if (rates.Count == 0)
        {
            return ServiceResult<RefreshCurrencyRatesResponse>.Validation(
                "The exchange rate provider could not be reached. Live rates are unchanged.");
        }

        var currencies = await _db.Currencies.ToListAsync(cancellationToken);
        var fetchedAt = DateTimeOffset.UtcNow;
        var updatedCount = 0;

        foreach (var currency in currencies)
        {
            if (!rates.TryGetValue(currency.Code, out var rate))
            {
                continue;
            }

            currency.LiveRateFromUsd = rate;
            currency.LiveRateFetchedAt = fetchedAt;
            updatedCount++;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<RefreshCurrencyRatesResponse>.Success(new RefreshCurrencyRatesResponse
        {
            UpdatedCount = updatedCount,
            FetchedAt = fetchedAt
        });
    }

    /// <summary>Null when the currency is valid, otherwise the message to hand back.</summary>
    private static string? Validate(
        string? code,
        string? name,
        string? symbol,
        decimal? manualRateFromUsd,
        bool isActive)
    {
        if (code is null)
        {
            return "A 3-letter code is required.";
        }

        if (name is null)
        {
            return "Name is required.";
        }

        if (symbol is null)
        {
            return "Symbol is required.";
        }

        if (manualRateFromUsd is not null && manualRateFromUsd <= 0)
        {
            return "manualRateFromUsd must be greater than zero.";
        }

        if (IsBase(code))
        {
            if (manualRateFromUsd != 1m)
            {
                return $"{BaseCurrencyCode} is the base currency, so its rate must be exactly 1.";
            }

            if (!isActive)
            {
                return $"{BaseCurrencyCode} is the fallback every visitor sees and cannot be deactivated.";
            }
        }

        return null;
    }

    private static bool IsBase(string code) =>
        string.Equals(code, BaseCurrencyCode, StringComparison.Ordinal);

    /// <summary>Upper-cased and trimmed, or null when it is not exactly 3 ASCII letters.</summary>
    private static string? NormalizeCode(string? value)
    {
        var trimmed = Blank(value)?.ToUpperInvariant();

        return trimmed is { Length: 3 } && trimmed.All(char.IsAsciiLetter) ? trimmed : null;
    }

    private Task<bool> CodeExistsAsync(string code, Guid? excludingId, CancellationToken cancellationToken) =>
        _db.Currencies.AnyAsync(
            c => c.Code == code && (excludingId == null || c.Id != excludingId),
            cancellationToken);

    private static ServiceResult<AdminCurrencyResponse> NotFound(Guid id) =>
        ServiceResult<AdminCurrencyResponse>.NotFound("not_found", $"No currency with id {id}.");

    private static ServiceResult<AdminCurrencyResponse> CodeTaken(string code) =>
        ServiceResult<AdminCurrencyResponse>.Conflict("code_taken", $"Currency '{code}' already exists.");

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static readonly Expression<Func<Currency, CurrencyResponse>> PublicProjection =
        c => new CurrencyResponse
        {
            Code = c.Code,
            Name = c.Name,
            Symbol = c.Symbol,
            // Never null here — the query above only selects rows where one of the two exists.
            RateFromUsd = (c.ManualRateFromUsd ?? c.LiveRateFromUsd)!.Value
        };

    /// <summary>Projected inside the query so the SQL stays narrow.</summary>
    private static readonly Expression<Func<Currency, AdminCurrencyResponse>> AdminProjection =
        c => new AdminCurrencyResponse
        {
            Id = c.Id,
            Code = c.Code,
            Name = c.Name,
            Symbol = c.Symbol,
            ManualRateFromUsd = c.ManualRateFromUsd,
            LiveRateFromUsd = c.LiveRateFromUsd,
            LiveRateFetchedAt = c.LiveRateFetchedAt,
            EffectiveRateFromUsd = c.ManualRateFromUsd ?? c.LiveRateFromUsd,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        };

    /// <summary>The same shape for an entity already in memory after a write.</summary>
    private static readonly Func<Currency, AdminCurrencyResponse> ToAdminResponse = AdminProjection.Compile();
}
