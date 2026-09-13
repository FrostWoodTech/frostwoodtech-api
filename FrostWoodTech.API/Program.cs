using Amazon.S3;
using FrostWoodTech.API.Auth;
using FrostWoodTech.API.Common;
using FrostWoodTech.API.Configuration;
using FrostWoodTech.API.Data;
using FrostWoodTech.API.Docs;
using FrostWoodTech.API.Email;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.ExchangeRates;
using FrostWoodTech.API.Interfaces;
using FrostWoodTech.API.Media;
using FrostWoodTech.API.Middleware;
using FrostWoodTech.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

var builder = FunctionsApplication.CreateBuilder(args);

var configuration = ConfigurationHelper.Build();

builder.Configuration.AddConfiguration(configuration);

builder.ConfigureFunctionsWebApplication();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Connection string 'Default' is missing. Use Neon's pooled connection string.");

// Enums are mapped on both the data source and the EF model; either alone sends ints.
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.MapEnum<TechCategory>("tech_category");
dataSourceBuilder.MapEnum<PriceType>("price_type");
dataSourceBuilder.MapEnum<UserRole>("user_role");
dataSourceBuilder.MapEnum<UserStatus>("user_status");
dataSourceBuilder.MapEnum<PasswordTokenPurpose>("password_token_purpose");
dataSourceBuilder.MapEnum<AuthAttemptAction>("auth_attempt_action");
dataSourceBuilder.MapEnum<ContactSubmissionStatus>("contact_submission_status");
dataSourceBuilder.MapEnum<ContactBudgetRange>("contact_budget_range");
dataSourceBuilder.MapEnum<Site>("site");
var dataSource = dataSourceBuilder.Build();

builder.Services.AddSingleton(dataSource);

builder.Services.AddDbContextPool<FrostWoodTechDbContext>(options =>
    options.UseNpgsql(dataSource, npgsql =>
    {
        npgsql.MapEnum<TechCategory>("tech_category");
        npgsql.MapEnum<PriceType>("price_type");
        npgsql.MapEnum<UserRole>("user_role");
        npgsql.MapEnum<UserStatus>("user_status");
        npgsql.MapEnum<PasswordTokenPurpose>("password_token_purpose");
        npgsql.MapEnum<AuthAttemptAction>("auth_attempt_action");
        npgsql.MapEnum<ContactSubmissionStatus>("contact_submission_status");
        npgsql.MapEnum<ContactBudgetRange>("contact_budget_range");
        npgsql.MapEnum<Site>("site");
        npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
        npgsql.CommandTimeout(30);
    }));

// Same JSON contract for IActionResult payloads as hand-serialized public responses.
builder.Services.Configure<JsonOptions>(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonDefaults.Options.PropertyNamingPolicy;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonDefaults.Options.DefaultIgnoreCondition;

    foreach (var converter in JsonDefaults.Options.Converters)
    {
        options.JsonSerializerOptions.Converters.Add(converter);
    }
});

builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IFaqService, FaqService>();
builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<ICurrencyService, CurrencyService>();

builder.Services.Configure<NeonStorageOptions>(builder.Configuration.GetSection("NeonS3"));
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var options = sp.GetRequiredService<IOptions<NeonStorageOptions>>().Value;

    return new AmazonS3Client(
        options.AccessKey,
        options.SecretKey,
        new AmazonS3Config
        {
            ServiceURL = options.Endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = options.Region,
        });
});
builder.Services.AddScoped<IMediaService, NeonStorageService>();
builder.Services.AddScoped<IArticleMediaResolver, ArticleMediaResolver>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<SuperAdminOptions>(builder.Configuration.GetSection("SuperAdmin"));
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

builder.Services.Configure<GoogleOptions>(builder.Configuration.GetSection("Google"));
// Singleton so Google's signing keys are cached.
builder.Services.AddSingleton<IGoogleTokenValidator, GoogleTokenValidator>();

builder.Services.AddScoped<ILoginRateLimiter, LoginRateLimiter>();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection("Cors"));

builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<ContactOptions>(builder.Configuration.GetSection("Contact"));

// Anything but "brevo" logs mail instead of sending.
if (string.Equals(builder.Configuration["Email:Provider"], "brevo", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHttpClient<IEmailService, BrevoEmailService>(client =>
    {
        client.BaseAddress = new Uri("https://api.brevo.com/v3/");
    });
}
else
{
    builder.Services.AddScoped<IEmailService, LoggingEmailService>();
}

builder.Services.Configure<ExchangeRateOptions>(builder.Configuration.GetSection("ExchangeRate"));
builder.Services.AddHttpClient<IExchangeRateProvider, OpenExchangeRateProvider>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<ExchangeRateOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});

// /api/docs and /api/openapi.yaml return 404 unless Docs__Enabled is set.
builder.Services.Configure<DocsOptions>(builder.Configuration.GetSection("Docs"));

// Order matters: CORS outermost so error responses get headers; the exception handler wraps auth.
builder.UseMiddleware<CorsMiddleware>();
builder.UseMiddleware<ExceptionHandlingMiddleware>();

builder.UseMiddleware<JwtAuthenticationMiddleware>();

var host = builder.Build();

// Safe on every cold start; schema changes still belong to CI migrations.
await using (var scope = host.Services.CreateAsyncScope())
{
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger(nameof(SuperAdminSeeder));

    try
    {
        await SuperAdminSeeder.EnsureSeededAsync(
            scope.ServiceProvider.GetRequiredService<FrostWoodTechDbContext>(),
            scope.ServiceProvider.GetRequiredService<IOptions<SuperAdminOptions>>().Value,
            scope.ServiceProvider.GetRequiredService<IEmailService>(),
            scope.ServiceProvider.GetRequiredService<IOptions<EmailOptions>>().Value,
            logger);
    }
    catch (Exception ex)
    {
        // A transient database failure must not stop the host from starting.
        logger.LogError(ex, "Super admin seeding failed. The host is starting anyway.");
    }

    try
    {
        await BaseCurrencySeeder.EnsureSeededAsync(
            scope.ServiceProvider.GetRequiredService<FrostWoodTechDbContext>(),
            logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Base currency seeding failed. The host is starting anyway.");
    }
}

host.Run();
