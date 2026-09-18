using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FrostWoodTech.API.Entities;

namespace FrostWoodTech.API.Data.Configurations;

public class ServiceOfferingConfiguration : IEntityTypeConfiguration<ServiceOffering>
{
    public void Configure(EntityTypeBuilder<ServiceOffering> builder)
    {
        builder.ToTable("services");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.Slug).HasColumnName("slug").IsRequired();
        builder.Property(s => s.Name).HasColumnName("name").IsRequired();
        builder.Property(s => s.ShortDescription).HasColumnName("short_description").IsRequired();
        builder.Property(s => s.Eyebrow).HasColumnName("eyebrow");
        builder.Property(s => s.Headline).HasColumnName("headline");
        builder.Property(s => s.Deck).HasColumnName("deck");
        builder.Property(s => s.WhoThisIsFor).HasColumnName("who_this_is_for");
        builder.Property(s => s.Outcomes).HasColumnName("outcomes");
        builder.Property(s => s.Capabilities).HasColumnName("capabilities");
        builder.Property(s => s.InDepth).HasColumnName("in_depth");
        builder.Property(s => s.PrimaryCtaLabel).HasColumnName("primary_cta_label");
        builder.Property(s => s.PrimaryCtaUrl).HasColumnName("primary_cta_url");
        builder.Property(s => s.SecondaryCtaLabel).HasColumnName("secondary_cta_label");
        builder.Property(s => s.SecondaryCtaUrl).HasColumnName("secondary_cta_url");
        builder.Property(s => s.IconObjectKey).HasColumnName("icon_object_key");
        builder.Property(s => s.IconUrl).HasColumnName("icon_url");
        builder.Property(s => s.IconWidth).HasColumnName("icon_width");
        builder.Property(s => s.IconHeight).HasColumnName("icon_height");
        builder.Property(s => s.IconAltText).HasColumnName("icon_alt_text");
        builder.Property(s => s.HeroImageObjectKey).HasColumnName("hero_image_object_key");
        builder.Property(s => s.HeroImageUrl).HasColumnName("hero_image_url");
        builder.Property(s => s.HeroImageWidth).HasColumnName("hero_image_width");
        builder.Property(s => s.HeroImageHeight).HasColumnName("hero_image_height");
        builder.Property(s => s.HeroImageAltText).HasColumnName("hero_image_alt_text");
        builder.Property(s => s.DepthImageObjectKey).HasColumnName("depth_image_object_key");
        builder.Property(s => s.DepthImageUrl).HasColumnName("depth_image_url");
        builder.Property(s => s.DepthImageWidth).HasColumnName("depth_image_width");
        builder.Property(s => s.DepthImageHeight).HasColumnName("depth_image_height");
        builder.Property(s => s.DepthImageAltText).HasColumnName("depth_image_alt_text");
        builder.Property(s => s.IsPublished).HasColumnName("is_published");
        builder.Property(s => s.PublishedAt).HasColumnName("published_at");
        builder.Property(s => s.SeoTitle).HasColumnName("seo_title");
        builder.Property(s => s.SeoDescription).HasColumnName("seo_description");

        builder.HasIndex(s => s.Slug).IsUnique();
    }
}
