using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Entities.Common;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Data;

public class FrostWoodTechDbContext : DbContext
{
    public FrostWoodTechDbContext(DbContextOptions<FrostWoodTechDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectImage> ProjectImages => Set<ProjectImage>();

    public DbSet<ProjectTag> ProjectTags => Set<ProjectTag>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    public DbSet<Article> Articles => Set<Article>();

    public DbSet<ArticleTag> ArticleTags => Set<ArticleTag>();

    public DbSet<ServiceOffering> Services => Set<ServiceOffering>();

    public DbSet<ServiceProject> ServiceProjects => Set<ServiceProject>();

    public DbSet<PricingPlan> PricingPlans => Set<PricingPlan>();

    public DbSet<PricingPlanFeature> PricingPlanFeatures => Set<PricingPlanFeature>();

    public DbSet<Faq> Faqs => Set<Faq>();

    public DbSet<Certificate> Certificates => Set<Certificate>();

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<ContactSubmission> ContactSubmissions => Set<ContactSubmission>();

    public DbSet<Currency> Currencies => Set<Currency>();

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();

    public DbSet<PasswordToken> PasswordTokens => Set<PasswordToken>();

    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("citext");

        // Native Postgres enums, never ints, so migrations stay readable.
        modelBuilder.HasPostgresEnum<TechCategory>(name: "tech_category");
        modelBuilder.HasPostgresEnum<PriceType>(name: "price_type");
        modelBuilder.HasPostgresEnum<UserRole>(name: "user_role");
        modelBuilder.HasPostgresEnum<UserStatus>(name: "user_status");
        modelBuilder.HasPostgresEnum<PasswordTokenPurpose>(name: "password_token_purpose");
        modelBuilder.HasPostgresEnum<AuthAttemptAction>(name: "auth_attempt_action");
        modelBuilder.HasPostgresEnum<ContactSubmissionStatus>(name: "contact_submission_status");
        modelBuilder.HasPostgresEnum<ContactBudgetRange>(name: "contact_budget_range");
        modelBuilder.HasPostgresEnum<CertificateCategory>(name: "certificate_category");
        modelBuilder.HasPostgresEnum<Site>(name: "site");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FrostWoodTechDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            if (typeof(AuditableEntity).IsAssignableFrom(clrType))
            {
                var entity = modelBuilder.Entity(clrType);

                entity.Property(nameof(AuditableEntity.CreatedAt)).HasColumnName("created_at");
                entity.Property(nameof(AuditableEntity.UpdatedAt)).HasColumnName("updated_at");
                entity.Property(nameof(AuditableEntity.IsDeleted)).HasColumnName("is_deleted").HasDefaultValue(false);
                entity.Property(nameof(AuditableEntity.DeletedAt)).HasColumnName("deleted_at");
                entity.Property(nameof(AuditableEntity.DeletedBy)).HasColumnName("deleted_by");

                entity.HasIndex(nameof(AuditableEntity.DeletedAt))
                    .HasFilter("is_deleted")
                    .HasDatabaseName($"ix_{entityType.GetTableName()}_deleted_at");

                // Use IgnoreQueryFilters only where a deleted row must be found on purpose.
                entity.HasQueryFilter(BuildNotDeletedFilter(clrType));
            }

            if (typeof(SiteVisibleEntity).IsAssignableFrom(clrType))
            {
                var entity = modelBuilder.Entity(clrType);

                entity.Property(nameof(SiteVisibleEntity.ShowOnAgency)).HasColumnName("show_on_agency");
                entity.Property(nameof(SiteVisibleEntity.FeaturedOnAgency)).HasColumnName("featured_on_agency");
                entity.Property(nameof(SiteVisibleEntity.AgencySortOrder)).HasColumnName("agency_sort_order");
                entity.Property(nameof(SiteVisibleEntity.ShowOnPersonal)).HasColumnName("show_on_personal");
                entity.Property(nameof(SiteVisibleEntity.FeaturedOnPersonal)).HasColumnName("featured_on_personal");
                entity.Property(nameof(SiteVisibleEntity.PersonalSortOrder)).HasColumnName("personal_sort_order");
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        StampTimestamps();
        return base.SaveChanges();
    }

    private static LambdaExpression BuildNotDeletedFilter(Type clrType)
    {
        var parameter = Expression.Parameter(clrType, "e");
        var isDeleted = Expression.Property(parameter, nameof(AuditableEntity.IsDeleted));

        return Expression.Lambda(Expression.Not(isDeleted), parameter);
    }

    private void StampTimestamps()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Property(e => e.CreatedAt).IsModified = false;

                // OriginalValue is the value read from the database, so the delete/restore flip is detectable.
                var wasDeleted = entry.Property(e => e.IsDeleted).OriginalValue;

                if (!wasDeleted && entry.Entity.IsDeleted)
                {
                    entry.Entity.DeletedAt = now;
                }
                else if (wasDeleted && !entry.Entity.IsDeleted)
                {
                    entry.Entity.DeletedAt = null;
                    entry.Entity.DeletedBy = null;
                }
            }
        }
    }
}
