using Microsoft.Extensions.Configuration;

namespace FrostWoodTech.API.Configuration;

public static class ConfigurationHelper
{
    public static IConfiguration Build()
    {
        // Development by default so a bare func start never loads Production settings.
        var environment =
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT")
            ?? "Development";

        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("local.settings.json", optional: true)
            .AddJsonFile($"local.settings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }
}
