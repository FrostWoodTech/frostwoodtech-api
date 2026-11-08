using FrostWoodTech.API.Auth;
using FrostWoodTech.API.Data;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Services;

namespace FrostWoodTech.Tests;

/// <summary>Presence is checked by slug; scanning a page breaks once the shared database grows.</summary>
public class ArticleTests(PostgresFixture fixture) : DatabaseTest(fixture)
{
    [Fact]
    public async Task Unpublished_article_is_never_returned_on_the_public_surface()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var request = NewArticle();
        request.IsPublished = false;
        var created = await CreateAsync(service, request);

        Assert.Equal("not_found", (await service.GetPublicArticleBySlugAsync(created.Slug!, CancellationToken.None)).Error!.Code);

        var admin = await service.GetAdminArticlesAsync(null, created.Title, 1, 20, CancellationToken.None);
        Assert.Contains(admin.Items, a => a.Id == created.Id);
    }

    [Fact]
    public async Task Soft_deleted_article_disappears_from_both_surfaces()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var created = await CreateAsync(service, NewArticle());
        Assert.True((await service.DeleteAsync(created.Id, CancellationToken.None)).IsSuccess);

        Assert.Equal("not_found", (await service.GetPublicArticleBySlugAsync(created.Slug!, CancellationToken.None)).Error!.Code);

        var admin = await service.GetAdminArticlesAsync(null, created.Title, 1, 20, CancellationToken.None);
        Assert.DoesNotContain(admin.Items, a => a.Id == created.Id);
    }

    [Fact]
    public async Task Slugs_must_be_unique_including_soft_deleted_articles()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var existing = await CreateAsync(service, NewArticle());

        var duplicate = NewArticle();
        duplicate.Slug = existing.Slug;
        Assert.Equal("slug_taken", (await service.CreateAsync(duplicate, CancellationToken.None)).Error!.Code);

        Assert.True((await service.DeleteAsync(existing.Id, CancellationToken.None)).IsSuccess);
        Assert.Equal("slug_taken", (await service.CreateAsync(duplicate, CancellationToken.None)).Error!.Code);
    }

    [Fact]
    public async Task Updating_an_article_to_another_articles_slug_is_rejected()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var first = await CreateAsync(service, NewArticle());
        var second = await CreateAsync(service, NewArticle());

        var result = await service.UpdateAsync(
            second.Id,
            new UpdateArticleRequest
            {
                Title = second.Title,
                Excerpt = "An excerpt.",
                Slug = first.Slug,
                IsPublished = true,
            },
            CancellationToken.None);

        Assert.Equal("slug_taken", result.Error!.Code);
    }

    [Fact]
    public async Task A_title_that_yields_no_slug_is_rejected()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var request = NewArticle();
        request.Title = "日本語";

        Assert.Equal("validation_failed", (await service.CreateAsync(request, CancellationToken.None)).Error!.Code);
    }

    [Fact]
    public async Task The_tag_filter_only_returns_articles_carrying_that_tag()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var tag = (await new TagService(db, new CurrentUser()).CreateAsync(
            new CreateTagRequest { Name = $"Agentic AI {Guid.NewGuid():N}" }, CancellationToken.None)).Value!;

        var taggedRequest = NewArticle();
        taggedRequest.TagIds = [tag.Id];
        var tagged = await CreateAsync(service, taggedRequest);
        var untagged = await CreateAsync(service, NewArticle());

        var result = await service.GetPublicArticlesAsync(tag.Slug, null, 1, 100, CancellationToken.None);

        Assert.Equal([tagged.Id], result.Items.Select(a => a.Id));
        Assert.DoesNotContain(result.Items, a => a.Id == untagged.Id);
    }

    [Fact]
    public async Task Reorder_rejects_an_unknown_id()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);
        var created = await CreateAsync(service, NewArticle());
        var unknown = await service.ReorderAsync(
            new ReorderRequest { Items = [new ReorderItem { Id = Guid.NewGuid() }] }, CancellationToken.None);
        Assert.Equal("not_found", unknown.Error!.Code);
    }

    private static ArticleService NewService(FrostWoodTechDbContext db) =>
        new(db, new ArticleMediaResolver(new FakeMediaService()), new FakeMediaService(), new CurrentUser());

    private static async Task<AdminArticleResponse> CreateAsync(ArticleService service, CreateArticleRequest request)
    {
        var created = await service.CreateAsync(request, CancellationToken.None);
        Assert.True(created.IsSuccess, created.Error?.Message);

        return created.Value!;
    }

    private static CreateArticleRequest NewArticle() => new()
    {
        Title = $"An article {Guid.NewGuid():N}",
        Excerpt = "An excerpt.",
        IsPublished = true,
    };
}
