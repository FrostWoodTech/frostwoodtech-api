using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FrostWoodTech.API.Entities;

namespace FrostWoodTech.API.Data.Configurations;

public class ServiceProjectConfiguration : IEntityTypeConfiguration<ServiceProject>
{
    public void Configure(EntityTypeBuilder<ServiceProject> builder)
    {
        builder.ToTable("service_projects");

        builder.HasKey(sp => new { sp.ServiceId, sp.ProjectId });
        builder.Property(sp => sp.ServiceId).HasColumnName("service_id");
        builder.Property(sp => sp.ProjectId).HasColumnName("project_id");

        builder.HasOne(sp => sp.Service)
            .WithMany(s => s.ServiceProjects)
            .HasForeignKey(sp => sp.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict, not cascade: a project still linked to a service must not be removable.
        builder.HasOne(sp => sp.Project)
            .WithMany(p => p.ServiceProjects)
            .HasForeignKey(sp => sp.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(sp => sp.ProjectId);
    }
}
