namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>Admin view: exposes both the override and the live rate separately, plus which one is
/// actually in effect, so the admin can see why a currency shows what it shows.</summary>
public sealed class AdminCurrencyResponse
{
    public required Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; init; }

    public required string Symbol { get; init; }

    /// <summary>"Price set by me." Null means the live rate is used instead.</summary>
    public decimal? ManualRateFromUsd { get; init; }

    /// <summary>"Actual price," as of the last refresh. Null until one has run.</summary>
    public decimal? LiveRateFromUsd { get; init; }

    public DateTimeOffset? LiveRateFetchedAt { get; init; }

    /// <summary>What a visitor actually converts at. Null means neither number exists yet.</summary>
    public decimal? EffectiveRateFromUsd { get; init; }

    public required bool IsActive { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
