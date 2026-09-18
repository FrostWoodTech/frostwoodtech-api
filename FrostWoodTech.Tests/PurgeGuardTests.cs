using Microsoft.EntityFrameworkCore;

using FrostWoodTech.API.Auth;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Services;

namespace FrostWoodTech.Tests;

/// <summary>Permanent delete against the Restrict foreign keys, where deleted rows still hold their links.</summary>
[Collection(nameof(PostgresCollection))]
public class PurgeGuardTests
{
    private static readonly CurrentUser SuperAdmin = new() { UserId = Guid.NewGuid(), Role = UserRole.SuperAdmin };

    private readonly PostgresFixture _fixture;

    public PurgeGuardTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Purging_a_tag_is_blocked_by_a_deleted_project_that_still_links_it()
    {
        await using var db = _fixture.CreateContext();
        var tags = new TagService(db, SuperAdmin);
        var projects = new ProjectService(db, new FakeMediaService(), SuperAdmin);

        var tag = (await tags.CreateAsync(
            new CreateTagRequest { Name = $"Tag {Guid.NewGuid():N}", IsTechnology = false }, CancellationToken.None)).Value!;
        var project = (await projects.CreateAsync(NewProject(tag.Id), CancellationToken.None)).Value!;
        await projects.DeleteAsync(project.Id, CancellationToken.None);
        await tags.DeleteAsync(tag.Id, CancellationToken.None);

        var result = await tags.PurgeAsync(tag.Id, CancellationToken.None);

        Assert.Equal("tag_in_use", result.Error!.Code);
    }

    [Fact]
    public async Task Purging_a_project_deletes_its_row_and_frees_its_slug()
    {
        await using var db = _fixture.CreateContext();
        var projects = new ProjectService(db, new FakeMediaService(), SuperAdmin);
        var project = (await projects.CreateAsync(NewProject(), CancellationToken.None)).Value!;
        await projects.DeleteAsync(project.Id, CancellationToken.None);

        var result = await projects.PurgeAsync(project.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        await using var raw = _fixture.CreateContext();
        Assert.False(await raw.Projects.IgnoreQueryFilters().AnyAsync(p => p.Id == project.Id));
    }

    [Fact]
    public async Task A_deleted_project_still_reserves_its_slug_so_restore_always_succeeds()
    {
        await using var db = _fixture.CreateContext();
        var projects = new ProjectService(db, new FakeMediaService(), SuperAdmin);
        var request = NewProject();
        var project = (await projects.CreateAsync(request, CancellationToken.None)).Value!;
        await projects.DeleteAsync(project.Id, CancellationToken.None);

        var duplicate = await projects.CreateAsync(request, CancellationToken.None);
        var restored = await projects.RestoreAsync(project.Id, CancellationToken.None);

        Assert.Equal("slug_taken", duplicate.Error!.Code);
        Assert.True(restored.IsSuccess);
    }


    [Fact]
    public async Task Purging_a_review_needs_a_super_admin()
    {
        await using var db = _fixture.CreateContext();
        var admin = new ReviewService(db, new CurrentUser { Role = UserRole.Admin });

        var result = await admin.PurgeAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal("forbidden", result.Error!.Code);
    }

    private static CreateProjectRequest NewProject(Guid? tagId = null) => new()
    {
        Title = $"Project {Guid.NewGuid():N}",
        Year = 2025,
        ShortDescription = "Short.",
        Description = "Long.",
        ShowOnAgency = true,
        TagIds = tagId is null ? [] : [tagId.Value]
    };
}
