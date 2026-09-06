using FrostWoodTech.API.Common;
using FrostWoodTech.API.Data;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Interfaces;
using FrostWoodTech.API.Services;

namespace FrostWoodTech.Tests;

[Collection(nameof(PostgresCollection))]
public class CurrencyTests
{
    private readonly PostgresFixture _fixture;

    public CurrencyTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task An_active_currency_appears_on_the_public_list()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var created = await service.CreateAsync(NewCurrency(isActive: true), CancellationToken.None);
        Assert.True(created.IsSuccess);

        var published = await service.GetPublicCurrenciesAsync(CancellationToken.None);

        Assert.Contains(published, c => c.Code == created.Value!.Code);
    }

    [Fact]
    public async Task An_inactive_currency_is_admin_only()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var created = await service.CreateAsync(NewCurrency(isActive: false), CancellationToken.None);
        Assert.True(created.IsSuccess);

        var published = await service.GetPublicCurrenciesAsync(CancellationToken.None);
        Assert.DoesNotContain(published, c => c.Code == created.Value!.Code);

        var admin = await service.GetAdminCurrenciesAsync(null, null, 1, 500, CancellationToken.None);
        Assert.Contains(admin.Items, c => c.Code == created.Value!.Code);
    }

    [Fact]
    public async Task A_currency_with_no_manual_override_and_no_live_rate_is_hidden_from_the_public_list()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var request = NewCurrency();
        request.ManualRateFromUsd = null;
        var created = await service.CreateAsync(request, CancellationToken.None);
        Assert.True(created.IsSuccess);
        Assert.Null(created.Value!.EffectiveRateFromUsd);

        var published = await service.GetPublicCurrenciesAsync(CancellationToken.None);

        Assert.DoesNotContain(published, c => c.Code == created.Value!.Code);
    }

    [Fact]
    public async Task A_currency_with_only_a_live_rate_is_public()
    {
        await using var db = _fixture.CreateContext();
        var provider = new FakeExchangeRateProvider();
        var service = NewService(db, provider);

        var request = NewCurrency();
        request.ManualRateFromUsd = null;
        var created = await service.CreateAsync(request, CancellationToken.None);
        Assert.True(created.IsSuccess);

        provider.Rates[created.Value!.Code] = 42m;
        var refreshed = await service.RefreshLiveRatesAsync(CancellationToken.None);
        Assert.True(refreshed.IsSuccess);

        var published = await service.GetPublicCurrenciesAsync(CancellationToken.None);
        var row = Assert.Single(published, c => c.Code == created.Value!.Code);
        Assert.Equal(42m, row.RateFromUsd);
    }

    [Fact]
    public async Task A_manual_override_wins_over_a_live_rate()
    {
        await using var db = _fixture.CreateContext();
        var provider = new FakeExchangeRateProvider();
        var service = NewService(db, provider);

        var created = await service.CreateAsync(NewCurrency(rate: 325m), CancellationToken.None);
        Assert.True(created.IsSuccess);

        provider.Rates[created.Value!.Code] = 999m;
        await service.RefreshLiveRatesAsync(CancellationToken.None);

        var reread = await service.GetByIdAsync(created.Value!.Id, CancellationToken.None);
        Assert.Equal(325m, reread.Value!.ManualRateFromUsd);
        Assert.Equal(999m, reread.Value!.LiveRateFromUsd);
        Assert.Equal(325m, reread.Value!.EffectiveRateFromUsd);
    }

    [Fact]
    public async Task Refreshing_ignores_a_code_the_provider_does_not_know()
    {
        await using var db = _fixture.CreateContext();
        var provider = new FakeExchangeRateProvider();
        var service = NewService(db, provider);

        var request = NewCurrency();
        request.ManualRateFromUsd = null;
        var created = await service.CreateAsync(request, CancellationToken.None);
        Assert.True(created.IsSuccess);

        // The fake provider has no entry for this code at all.
        var result = await service.RefreshLiveRatesAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reread = await service.GetByIdAsync(created.Value!.Id, CancellationToken.None);
        Assert.Null(reread.Value!.LiveRateFromUsd);
    }

    [Fact]
    public async Task A_duplicate_code_is_rejected()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var request = NewCurrency();
        var first = await service.CreateAsync(request, CancellationToken.None);
        Assert.True(first.IsSuccess);

        var duplicate = NewCurrency();
        duplicate.Code = request.Code;

        var second = await service.CreateAsync(duplicate, CancellationToken.None);

        Assert.False(second.IsSuccess);
        Assert.Equal(ServiceErrorKind.Conflict, second.Error!.Kind);
        Assert.Equal("code_taken", second.Error!.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task A_manual_rate_at_or_below_zero_is_rejected(decimal rate)
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var request = NewCurrency();
        request.ManualRateFromUsd = rate;

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation_failed", result.Error!.Code);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("U5D")]
    public async Task A_code_that_is_not_three_letters_is_rejected(string code)
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var request = NewCurrency();
        request.Code = code;

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation_failed", result.Error!.Code);
    }

    [Fact]
    public async Task The_base_currency_cannot_be_given_a_rate_other_than_one()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        await SeedBaseAsync(db);

        var usd = await FindBaseAsync(service);

        var result = await service.UpdateAsync(
            usd.Id,
            new UpdateCurrencyRequest
            {
                Code = usd.Code,
                Name = usd.Name,
                Symbol = usd.Symbol,
                ManualRateFromUsd = 1.5m,
                IsActive = true
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation_failed", result.Error!.Code);
    }

    [Fact]
    public async Task The_base_currency_cannot_have_a_null_manual_rate()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        await SeedBaseAsync(db);

        var usd = await FindBaseAsync(service);

        var result = await service.UpdateAsync(
            usd.Id,
            new UpdateCurrencyRequest
            {
                Code = usd.Code,
                Name = usd.Name,
                Symbol = usd.Symbol,
                ManualRateFromUsd = null,
                IsActive = true
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation_failed", result.Error!.Code);
    }

    [Fact]
    public async Task The_base_currency_cannot_be_deactivated()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        await SeedBaseAsync(db);

        var usd = await FindBaseAsync(service);

        var result = await service.UpdateAsync(
            usd.Id,
            new UpdateCurrencyRequest
            {
                Code = usd.Code,
                Name = usd.Name,
                Symbol = usd.Symbol,
                ManualRateFromUsd = 1m,
                IsActive = false
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation_failed", result.Error!.Code);
    }

    [Fact]
    public async Task The_base_currency_cannot_be_deleted()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        await SeedBaseAsync(db);

        var usd = await FindBaseAsync(service);

        var result = await service.DeleteAsync(usd.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorKind.Conflict, result.Error!.Kind);
        Assert.Equal("cannot_delete_base_currency", result.Error!.Code);
    }

    [Fact]
    public async Task A_manual_rate_update_sticks_until_it_is_changed_again()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var created = await service.CreateAsync(NewCurrency(rate: 325m), CancellationToken.None);
        Assert.True(created.IsSuccess);

        var updated = await service.UpdateAsync(
            created.Value!.Id,
            new UpdateCurrencyRequest
            {
                Code = created.Value!.Code,
                Name = created.Value!.Name,
                Symbol = created.Value!.Symbol,
                ManualRateFromUsd = 333m,
                IsActive = true
            },
            CancellationToken.None);

        Assert.True(updated.IsSuccess);
        Assert.Equal(333m, updated.Value!.EffectiveRateFromUsd);

        var reread = await service.GetByIdAsync(created.Value!.Id, CancellationToken.None);
        Assert.Equal(333m, reread.Value!.EffectiveRateFromUsd);
    }

    /// <summary>The seeder normally does this on startup; the tests own their own database.</summary>
    private static async Task SeedBaseAsync(FrostWoodTechDbContext db) =>
        await BaseCurrencySeeder.EnsureSeededAsync(
            db,
            Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance,
            CancellationToken.None);

    private static async Task<AdminCurrencyResponse> FindBaseAsync(CurrencyService service)
    {
        var all = await service.GetAdminCurrenciesAsync(null, null, 1, 500, CancellationToken.None);

        return all.Items.First(c => c.Code == CurrencyService.BaseCurrencyCode);
    }

    private static CurrencyService NewService(FrostWoodTechDbContext db, IExchangeRateProvider? provider = null) =>
        new(db, provider ?? new FakeExchangeRateProvider());

    /// <summary>Never calls the network — hands back whatever the test put in <see cref="Rates"/>.</summary>
    private sealed class FakeExchangeRateProvider : IExchangeRateProvider
    {
        public Dictionary<string, decimal> Rates { get; } = [];

        public Task<IReadOnlyDictionary<string, decimal>> GetRatesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, decimal>>(Rates);
    }

    /// <summary>
    /// Walks the 3-letter space from a random start rather than picking letters at random: these
    /// tests share one database, and a collision with another test's code — or with USD — would
    /// fail as a spurious `code_taken`.
    /// </summary>
    private static int _nextCode = Random.Shared.Next(26 * 26 * 26);

    private static string UniqueCode()
    {
        while (true)
        {
            var value = Interlocked.Increment(ref _nextCode) % (26 * 26 * 26);
            var code = string.Concat(
                (char)('A' + value / (26 * 26)),
                (char)('A' + value / 26 % 26),
                (char)('A' + value % 26));

            if (code != CurrencyService.BaseCurrencyCode)
            {
                return code;
            }
        }
    }

    private static CreateCurrencyRequest NewCurrency(decimal rate = 325m, bool isActive = true) => new()
    {
        Code = UniqueCode(),
        Name = "Test Currency",
        Symbol = "T$",
        ManualRateFromUsd = rate,
        IsActive = isActive
    };
}
