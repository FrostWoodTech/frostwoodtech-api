using FrostWoodTech.API.Configuration;
using FrostWoodTech.API.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

using Npgsql;

namespace FrostWoodTech.API.Data;

/// <summary>
/// Used only by <c>dotnet ef</c>. The runtime wiring lives in <c>Program.cs</c>; this exists so
/// the CLI does not have to boot the Functions host to read the model.
/// </summary>
public class FrostWoodTechDbContextDesignTimeFactory : IDesignTimeDbContextFactory<FrostWoodTechDbContext>
{
    public FrostWoodTechDbContext CreateDbContext(string[] args)
    {
        var configuration = ConfigurationHelper.Build();

        var connectionString =
            configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Default was not configured.");

        Console.WriteLine($"Environment - DesignTimeFactory: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}");
        Console.WriteLine(
            $"Connection String - DesignTimeFactory: {configuration.GetConnectionString("Default")}");

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.MapEnum<TechCategory>("tech_category");
        dataSourceBuilder.MapEnum<PriceType>("price_type");
        dataSourceBuilder.MapEnum<UserRole>("user_role");
        dataSourceBuilder.MapEnum<UserStatus>("user_status");
        dataSourceBuilder.MapEnum<ContactSubmissionStatus>("contact_submission_status");
        dataSourceBuilder.MapEnum<ContactBudgetRange>("contact_budget_range");
        dataSourceBuilder.MapEnum<Site>("site");

        var options = new DbContextOptionsBuilder<FrostWoodTechDbContext>()
            .UseNpgsql(dataSourceBuilder.Build())
            .Options;

        return new FrostWoodTechDbContext(options);
    }
}
