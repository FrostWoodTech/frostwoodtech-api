namespace FrostWoodTech.API.Enums;

/// <summary>
/// Optional budget band on the contact form. Deliberately coarse and currency-free — it qualifies
/// a lead, it is not a quote. Maps to the Postgres native enum <c>contact_budget_range</c>; the
/// amounts are spelled out because the snake_case translator mangles digits.
/// </summary>
public enum ContactBudgetRange
{
    UnderOneK,
    OneToFiveK,
    FiveToFifteenK,
    OverFifteenK,
    NotSure
}
