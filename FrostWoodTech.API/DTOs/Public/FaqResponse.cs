namespace FrostWoodTech.API.DTOs.Public;

/// <summary>
/// What the two public frontends see. The draft state, the other site's visibility flag, and the
/// audit metadata never cross this boundary. FAQs have no "featured" concept and share one sort
/// order across both sites.
/// </summary>
public sealed class FaqResponse
{
    public required Guid Id { get; init; }

    public required string Question { get; init; }

    /// <summary>Markdown — sanitised on render in React, not on write.</summary>
    public required string Answer { get; init; }

    public required int SortOrder { get; init; }
}
