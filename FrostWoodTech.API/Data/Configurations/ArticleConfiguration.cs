using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FrostWoodTech.API.Entities;

namespace FrostWoodTech.API.Data.Configurations;

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.ToTable("articles");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.Title).HasColumnName("title").IsRequired();
        builder.Property(a => a.Excerpt).HasColumnName("excerpt").IsRequired();
        builder.Property(a => a.PublishedAt).HasColumnName("published_at");
        builder.Property(a => a.ContentMarkdown).HasColumnName("content_markdown");
        builder.Property(a => a.CoverImageKey).HasColumnName("cover_image_key");
        builder.Property(a => a.Slug).HasColumnName("slug");
        builder.Property(a => a.IsPublished).HasColumnName("is_published");

        builder.HasIndex(a => a.Slug).IsUnique();
    }
}
