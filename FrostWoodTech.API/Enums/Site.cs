namespace FrostWoodTech.API.Enums;

/// <summary>
/// The two public frontends. Visibility is stored as a pair of flag columns per row, not as this
/// enum, so it mostly just travels on the wire as <c>?site=</c> — but it is also the Postgres enum
/// <c>site</c>, persisted by <c>contact_submissions.site</c>.
/// </summary>
public enum Site
{
    Agency,
    Personal
}
