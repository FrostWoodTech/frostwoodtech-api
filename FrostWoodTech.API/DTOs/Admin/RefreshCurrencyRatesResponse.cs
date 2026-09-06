namespace FrostWoodTech.API.DTOs.Admin;

public sealed class RefreshCurrencyRatesResponse
{
    public required int UpdatedCount { get; init; }

    public required DateTimeOffset FetchedAt { get; init; }
}
