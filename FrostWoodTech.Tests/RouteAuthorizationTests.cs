using System.Reflection;

using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Functions.Health;

namespace FrostWoodTech.Tests;

/// <summary>Auth is prefix-based in middleware, so every route must sit under a known prefix.</summary>
public class RouteAuthorizationTests
{
    private static readonly string[] KnownPrefixes = ["public/", "cms/admin/", "health", "docs", "openapi.yaml"];

    private static readonly string[] PublicWrites = ["public/reviews", "public/contact"];

    public static IEnumerable<(string Function, HttpTriggerAttribute Trigger)> Triggers() =>
        typeof(GetHealth).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttribute<FunctionAttribute>() is not null)
            .SelectMany(m => m.GetParameters()
                .Select(p => p.GetCustomAttribute<HttpTriggerAttribute>())
                .OfType<HttpTriggerAttribute>()
                .Select(trigger => (m.GetCustomAttribute<FunctionAttribute>()!.Name, trigger)));

    [Fact]
    public void Every_http_function_is_discovered()
    {
        Assert.True(Triggers().Count() > 50);
    }

    [Fact]
    public void Every_trigger_is_anonymous_because_auth_lives_in_middleware()
    {
        var keyed = Triggers().Where(t => t.Trigger.AuthLevel != AuthorizationLevel.Anonymous).Select(t => t.Function);

        Assert.Empty(keyed);
    }

    [Fact]
    public void Every_route_sits_under_a_known_prefix()
    {
        var stray = Triggers()
            .Where(t => !KnownPrefixes.Any(prefix => (t.Trigger.Route ?? string.Empty).StartsWith(prefix, StringComparison.Ordinal)))
            .Select(t => $"{t.Function}: {t.Trigger.Route}");

        Assert.Empty(stray);
    }

    [Fact]
    public void Public_routes_are_read_only_apart_from_reviews_and_contact()
    {
        var writes = Triggers()
            .Where(t => t.Trigger.Route!.StartsWith("public/", StringComparison.Ordinal))
            .Where(t => t.Trigger.Methods!.Any(m => !string.Equals(m, "get", StringComparison.OrdinalIgnoreCase)))
            .Select(t => t.Trigger.Route!)
            .Order();

        Assert.Equal(PublicWrites.Order(), writes);
    }
}
