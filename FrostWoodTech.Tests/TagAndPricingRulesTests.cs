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
            null, 1, 100, CancellationToken.None);

        Assert.Contains(combos.Items, p => p.Id == combo.Value!.Id);
    }

    [Fact]
    public async Task A_tag_still_used_by_a_project_cannot_be_deleted_until_the_project_is()
    {
        await using var db = _fixture.CreateContext();
        var tags = new TagService(db);
        var projects = new ProjectService(db, new FakeMediaService());

        var tag = (await tags.CreateAsync(
            new CreateTagRequest { Name = $"Backend {Guid.NewGuid():N}" }, CancellationToken.None)).Value!;

        var project = await projects.CreateAsync(
            new CreateProjectRequest
            {
                Title = $"Tagged project {Guid.NewGuid():N}",
                Year = 2025,
                ShortDescription = "Short.",
                Description = "Long.",
                ShowOnAgency = true,
                TagIds = [tag.Id]
            },
            CancellationToken.None);
        Assert.True(project.IsSuccess, project.Error?.Message);

        Assert.Equal("tag_in_use", (await tags.DeleteAsync(tag.Id, CancellationToken.None)).Error!.Code);

        Assert.True((await projects.DeleteAsync(project.Value!.Id, CancellationToken.None)).IsSuccess);
        Assert.True((await tags.DeleteAsync(tag.Id, CancellationToken.None)).IsSuccess);
    }

    [Fact]
    public async Task A_category_tag_may_not_carry_a_technology_category()
    {
        await using var db = _fixture.CreateContext();
        var tags = new TagService(db);

        var result = await tags.CreateAsync(
            new CreateTagRequest
            {
                Name = $"Frontend {Guid.NewGuid():N}",
                IsTechnology = false,
                TechnologyCategory = TechCategory.Frontend
            },
            CancellationToken.None);

        Assert.Equal("validation_failed", result.Error!.Code);
    }

    [Fact]
    public async Task Tag_slugs_stay_unique_across_soft_deleted_tags()
    {
        await using var db = _fixture.CreateContext();
        var tags = new TagService(db);

        var name = $"Rust {Guid.NewGuid():N}";
        var original = (await tags.CreateAsync(new CreateTagRequest { Name = name }, CancellationToken.None)).Value!;
        Assert.True((await tags.DeleteAsync(original.Id, CancellationToken.None)).IsSuccess);

        var again = await tags.CreateAsync(new CreateTagRequest { Name = name }, CancellationToken.None);

        Assert.Equal("slug_taken", again.Error!.Code);
    }

    [Fact]
    public async Task The_public_tag_list_filters_by_technology_and_category()
    {
        await using var db = _fixture.CreateContext();
        var tags = new TagService(db);

        var database = (await tags.CreateAsync(
            new CreateTagRequest { Name = $"Postgres {Guid.NewGuid():N}", IsTechnology = true, TechnologyCategory = TechCategory.Database },
            CancellationToken.None)).Value!;
        var category = (await tags.CreateAsync(
            new CreateTagRequest { Name = $"Backend {Guid.NewGuid():N}" }, CancellationToken.None)).Value!;

        var databases = await tags.GetPublicTagsAsync(true, TechCategory.Database, CancellationToken.None);
        Assert.Contains(databases, t => t.Id == database.Id);
        Assert.DoesNotContain(databases, t => t.Id == category.Id);

        var categories = await tags.GetPublicTagsAsync(false, null, CancellationToken.None);
        Assert.Contains(categories, t => t.Id == category.Id);
        Assert.DoesNotContain(categories, t => t.Id == database.Id);
    }

    [Fact]
    public async Task Pricing_plans_and_their_features_can_be_reordered()
    {
        await using var db = _fixture.CreateContext();
        var pricing = new PricingService(db);

        var plan = (await pricing.CreateAsync(NewComboPlan(), CancellationToken.None)).Value!;

        var first = (await pricing.AddFeatureAsync(
            plan.Id, new AddPricingPlanFeatureRequest { Text = "Design", IsIncluded = true, SortOrder = 0 }, CancellationToken.None)).Value!;
        var second = (await pricing.AddFeatureAsync(
            plan.Id, new AddPricingPlanFeatureRequest { Text = "Hosting", IsIncluded = false, SortOrder = 1 }, CancellationToken.None)).Value!;

        Assert.True((await pricing.ReorderFeaturesAsync(
            plan.Id,
            new FeatureReorderRequest { Items = [new ReorderItem { Id = first.Id, SortOrder = 1 }, new ReorderItem { Id = second.Id, SortOrder = 0 }] },
            CancellationToken.None)).IsSuccess);

        Assert.True((await pricing.ReorderAsync(
            new PricingReorderRequest { Items = [new ReorderItem { Id = plan.Id, SortOrder = 12 }] },
            CancellationToken.None)).IsSuccess);

        var after = (await pricing.GetByIdAsync(plan.Id, CancellationToken.None)).Value!;
        Assert.Equal(12, after.SortOrder);
        Assert.Equal(0, after.Features.Single(f => f.Id == second.Id).SortOrder);
        Assert.Equal(1, after.Features.Single(f => f.Id == first.Id).SortOrder);
    }

    [Fact]
    public async Task A_feature_cannot_be_reached_through_another_plan()
    {
        await using var db = _fixture.CreateContext();
        var pricing = new PricingService(db);

        var owner = (await pricing.CreateAsync(NewComboPlan(), CancellationToken.None)).Value!;
        var other = (await pricing.CreateAsync(NewComboPlan(), CancellationToken.None)).Value!;

        var feature = (await pricing.AddFeatureAsync(
            owner.Id, new AddPricingPlanFeatureRequest { Text = "Support" }, CancellationToken.None)).Value!;

        var update = await pricing.UpdateFeatureAsync(
            other.Id, feature.Id, new UpdatePricingPlanFeatureRequest { Text = "Hijacked" }, CancellationToken.None);
        var delete = await pricing.DeleteFeatureAsync(other.Id, feature.Id, CancellationToken.None);

        Assert.Equal("not_found", update.Error!.Code);
        Assert.Equal("not_found", delete.Error!.Code);

        var blank = await pricing.AddFeatureAsync(owner.Id, new AddPricingPlanFeatureRequest { Text = " " }, CancellationToken.None);
        Assert.Equal("validation_failed", blank.Error!.Code);
    }

    [Fact]
    public async Task A_draft_or_deleted_plan_is_not_public()
    {
        await using var db = _fixture.CreateContext();
        var pricing = new PricingService(db);

        var draftRequest = NewComboPlan();
        draftRequest.IsPublished = false;
        var draft = (await pricing.CreateAsync(draftRequest, CancellationToken.None)).Value!;
        var deleted = (await pricing.CreateAsync(NewComboPlan(), CancellationToken.None)).Value!;
        Assert.True((await pricing.DeleteAsync(deleted.Id, CancellationToken.None)).IsSuccess);

        var combos = await pricing.GetPublicComboPlansAsync(null, 1, 100, CancellationToken.None);

        Assert.DoesNotContain(combos.Items, p => p.Id == draft.Id || p.Id == deleted.Id);
    }

    private static CreatePricingPlanRequest NewComboPlan() => new()
    {
        ServiceId = null,
        Name = $"Combo {Guid.NewGuid():N}",
        PriceAmount = 500m,
        Currency = "USD",
        PriceType = PriceType.Fixed,
        Description = "A combo pack.",
        IsPublished = true
    };
}
