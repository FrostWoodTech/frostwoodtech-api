using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FrostWoodTech.API.Entities;

namespace FrostWoodTech.API.Data.Configurations;

public class PricingPlanConfiguration : IEntityTypeConfiguration<PricingPlan>
{
    public void Configure(EntityTypeBuilder<PricingPlan> builder)
    {
        builder.ToTable("pricing_plans");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.ServiceId).HasColumnName("service_id");
        builder.Property(p => p.Name).HasColumnName("name").IsRequired();
        builder.Property(p => p.Tagline).HasColumnName("tagline");
        builder.Property(p => p.PriceAmount).HasColumnName("price_amount").HasColumnType("numeric(12,2)");
        builder.Property(p => p.Currency).HasColumnName("currency").HasColumnType("char(3)").IsRequired();
        builder.Property(p => p.PriceType).HasColumnName("price_type").HasColumnType("price_type");
        builder.Property(p => p.DeliveryText).HasColumnName("delivery_text");
        builder.Property(p => p.Description).HasColumnName("description").IsRequired();
        builder.Property(p => p.IsPopular).HasColumnName("is_popular");
        builder.Property(p => p.CtaLabel).HasColumnName("cta_label");
        builder.Property(p => p.CtaUrl).HasColumnName("cta_url");
        builder.Property(p => p.IsPublished).HasColumnName("is_published");
        builder.Property(p => p.Featured).HasColumnName("featured");
        builder.Property(p => p.SortOrder).HasColumnName("sort_order");

        // A null service_id means a combo pack, so the FK is optional.
        builder.HasOne(p => p.Service)
            .WithMany(s => s.PricingPlans)
            .HasForeignKey(p => p.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.ServiceId);
    }
}
