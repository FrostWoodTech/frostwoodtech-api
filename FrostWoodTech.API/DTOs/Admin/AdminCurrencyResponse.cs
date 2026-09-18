namespace FrostWoodTech.API.DTOs.Admin;

public sealed class AdminCurrencyResponse
{
    public required Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; init; }

    public required string Symbol { get; init; }

    /// <summary>Admin override; null means the live rate is used.</summary>
    public decimal? ManualRateFromUsd { get; init; }

    /// <summary>Null until the first refresh.</summary>
    public decimal? LiveRateFromUsd { get; init; }

    public DateTimeOffset? LiveRateFetchedAt { get; init; }

    /// <summary>Manual rate if set, otherwise live. Null when neither exists.</summary>
    public decimal? EffectiveRateFromUsd { get; init; }

    public required bool IsActive { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
