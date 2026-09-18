using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FrostWoodTech.API.Entities;

namespace FrostWoodTech.API.Data.Configurations;

public class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        builder.ToTable("certificates");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.Name).HasColumnName("name").IsRequired();
        builder.Property(c => c.IssuedBy).HasColumnName("issued_by").IsRequired();
        builder.Property(c => c.IssuedDate).HasColumnName("issued_date");
        builder.Property(c => c.Marks).HasColumnName("marks");
        builder.Property(c => c.ObjectKey).HasColumnName("object_key").IsRequired();
        builder.Property(c => c.Url).HasColumnName("url").IsRequired();
        builder.Property(c => c.MimeType).HasColumnName("mime_type").IsRequired();
        builder.Property(c => c.Width).HasColumnName("width");
        builder.Property(c => c.Height).HasColumnName("height");
        builder.Property(c => c.AltText).HasColumnName("alt_text").IsRequired();
        builder.Property(c => c.IsPublished).HasColumnName("is_published");
        builder.Property(c => c.Featured).HasColumnName("featured");
        builder.Property(c => c.SortOrder).HasColumnName("sort_order");
    }
}
