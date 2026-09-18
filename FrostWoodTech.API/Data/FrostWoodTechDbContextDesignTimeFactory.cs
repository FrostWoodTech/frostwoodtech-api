using FrostWoodTech.API.Configuration;
using FrostWoodTech.API.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

using Npgsql;

namespace FrostWoodTech.API.Data;

/// <summary>For dotnet ef only, so the CLI doesn't boot the Functions host.</summary>
public class FrostWoodTechDbContextDesignTimeFactory : IDesignTimeDbContextFactory<FrostWoodTechDbContext>
{
    public FrostWoodTechDbContext CreateDbContext(string[] args)
    {
        var configuration = ConfigurationHelper.Build();

        var connectionString =
            configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Default was not configured.");

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.MapEnum<TechCategory>("tech_category");
        dataSourceBuilder.MapEnum<PriceType>("price_type");
        dataSourceBuilder.MapEnum<UserRole>("user_role");
        dataSourceBuilder.MapEnum<UserStatus>("user_status");
        dataSourceBuilder.MapEnum<PasswordTokenPurpose>("password_token_purpose");
        dataSourceBuilder.MapEnum<AuthAttemptAction>("auth_attempt_action");
        dataSourceBuilder.MapEnum<ContactSubmissionStatus>("contact_submission_status");
        dataSourceBuilder.MapEnum<ContactBudgetRange>("contact_budget_range");
        dataSourceBuilder.MapEnum<CertificateCategory>("certificate_category");
        dataSourceBuilder.MapEnum<Site>("site");

        var options = new DbContextOptionsBuilder<FrostWoodTechDbContext>()
            .UseNpgsql(dataSourceBuilder.Build())
            .Options;

        return new FrostWoodTechDbContext(options);
    }
}
