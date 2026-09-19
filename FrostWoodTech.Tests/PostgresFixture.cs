using Microsoft.EntityFrameworkCore;

using Npgsql;

using FrostWoodTech.API.Data;
using FrostWoodTech.API.Enums;

using Testcontainers.PostgreSql;

namespace FrostWoodTech.Tests;

/// <summary>One Postgres container per run with real migrations; the in-memory provider can't model enums, citext or partial indexes.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    private NpgsqlDataSource _dataSource = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var builder = new NpgsqlDataSourceBuilder(_container.GetConnectionString());
        builder.MapEnum<TechCategory>("tech_category");
        builder.MapEnum<PriceType>("price_type");
        builder.MapEnum<UserRole>("user_role");
        builder.MapEnum<UserStatus>("user_status");
        builder.MapEnum<PasswordTokenPurpose>("password_token_purpose");
        builder.MapEnum<AuthAttemptAction>("auth_attempt_action");
        builder.MapEnum<ContactSubmissionStatus>("contact_submission_status");
        builder.MapEnum<ContactBudgetRange>("contact_budget_range");
        builder.MapEnum<CertificateCategory>("certificate_category");
        builder.MapEnum<Site>("site");
        _dataSource = builder.Build();

        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public FrostWoodTechDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<FrostWoodTechDbContext>()
            .UseNpgsql(_dataSource, npgsql =>
            {
                npgsql.MapEnum<TechCategory>("tech_category");
                npgsql.MapEnum<PriceType>("price_type");
                npgsql.MapEnum<UserRole>("user_role");
                npgsql.MapEnum<UserStatus>("user_status");
                npgsql.MapEnum<PasswordTokenPurpose>("password_token_purpose");
                npgsql.MapEnum<AuthAttemptAction>("auth_attempt_action");
                npgsql.MapEnum<ContactSubmissionStatus>("contact_submission_status");
                npgsql.MapEnum<ContactBudgetRange>("contact_budget_range");
                npgsql.MapEnum<CertificateCategory>("certificate_category");
                npgsql.MapEnum<Site>("site");
            })
            .Options);

    /// <summary>One TRUNCATE of every mapped table; the schema and migration history stay put.</summary>
    public async Task ResetAsync()
    {
        await using var db = CreateContext();
        var tables = db.Model.GetEntityTypes()
            .Select(t => t.GetTableName())
            .Where(name => name is not null)
            .Distinct()
            .Select(name => "\"" + name + "\"");

        // Table names come from the EF model, not from test input, so there is nothing to parameterize.
#pragma warning disable EF1002
        await db.Database.ExecuteSqlRawAsync(
            $"TRUNCATE {string.Join(", ", tables)} RESTART IDENTITY CASCADE;");
#pragma warning restore EF1002
    }

    public async Task DisposeAsync()
    {
        await _dataSource.DisposeAsync();
        await _container.DisposeAsync();
    }
}

[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
