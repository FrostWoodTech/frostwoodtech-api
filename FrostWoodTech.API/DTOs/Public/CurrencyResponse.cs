namespace FrostWoodTech.API.DTOs.Public;

public sealed class CurrencyResponse
{
    public required string Code { get; init; }

    public required string Name { get; init; }

    public required string Symbol { get; init; }

    /// <summary>Units of this currency per 1 USD.</summary>
    public required decimal RateFromUsd { get; init; }
}
