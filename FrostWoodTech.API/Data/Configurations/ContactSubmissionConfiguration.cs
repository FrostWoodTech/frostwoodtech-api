using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FrostWoodTech.API.Entities;

namespace FrostWoodTech.API.Data.Configurations;

public class ContactSubmissionConfiguration : IEntityTypeConfiguration<ContactSubmission>
{
    public void Configure(EntityTypeBuilder<ContactSubmission> builder)
    {
        builder.ToTable("contact_submissions");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(c => c.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
        builder.Property(c => c.Phone).HasColumnName("phone").HasMaxLength(30);
        builder.Property(c => c.Company).HasColumnName("company").HasMaxLength(150);
        builder.Property(c => c.Subject).HasColumnName("subject").HasMaxLength(200);
        builder.Property(c => c.Message).HasColumnName("message").HasMaxLength(4000).IsRequired();
        builder.Property(c => c.ServiceId).HasColumnName("service_id");
        builder.Property(c => c.BudgetRange).HasColumnName("budget_range").HasColumnType("contact_budget_range");
        builder.Property(c => c.Site).HasColumnName("site").HasColumnType("site");
        builder.Property(c => c.Status).HasColumnName("status").HasColumnType("contact_submission_status");
        builder.Property(c => c.AdminNotes).HasColumnName("admin_notes").HasMaxLength(4000);
        builder.Property(c => c.RepliedAt).HasColumnName("replied_at");
        builder.Property(c => c.RepliedBy).HasColumnName("replied_by");
        builder.Property(c => c.SubmitterIp).HasColumnName("submitter_ip");

        // SetNull both ways: losing a service or an admin account must never destroy the enquiry.
        builder.HasOne(c => c.Service)
            .WithMany()
            .HasForeignKey(c => c.ServiceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.RepliedByUser)
            .WithMany()
            .HasForeignKey(c => c.RepliedBy)
            .OnDelete(DeleteBehavior.SetNull);

        // The default admin inbox query.
        builder.HasIndex(c => new { c.Status, c.CreatedAt })
            .HasDatabaseName("ix_contact_submissions_status_created_at");

        // The per-IP submission rate limit count.
        builder.HasIndex(c => new { c.SubmitterIp, c.CreatedAt })
            .HasDatabaseName("ix_contact_submissions_submitter_ip_created_at");
    }
}
