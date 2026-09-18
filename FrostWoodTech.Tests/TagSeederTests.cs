using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using FrostWoodTech.API.Auth;
using FrostWoodTech.API.Data;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Services;

namespace FrostWoodTech.Tests;

public class TagSeederTests(PostgresFixture fixture) : DatabaseTest(fixture)
{
    [Fact]
    public async Task Seeding_creates_the_starter_tags()
    {
        await SeedAsync();

        await using var db = _fixture.CreateContext();

        var react = await FindAsync(db, "react");
        Assert.True(react.IsTechnology);
        Assert.Equal(TechCategory.Frontend, react.TechnologyCategory);

        var category = await FindAsync(db, "web-application");
        Assert.False(category.IsTechnology);
        Assert.Null(category.TechnologyCategory);

        // The two names the generator would collapse to "c" and "net".
        Assert.Equal("C#", (await FindAsync(db, "csharp")).Name);
        Assert.Equal(".NET", (await FindAsync(db, "dotnet")).Name);
    }

    [Fact]
    public async Task Seeding_twice_adds_nothing_the_second_time()
    {
        await SeedAsync();

        await using (var first = _fixture.CreateContext())
        {
            var before = await SeededAsync(first);

            await SeedAsync();

            await using var second = _fixture.CreateContext();
            var after = await SeededAsync(second);

            Assert.Equal(before, after);
        }
    }

    [Fact]
    public async Task A_deleted_tag_is_not_resurrected()
    {
        await SeedAsync();

        await using var db = _fixture.CreateContext();
        var tag = await FindAsync(db, "vite");
        Assert.True((await new TagService(db, new CurrentUser()).DeleteAsync(tag.Id, CancellationToken.None)).IsSuccess);

        await SeedAsync();

        await using var raw = _fixture.CreateContext();
        var rows = await raw.Tags.IgnoreQueryFilters().Where(t => t.Slug == "vite").ToListAsync();

        Assert.True(Assert.Single(rows).IsDeleted);
    }

    [Fact]
    public async Task A_renamed_tag_is_left_alone()
    {
        await SeedAsync();

        await using var db = _fixture.CreateContext();
        var tag = await FindAsync(db, "cloudflare");
        var renamed = await new TagService(db, new CurrentUser()).UpdateAsync(
            tag.Id,
            new UpdateTagRequest { Name = "Cloudflare Workers", Slug = tag.Slug, IsTechnology = true, TechnologyCategory = TechCategory.CloudDevops },
            CancellationToken.None);

        Assert.True(renamed.IsSuccess);

        await SeedAsync();

        await using var raw = _fixture.CreateContext();
        var rows = await raw.Tags.IgnoreQueryFilters().Where(t => t.Slug == "cloudflare").ToListAsync();

        Assert.Equal("Cloudflare Workers", Assert.Single(rows).Name);
    }

    [Fact]
    public async Task Every_technology_tag_has_a_category_and_every_category_tag_has_none()
    {
        await SeedAsync();

        await using var db = _fixture.CreateContext();
        var seeded = await db.Tags.IgnoreQueryFilters().ToListAsync();

        // The rule TagService.Validate enforces, so a bad seed entry fails here and not at runtime.
        Assert.All(seeded.Where(t => t.IsTechnology), t => Assert.NotNull(t.TechnologyCategory));
        Assert.All(seeded.Where(t => !t.IsTechnology), t => Assert.Null(t.TechnologyCategory));
    }

    private async Task SeedAsync()
    {
        await using var db = _fixture.CreateContext();
        await TagSeeder.EnsureSeededAsync(db, NullLogger.Instance);
    }

    private static async Task<Tag> FindAsync(FrostWoodTechDbContext db, string slug) =>
        await db.Tags.IgnoreQueryFilters().SingleAsync(t => t.Slug == slug);

    private static async Task<List<(Guid Id, string Slug)>> SeededAsync(FrostWoodTechDbContext db) =>
        [.. (await db.Tags.IgnoreQueryFilters().OrderBy(t => t.Slug).Select(t => new { t.Id, t.Slug }).ToListAsync())
            .Select(t => (t.Id, t.Slug))];
}
