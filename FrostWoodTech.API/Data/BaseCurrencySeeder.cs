using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Services;

namespace FrostWoodTech.API.Data;

/// <summary>Ensures USD exists; every other currency is added by an admin.</summary>
public static class BaseCurrencySeeder
{
    public static async Task EnsureSeededAsync(
        FrostWoodTechDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        // Include soft-deleted rows: they still hold the unique code.
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
            logger.LogWarning(ex, "Base currency seeding lost a race — it already exists.");
        }
    }
}
