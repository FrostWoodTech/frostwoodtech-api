using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Services;

namespace FrostWoodTech.API.Data;

/// <summary>
/// Ensures USD exists, since every other rate is expressed against it and the public sites fall
/// back to it. Every other currency is the admin's to add — only they know today's rate.
/// </summary>
public static class BaseCurrencySeeder
{
    public static async Task EnsureSeededAsync(
        FrostWoodTechDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        // IgnoreQueryFilters: a soft-deleted USD row still occupies the unique code index.
        var exists = await db.Currencies
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Code == CurrencyService.BaseCurrencyCode, cancellationToken);

        if (exists)
        {
            return;
        }

        db.Currencies.Add(new Currency
        {
            Id = Guid.NewGuid(),
            Code = CurrencyService.BaseCurrencyCode,
            Name = "US Dollar",
            Symbol = "$",
            ManualRateFromUsd = 1m,
            IsActive = true
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Seeded the base currency {Code}.", CurrencyService.BaseCurrencyCode);
        }
        catch (DbUpdateException ex)
        {
            // The unique index on code catches two cold starts racing here. Either way it now exists.
            logger.LogWarning(ex, "Base currency seeding lost a race — it already exists.");
        }
    }
}
