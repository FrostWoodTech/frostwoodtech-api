using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Services;

namespace FrostWoodTech.Tests;

[Collection(nameof(PostgresCollection))]
public class TagAndPricingRulesTests
{
    private readonly PostgresFixture _fixture;

    public TagAndPricingRulesTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task A_tag_still_used_by_an_article_cannot_be_deleted()
    {
        await using var db = _fixture.CreateContext();
        var tags = new TagService(db);
        var articles = new ArticleService(db, new ArticleMediaResolver(new FakeMediaService()));

        var tag = await tags.CreateAsync(
            new CreateTagRequest { Name = $"Frontend {Guid.NewGuid():N}", IsTechnology = false },
            CancellationToken.None);

        Assert.True(tag.IsSuccess);

        var article = await articles.CreateAsync(
            new CreateArticleRequest
            {
                Title = $"Tagged {Guid.NewGuid():N}",
                Excerpt = "An excerpt.",
                IsPublished = true,
                ShowOnAgency = true,
                TagIds = [tag.Value!.Id]
            },
            CancellationToken.None);

        Assert.True(article.IsSuccess);

        var deleted = await tags.DeleteAsync(tag.Value.Id, CancellationToken.None);

        Assert.False(deleted.IsSuccess);
        Assert.Equal("tag_in_use", deleted.Error!.Code);
    }

    [Fact]
    public async Task A_technology_tag_requires_a_category()
    {
        await using var db = _fixture.CreateContext();
        var tags = new TagService(db);

        var result = await tags.CreateAsync(
            new CreateTagRequest { Name = $"React {Guid.NewGuid():N}", IsTechnology = true },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation_failed", result.Error!.Code);
    }

    /// <summary>
    /// A null price means "Custom / Contact us". Defaulting it to 0 would advertise the work as
    /// free, so this asserts the null survives the whole round trip.
    /// </summary>
    [Fact]
    public async Task A_null_price_amount_is_stored_as_null_and_not_defaulted_to_zero()
    {
        await using var db = _fixture.CreateContext();
        var pricing = new PricingService(db);

        var created = await pricing.CreateAsync(
            new CreatePricingPlanRequest
            {
                ServiceId = null,
                Name = $"Enterprise {Guid.NewGuid():N}",
                PriceAmount = null,
                Currency = "USD",
                PriceType = PriceType.Custom,
                Description = "Let's talk.",
                IsPublished = true
            },
            CancellationToken.None);

        Assert.True(created.IsSuccess);
        Assert.Null(created.Value!.PriceAmount);

        var reread = await pricing.GetByIdAsync(created.Value.Id, CancellationToken.None);

        Assert.True(reread.IsSuccess);
        Assert.Null(reread.Value!.PriceAmount);
    }

    [Fact]
    public async Task A_combo_plan_is_returned_by_the_combo_route_and_not_by_a_services_tiers()
    {
        await using var db = _fixture.CreateContext();
        var pricing = new PricingService(db);

        var combo = await pricing.CreateAsync(
            new CreatePricingPlanRequest
            {
                ServiceId = null,
                Name = $"Landing Page Combo {Guid.NewGuid():N}",
                PriceAmount = 1000m,
                Currency = "USD",
                PriceType = PriceType.Fixed,
                Description = "A combo pack.",
                IsPublished = true
            },
            CancellationToken.None);

        Assert.True(combo.IsSuccess);

        var combos = await pricing.GetPublicComboPlansAsync(
            null, 1, 50, CancellationToken.None);

        Assert.Contains(combos.Items, p => p.Id == combo.Value!.Id);
    }
}
