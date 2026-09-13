using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FrostWoodTech.API.Entities;

namespace FrostWoodTech.API.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.Email).HasColumnName("email").HasColumnType("citext").IsRequired();
        builder.Property(u => u.FirstName).HasColumnName("first_name").IsRequired();
        builder.Property(u => u.LastName).HasColumnName("last_name").IsRequired();
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash");
        builder.Property(u => u.GoogleSubjectId).HasColumnName("google_subject_id");
        builder.Property(u => u.AvatarUrl).HasColumnName("avatar_url");
        builder.Property(u => u.Role).HasColumnName("role").HasColumnType("user_role");
        builder.Property(u => u.Status).HasColumnName("status").HasColumnType("user_status");
        builder.Property(u => u.ApprovedBy).HasColumnName("approved_by");
        builder.Property(u => u.ApprovedAt).HasColumnName("approved_at");
        builder.Property(u => u.RejectionReason).HasColumnName("rejection_reason");
        builder.Property(u => u.LastLoginAt).HasColumnName("last_login_at");
        builder.Property(u => u.EmailVerifiedAt).HasColumnName("email_verified_at");

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.GoogleSubjectId).IsUnique();

        // At most one super admin; the role enum label is 'super_admin'.
        builder.HasIndex(u => u.Role)
            .IsUnique()
            .HasFilter("\"role\" = 'super_admin'")
            .HasDatabaseName("ix_users_single_super_admin");

        builder.HasOne(u => u.ApprovedByUser)
            .WithMany()
            .HasForeignKey(u => u.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
