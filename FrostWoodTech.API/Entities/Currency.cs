using FrostWoodTech.API.Entities.Common;

namespace FrostWoodTech.API.Entities;

/// <summary>
/// A display currency and the rate it converts at. Two independent numbers feed that rate: an
/// admin-typed override that stays exactly what was typed until it's typed again, and a
/// periodically-fetched live rate that tracks the market. The override wins when both exist —
/// see <see cref="EffectiveRateFromUsd"/>.
/// </summary>
public class Currency : AuditableEntity
{
    /// <summary>ISO 4217, upper-cased, e.g. <c>LKR</c>.</summary>
    public required string Code { get; set; }

    public required string Name { get; set; }

    /// <summary>Display symbol, e.g. <c>Rs</c>. Not unique — several currencies share <c>$</c>.</summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// "Price set by me" — units of this currency per 1 USD, admin-typed and sticky. Null means
    /// "use the live rate." USD itself is always exactly 1 — the base cannot be re-based, which
    /// the service enforces.
    /// </summary>
    public decimal? ManualRateFromUsd { get; set; }

    /// <summary>"Actual price" — units per 1 USD as of the last refresh. Null until one has run.</summary>
    public decimal? LiveRateFromUsd { get; set; }

    public DateTimeOffset? LiveRateFetchedAt { get; set; }

    /// <summary>Whether visitors can pick it. An inactive currency stays for its rate history.</summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// What a visitor actually converts at. Null means neither an override nor a live rate exists
    /// yet — the service excludes such a currency from the public list rather than show a
    /// fabricated number.
    /// </summary>
    public decimal? EffectiveRateFromUsd => ManualRateFromUsd ?? LiveRateFromUsd;
}
