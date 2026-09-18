using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FrostWoodTech.API.Entities;

namespace FrostWoodTech.API.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.Slug).HasColumnName("slug").IsRequired();
        builder.Property(p => p.Name).HasColumnName("name").IsRequired();
        builder.Property(p => p.Tagline).HasColumnName("tagline").IsRequired();
        builder.Property(p => p.Description).HasColumnName("description").IsRequired();
        builder.Property(p => p.PriceDetails).HasColumnName("price_details");
        builder.Property(p => p.ProductUrl).HasColumnName("product_url");
        builder.Property(p => p.IsPublished).HasColumnName("is_published");
        builder.Property(p => p.PublishedAt).HasColumnName("published_at");
        builder.Property(p => p.SeoTitle).HasColumnName("seo_title");
        builder.Property(p => p.SeoDescription).HasColumnName("seo_description");

        builder.HasIndex(p => p.Slug).IsUnique();
    }
}
