using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FrostWoodTech.API.Entities;

namespace FrostWoodTech.API.Data.Configurations;

public class FaqConfiguration : IEntityTypeConfiguration<Faq>
{
    public void Configure(EntityTypeBuilder<Faq> builder)
    {
        builder.ToTable("faqs");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id");
        builder.Property(f => f.Question).HasColumnName("question").IsRequired();
        builder.Property(f => f.Answer).HasColumnName("answer").IsRequired();
        builder.Property(f => f.ServiceId).HasColumnName("service_id");
        builder.Property(f => f.SortOrder).HasColumnName("sort_order");
        builder.Property(f => f.IsPublished).HasColumnName("is_published");

        // Restrict, not cascade: a service still owning FAQs must not be removable.
        builder.HasOne(f => f.Service)
            .WithMany(s => s.Faqs)
            .HasForeignKey(f => f.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => f.ServiceId);

        // Not the shared SiteVisibleEntity block — FAQs keep their own visibility flags but no
        // per-site featured/sort fields — so these need explicit mapping here instead.
        builder.Property(f => f.ShowOnAgency).HasColumnName("show_on_agency");
        builder.Property(f => f.ShowOnPersonal).HasColumnName("show_on_personal");
    }
}
