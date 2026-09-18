using Microsoft.EntityFrameworkCore;

using FrostWoodTech.API.Auth;
using FrostWoodTech.API.Common;
using FrostWoodTech.API.Data;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Interfaces;
using FrostWoodTech.API.Services;

namespace FrostWoodTech.Tests;

/// <summary>Delete, list in trash, restore, delete again, purge: once per content type, then the cross-entity guards.</summary>
public class TrashTests(PostgresFixture fixture) : DatabaseTest(fixture)
{
    private static readonly CurrentUser Admin = new() { UserId = Guid.NewGuid(), Role = UserRole.Admin };
    private static readonly CurrentUser SuperAdmin = new() { UserId = Guid.NewGuid(), Role = UserRole.SuperAdmin };

    [Fact]
    public async Task Faqs_can_be_trashed_restored_and_purged()
    {
        await using var db = _fixture.CreateContext();
        var service = new FaqService(db, SuperAdmin);
        var id = (await service.CreateAsync(NewFaq(), CancellationToken.None)).Value!.Id;

        await AssertLifecycleAsync(id, service.DeleteAsync, service.GetTrashAsync,
            async (i, ct) => (await service.RestoreAsync(i, ct)).IsSuccess, service.PurgeAsync,
            () => db.Faqs.IgnoreQueryFilters().AnyAsync(f => f.Id == id));
    }

    [Fact]
    public async Task Reviews_can_be_trashed_restored_and_purged()
    {
        await using var db = _fixture.CreateContext();
        var service = new ReviewService(db, SuperAdmin);
        var id = (await service.CreateAsync(
            new CreateReviewRequest
            {
                Name = $"Reviewer {Guid.NewGuid():N}",
                Country = "Sri Lanka",
                CountryCode = "LK",
                Rating = 5,
                ReviewText = "Great service."
            },
            CancellationToken.None)).Value!.Id;

        await AssertLifecycleAsync(id, service.DeleteAsync, service.GetTrashAsync,
            async (i, ct) => (await service.RestoreAsync(i, ct)).IsSuccess, service.PurgeAsync,
            () => db.Reviews.IgnoreQueryFilters().AnyAsync(r => r.Id == id));
    }

    [Fact]
    public async Task Tags_can_be_trashed_restored_and_purged()
    {
        await using var db = _fixture.CreateContext();
        var service = new TagService(db, SuperAdmin);
        var id = (await service.CreateAsync(
            new CreateTagRequest { Name = $"Tag {Guid.NewGuid():N}", IsTechnology = false }, CancellationToken.None)).Value!.Id;

        await AssertLifecycleAsync(id, service.DeleteAsync, service.GetTrashAsync,
            async (i, ct) => (await service.RestoreAsync(i, ct)).IsSuccess, service.PurgeAsync,
            () => db.Tags.IgnoreQueryFilters().AnyAsync(t => t.Id == id));
    }

    [Fact]
    public async Task Projects_can_be_trashed_restored_and_purged()
    {
        await using var db = _fixture.CreateContext();
        var service = new ProjectService(db, new FakeMediaService(), SuperAdmin);
        var id = (await service.CreateAsync(NewProject(), CancellationToken.None)).Value!.Id;

        await AssertLifecycleAsync(id, service.DeleteAsync, service.GetTrashAsync,
            async (i, ct) => (await service.RestoreAsync(i, ct)).IsSuccess, service.PurgeAsync,
            () => db.Projects.IgnoreQueryFilters().AnyAsync(p => p.Id == id));
    }

    [Fact]
    public async Task Products_can_be_trashed_restored_and_purged()
    {
        await using var db = _fixture.CreateContext();
        var service = new ProductService(db, new FakeMediaService(), SuperAdmin);
        var id = (await service.CreateAsync(NewProduct(), CancellationToken.None)).Value!.Id;

        await AssertLifecycleAsync(id, service.DeleteAsync, service.GetTrashAsync,
            async (i, ct) => (await service.RestoreAsync(i, ct)).IsSuccess, service.PurgeAsync,
            () => db.Products.IgnoreQueryFilters().AnyAsync(p => p.Id == id));
    }

    [Fact]
    public async Task Articles_can_be_trashed_restored_and_purged()
    {
        await using var db = _fixture.CreateContext();
        var service = NewArticleService(db, new FakeMediaService(), SuperAdmin);
        var id = (await service.CreateAsync(NewArticle(), CancellationToken.None)).Value!.Id;

        await AssertLifecycleAsync(id, service.DeleteAsync, service.GetTrashAsync,
            async (i, ct) => (await service.RestoreAsync(i, ct)).IsSuccess, service.PurgeAsync,
            () => db.Articles.IgnoreQueryFilters().AnyAsync(a => a.Id == id));
    }

    [Fact]
    public async Task Services_can_be_trashed_restored_and_purged()
    {
        await using var db = _fixture.CreateContext();
        var service = new ServiceCatalogService(db, new FakeMediaService(), SuperAdmin);
        var id = (await service.CreateAsync(NewService(), CancellationToken.None)).Value!.Id;

        await AssertLifecycleAsync(id, service.DeleteAsync, service.GetTrashAsync,
            async (i, ct) => (await service.RestoreAsync(i, ct)).IsSuccess, service.PurgeAsync,
            () => db.Services.IgnoreQueryFilters().AnyAsync(s => s.Id == id));
    }

    [Fact]
    public async Task Pricing_plans_can_be_trashed_restored_and_purged()
    {
        await using var db = _fixture.CreateContext();
        var service = new PricingService(db, SuperAdmin);
        var id = (await service.CreateAsync(NewPlan(null, "USD"), CancellationToken.None)).Value!.Id;

        await AssertLifecycleAsync(id, service.DeleteAsync, service.GetTrashAsync,
            async (i, ct) => (await service.RestoreAsync(i, ct)).IsSuccess, service.PurgeAsync,
            () => db.PricingPlans.IgnoreQueryFilters().AnyAsync(p => p.Id == id));
    }

    [Fact]
    public async Task Currencies_can_be_trashed_restored_and_purged()
    {
        await using var db = _fixture.CreateContext();
        var service = new CurrencyService(db, new NoRates(), SuperAdmin);
        var id = (await service.CreateAsync(NewCurrency(), CancellationToken.None)).Value!.Id;

        await AssertLifecycleAsync(id, service.DeleteAsync, service.GetTrashAsync,
            async (i, ct) => (await service.RestoreAsync(i, ct)).IsSuccess, service.PurgeAsync,
            () => db.Currencies.IgnoreQueryFilters().AnyAsync(c => c.Id == id));
    }

    [Fact]
    public async Task A_deleted_faq_cannot_come_back_under_a_deleted_service()
    {
        await using var db = _fixture.CreateContext();
        var services = new ServiceCatalogService(db, new FakeMediaService(), Admin);
        var faqs = new FaqService(db, Admin);
        var serviceId = (await services.CreateAsync(NewService(), CancellationToken.None)).Value!.Id;
        var faq = NewFaq();
        faq.ServiceId = serviceId;
        var faqId = (await faqs.CreateAsync(faq, CancellationToken.None)).Value!.Id;
        await faqs.DeleteAsync(faqId, CancellationToken.None);
        await services.DeleteAsync(serviceId, CancellationToken.None);

        var result = await faqs.RestoreAsync(faqId, CancellationToken.None);

        Assert.Equal("service_deleted", result.Error!.Code);
    }

    [Fact]
    public async Task A_deleted_plan_cannot_come_back_under_a_deleted_service()
    {
        await using var db = _fixture.CreateContext();
        var services = new ServiceCatalogService(db, new FakeMediaService(), Admin);
        var pricing = new PricingService(db, Admin);
        var serviceId = (await services.CreateAsync(NewService(), CancellationToken.None)).Value!.Id;
        var planId = (await pricing.CreateAsync(NewPlan(serviceId, "USD"), CancellationToken.None)).Value!.Id;
        await pricing.DeleteAsync(planId, CancellationToken.None);
        await services.DeleteAsync(serviceId, CancellationToken.None);

        var result = await pricing.RestoreAsync(planId, CancellationToken.None);

        Assert.Equal("service_deleted", result.Error!.Code);
    }

    [Fact]
    public async Task A_deleted_plan_cannot_come_back_when_its_currency_was_deleted_meanwhile()
    {
        await using var db = _fixture.CreateContext();
        var currencies = new CurrencyService(db, new NoRates(), Admin);
        var pricing = new PricingService(db, Admin);
        var currency = NewCurrency();
        var currencyId = (await currencies.CreateAsync(currency, CancellationToken.None)).Value!.Id;
        var planId = (await pricing.CreateAsync(NewPlan(null, currency.Code!), CancellationToken.None)).Value!.Id;
        await pricing.DeleteAsync(planId, CancellationToken.None);
        Assert.True((await currencies.DeleteAsync(currencyId, CancellationToken.None)).IsSuccess);

        var result = await pricing.RestoreAsync(planId, CancellationToken.None);

        Assert.Equal("currency_deleted", result.Error!.Code);
    }

    [Fact]
    public async Task A_currency_used_by_a_deleted_plan_cannot_be_purged()
    {
        await using var db = _fixture.CreateContext();
        var currencies = new CurrencyService(db, new NoRates(), SuperAdmin);
        var pricing = new PricingService(db, SuperAdmin);
        var currency = NewCurrency();
        var currencyId = (await currencies.CreateAsync(currency, CancellationToken.None)).Value!.Id;
        var planId = (await pricing.CreateAsync(NewPlan(null, currency.Code!), CancellationToken.None)).Value!.Id;
        await pricing.DeleteAsync(planId, CancellationToken.None);
        await currencies.DeleteAsync(currencyId, CancellationToken.None);

        var result = await currencies.PurgeAsync(currencyId, CancellationToken.None);

        Assert.Equal("currency_in_use", result.Error!.Code);
    }

    [Fact]
    public async Task A_service_holding_a_deleted_faq_cannot_be_purged()
    {
        await using var db = _fixture.CreateContext();
        var services = new ServiceCatalogService(db, new FakeMediaService(), SuperAdmin);
        var faqs = new FaqService(db, SuperAdmin);
        var serviceId = (await services.CreateAsync(NewService(), CancellationToken.None)).Value!.Id;
        var faq = NewFaq();
        faq.ServiceId = serviceId;
        var faqId = (await faqs.CreateAsync(faq, CancellationToken.None)).Value!.Id;
        await faqs.DeleteAsync(faqId, CancellationToken.None);
        await services.DeleteAsync(serviceId, CancellationToken.None);

        var result = await services.PurgeAsync(serviceId, CancellationToken.None);

        Assert.Equal("service_in_use", result.Error!.Code);
    }

    [Fact]
    public async Task A_project_linked_by_a_service_cannot_be_purged()
    {
        await using var db = _fixture.CreateContext();
        var projects = new ProjectService(db, new FakeMediaService(), SuperAdmin);
        var services = new ServiceCatalogService(db, new FakeMediaService(), SuperAdmin);
        var projectId = (await projects.CreateAsync(NewProject(), CancellationToken.None)).Value!.Id;
        var service = NewService();
        service.ProjectIds = [projectId];
        await services.CreateAsync(service, CancellationToken.None);
        await projects.DeleteAsync(projectId, CancellationToken.None);

        var result = await projects.PurgeAsync(projectId, CancellationToken.None);

        Assert.Equal("project_in_use", result.Error!.Code);
    }

    [Fact]
    public async Task Purging_a_product_removes_its_image_files()
    {
        await using var db = _fixture.CreateContext();
        var media = new FakeMediaService();
        var service = new ProductService(db, media, SuperAdmin);
        var productId = (await service.CreateAsync(NewProduct(), CancellationToken.None)).Value!.Id;
        var image = new AddProductImageRequest
        {
            ObjectKey = $"frostwoodtech/products/test/{Guid.NewGuid():N}.png",
            Url = "https://fake-storage.test/shot.png",
            AltText = "A screenshot.",
            Width = 100,
            Height = 100,
            IsPrimary = true
        };
        await service.AddImageAsync(productId, image, CancellationToken.None);
        await service.DeleteAsync(productId, CancellationToken.None);
        Assert.Empty(media.Deleted);

        await service.PurgeAsync(productId, CancellationToken.None);

        Assert.Equal([image.ObjectKey], media.Deleted);
    }

    [Fact]
    public async Task Purging_an_article_removes_its_cover_and_inline_files()
    {
        await using var db = _fixture.CreateContext();
        var media = new FakeMediaService();
        var service = NewArticleService(db, media, SuperAdmin);
        var article = NewArticle();
        article.CoverImageKey = "media://articles/a/cover.png";
        article.ContentMarkdown = "![one](media://articles/a/one.png) ![again](media://articles/a/one.png)";
        var id = (await service.CreateAsync(article, CancellationToken.None)).Value!.Id;
        await service.DeleteAsync(id, CancellationToken.None);

        await service.PurgeAsync(id, CancellationToken.None);

        Assert.Equal(["articles/a/cover.png", "articles/a/one.png"], media.Deleted.Order().ToList());
    }

    [Fact]
    public async Task Purging_an_article_leaves_a_legacy_url_cover_alone()
    {
        await using var db = _fixture.CreateContext();
        var media = new FakeMediaService();
        var service = NewArticleService(db, media, SuperAdmin);
        var article = NewArticle();
        article.CoverImageKey = "https://old.example.test/cover.png";
        var id = (await service.CreateAsync(article, CancellationToken.None)).Value!.Id;
        await service.DeleteAsync(id, CancellationToken.None);

        await service.PurgeAsync(id, CancellationToken.None);

        Assert.Empty(media.Deleted);
    }

    [Fact]
    public async Task The_trash_search_filters_by_label_and_leaves_live_rows_out()
    {
        await using var db = _fixture.CreateContext();
        var service = new FaqService(db, Admin);
        var gone = NewFaq();
        var kept = NewFaq();
        var goneId = (await service.CreateAsync(gone, CancellationToken.None)).Value!.Id;
        var keptId = (await service.CreateAsync(kept, CancellationToken.None)).Value!.Id;
        await service.DeleteAsync(goneId, CancellationToken.None);

        var hit = await service.GetTrashAsync(gone.Question, 1, 20, CancellationToken.None);
        var miss = await service.GetTrashAsync(kept.Question, 1, 20, CancellationToken.None);

        Assert.Contains(hit.Items, i => i.Id == goneId);
        Assert.DoesNotContain(miss.Items, i => i.Id == keptId);
        Assert.Empty(miss.Items);
    }

    private static async Task AssertLifecycleAsync(
        Guid id,
        Func<Guid, CancellationToken, Task<ServiceResult<bool>>> delete,
        Func<string?, int, int, CancellationToken, Task<PagedResult<TrashedItemResponse>>> trash,
        Func<Guid, CancellationToken, Task<bool>> restore,
        Func<Guid, CancellationToken, Task<ServiceResult<bool>>> purge,
        Func<Task<bool>> rowExists)
    {
        Assert.True((await delete(id, CancellationToken.None)).IsSuccess);

        var trashed = (await trash(null, 1, 100, CancellationToken.None)).Items.Single(i => i.Id == id);
        Assert.NotNull(trashed.DeletedAt);

        Assert.True(await restore(id, CancellationToken.None));
        Assert.DoesNotContain((await trash(null, 1, 100, CancellationToken.None)).Items, i => i.Id == id);

        await delete(id, CancellationToken.None);
        Assert.True((await purge(id, CancellationToken.None)).IsSuccess);
        Assert.False(await rowExists());
    }

    private static ArticleService NewArticleService(FrostWoodTechDbContext db, IMediaService media, CurrentUser user) =>
        new(db, new ArticleMediaResolver(media), media, user);

    private static CreateFaqRequest NewFaq() => new()
    {
        Question = $"How much? {Guid.NewGuid():N}",
        Answer = "It depends.",
        IsPublished = true,
        ShowOnAgency = true
    };

    private static CreateProjectRequest NewProject() => new()
    {
        Title = $"Project {Guid.NewGuid():N}",
        Year = 2025,
        ShortDescription = "Short.",
        Description = "Long.",
        ShowOnAgency = true
    };

    private static CreateProductRequest NewProduct() => new()
    {
        Name = $"Product {Guid.NewGuid():N}",
        Tagline = "One thing.",
        Description = "Details.",
        ShowOnAgency = true
    };

    private static CreateArticleRequest NewArticle() => new()
    {
        Title = $"Article {Guid.NewGuid():N}",
        Excerpt = "An excerpt.",
        ShowOnAgency = true
    };

    private static CreateServiceRequest NewService() => new()
    {
        Name = $"Service {Guid.NewGuid():N}",
        ShortDescription = "What it is.",
        ShowOnAgency = true
    };

    private static CreatePricingPlanRequest NewPlan(Guid? serviceId, string currency) => new()
    {
        ServiceId = serviceId,
        Name = $"Plan {Guid.NewGuid():N}",
        PriceAmount = 100,
        Currency = currency,
        PriceType = PriceType.Fixed,
        Description = "A plan."
    };

    private static CreateCurrencyRequest NewCurrency() => new()
    {
        Code = new string(Enumerable.Range(0, 3).Select(_ => (char)Random.Shared.Next('A', 'Z' + 1)).ToArray()),
        Name = "Test Currency",
        Symbol = "T$",
        ManualRateFromUsd = 325m,
        IsActive = true
    };

    private sealed class NoRates : IExchangeRateProvider
    {
        public Task<IReadOnlyDictionary<string, decimal>> GetRatesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, decimal>>(new Dictionary<string, decimal>());
    }
}
