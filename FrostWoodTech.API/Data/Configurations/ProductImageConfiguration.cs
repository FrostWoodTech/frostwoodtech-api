using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FrostWoodTech.API.Entities;

namespace FrostWoodTech.API.Data.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("product_images");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id");
        builder.Property(i => i.ProductId).HasColumnName("product_id");
        builder.Property(i => i.ObjectKey).HasColumnName("object_key").IsRequired();
        builder.Property(i => i.Url).HasColumnName("url").IsRequired();
        builder.Property(i => i.AltText).HasColumnName("alt_text").IsRequired();
        builder.Property(i => i.Width).HasColumnName("width");
        builder.Property(i => i.Height).HasColumnName("height");
        builder.Property(i => i.IsPrimary).HasColumnName("is_primary");
        builder.Property(i => i.SortOrder).HasColumnName("sort_order");

        builder.HasOne(i => i.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Exactly one primary image per product.
        builder.HasIndex(i => i.ProductId)
            .IsUnique()
            .HasFilter("is_primary");
    }
}
