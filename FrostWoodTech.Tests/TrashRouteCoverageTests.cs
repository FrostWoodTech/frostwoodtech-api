namespace FrostWoodTech.Tests;

/// <summary>Catches a content type that gained a soft delete but no way to see, restore or purge it.</summary>
public class TrashRouteCoverageTests
{
    [Theory]
    [InlineData("projects")]
    [InlineData("products")]
    [InlineData("articles")]
    [InlineData("services")]
    [InlineData("pricing-plans")]
    [InlineData("faqs")]
    [InlineData("certificates")]
    [InlineData("tags")]
    [InlineData("currencies")]
    [InlineData("reviews")]
    [InlineData("contact-submissions")]
    public void Every_content_type_has_trash_restore_and_permanent_routes(string entity)
    {
        var routes = RouteAuthorizationTests.Triggers()
            .Select(t => (Route: t.Trigger.Route, Methods: t.Trigger.Methods))
            .ToList();

        Assert.Contains(routes, r => r.Route == $"cms/admin/{entity}/trash" && r.Methods!.SequenceEqual(["get"]));
        Assert.Contains(routes, r => r.Route == $"cms/admin/{entity}/{{id:guid}}/restore" && r.Methods!.SequenceEqual(["post"]));
        Assert.Contains(routes, r => r.Route == $"cms/admin/{entity}/{{id:guid}}/permanent" && r.Methods!.SequenceEqual(["delete"]));
    }
}
