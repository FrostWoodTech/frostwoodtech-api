using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Services;

namespace FrostWoodTech.Tests;

/// <summary>
/// The guard that stops personal content appearing on the agency site. This is the rule with the
/// worst failure mode in the whole codebase — a leak is visible to the public and cannot be
/// taken back — so it is tested at the service layer where the filter actually lives.
/// </summary>
[Collection(nameof(PostgresCollection))]
public class SiteVisibilityTests
{
    private readonly PostgresFixture _fixture;

    public SiteVisibilityTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Personal_only_article_is_not_returned_for_the_agency_site()
    {
        await using var db = _fixture.CreateContext();
        var service = new ArticleService(db, new ArticleMediaResolver(new FakeMediaService()));

        var created = await service.CreateAsync(
            NewArticle("Personal only", showOnAgency: false, showOnPersonal: true),
            CancellationToken.None);

        Assert.True(created.IsSuccess);

        var personal = await service.GetPublicArticlesAsync(
            Site.Personal, null, null, 1, 50, CancellationToken.None);

        var agency = await service.GetPublicArticlesAsync(
            Site.Agency, null, null, 1, 50, CancellationToken.None);

        Assert.Contains(personal.Items, a => a.Id == created.Value!.Id);
        Assert.DoesNotContain(agency.Items, a => a.Id == created.Value!.Id);
    }

    [Fact]
    public async Task Unpublished_article_is_never_returned_on_the_public_surface()
    {
        await using var db = _fixture.CreateContext();
        var service = new ArticleService(db, new ArticleMediaResolver(new FakeMediaService()));

        var request = NewArticle("Still a draft", showOnAgency: true, showOnPersonal: false);
        request.IsPublished = false;

        var created = await service.CreateAsync(request, CancellationToken.None);
        Assert.True(created.IsSuccess);

        var agency = await service.GetPublicArticlesAsync(
            Site.Agency, null, null, 1, 50, CancellationToken.None);

        Assert.DoesNotContain(agency.Items, a => a.Id == created.Value!.Id);

        // ...but the admin surface must still show it, or drafts would be unreachable.
        var admin = await service.GetAdminArticlesAsync(
            null, null, null, false, 1, 50, CancellationToken.None);

        Assert.Contains(admin.Items, a => a.Id == created.Value!.Id);
    }

    [Fact]
    public async Task Soft_deleted_article_disappears_from_both_surfaces()
    {
        await using var db = _fixture.CreateContext();
        var service = new ArticleService(db, new ArticleMediaResolver(new FakeMediaService()));

        var created = await service.CreateAsync(
            NewArticle("Doomed", showOnAgency: true, showOnPersonal: false),
            CancellationToken.None);

        Assert.True(created.IsSuccess);

        var deleted = await service.DeleteAsync(created.Value!.Id, CancellationToken.None);
        Assert.True(deleted.IsSuccess);

        var agency = await service.GetPublicArticlesAsync(
            Site.Agency, null, null, 1, 50, CancellationToken.None);

        var admin = await service.GetAdminArticlesAsync(
            null, null, null, false, 1, 50, CancellationToken.None);

        Assert.DoesNotContain(agency.Items, a => a.Id == created.Value.Id);
        Assert.DoesNotContain(admin.Items, a => a.Id == created.Value.Id);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Featuring_an_article_on_a_site_it_is_not_shown_on_is_rejected(bool agency, bool personal)
    {
        await using var db = _fixture.CreateContext();
        var service = new ArticleService(db, new ArticleMediaResolver(new FakeMediaService()));

        var request = NewArticle("Contradictory", showOnAgency: false, showOnPersonal: false);
        request.FeaturedOnAgency = agency;
        request.FeaturedOnPersonal = personal;

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation_failed", result.Error!.Code);
    }

    private static CreateArticleRequest NewArticle(string title, bool showOnAgency, bool showOnPersonal) => new()
    {
        // Unique per run so repeated runs against the same container do not collide on the slug.
        Title = $"{title} {Guid.NewGuid():N}",
        Excerpt = "An excerpt.",
        IsPublished = true,
        ShowOnAgency = showOnAgency,
        ShowOnPersonal = showOnPersonal
    };
}
