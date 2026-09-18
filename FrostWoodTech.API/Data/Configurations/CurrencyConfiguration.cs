using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FrostWoodTech.API.Entities;

namespace FrostWoodTech.API.Data.Configurations;

public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("currencies");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        // char(3), matching pricing_plans.currency.
        builder.Property(c => c.Code).HasColumnName("code").HasColumnType("char(3)").IsRequired();
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(c => c.Symbol).HasColumnName("symbol").HasMaxLength(10).IsRequired();
        // Positivity is checked in the service.
        builder.Property(c => c.ManualRateFromUsd)
            .HasColumnName("manual_rate_from_usd")
            .HasColumnType("numeric(18,6)");
        builder.Property(c => c.LiveRateFromUsd)
            .HasColumnName("live_rate_from_usd")
            .HasColumnType("numeric(18,6)");
        builder.Property(c => c.LiveRateFetchedAt).HasColumnName("live_rate_fetched_at");
        builder.Property(c => c.IsActive).HasColumnName("is_active");

        builder.Ignore(c => c.EffectiveRateFromUsd);

        builder.HasIndex(c => c.Code).IsUnique();
    }
}
