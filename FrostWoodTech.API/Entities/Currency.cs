using FrostWoodTech.API.Entities.Common;

namespace FrostWoodTech.API.Entities;

public class Currency : AuditableEntity
{
    public required string Code { get; set; }

    public required string Name { get; set; }

    public required string Symbol { get; set; }

    /// <summary>Admin override in units per 1 USD. Null means use the live rate; USD is always 1.</summary>
    public decimal? ManualRateFromUsd { get; set; }

    /// <summary>Null until the first refresh.</summary>
    public decimal? LiveRateFromUsd { get; set; }

    public DateTimeOffset? LiveRateFetchedAt { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Override wins. Null currencies are hidden from the public list.</summary>
    public decimal? EffectiveRateFromUsd => ManualRateFromUsd ?? LiveRateFromUsd;
}
