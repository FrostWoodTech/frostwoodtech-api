using Microsoft.EntityFrameworkCore;

using FrostWoodTech.API.Auth;
using FrostWoodTech.API.Common;
using FrostWoodTech.API.Data;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Services;

namespace FrostWoodTech.Tests;

public class ProjectTests(PostgresFixture fixture) : DatabaseTest(fixture)
{
    [Fact]
    public async Task Personal_only_project_is_not_returned_for_the_agency_site()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var created = await CreateAsync(service, NewProject(showOnAgency: false, showOnPersonal: true));

        var agency = await service.GetPublicProjectsAsync(Site.Agency, null, null, null, 1, 100, CancellationToken.None);
        var personal = await service.GetPublicProjectsAsync(Site.Personal, null, null, null, 1, 100, CancellationToken.None);

        Assert.DoesNotContain(agency.Items, p => p.Id == created.Id);
        Assert.Contains(personal.Items, p => p.Id == created.Id);

        var bySlug = await service.GetPublicProjectBySlugAsync(Site.Agency, created.Slug, CancellationToken.None);
        Assert.Equal(ServiceErrorKind.NotFound, bySlug.Error!.Kind);
    }

    [Fact]
    public async Task A_draft_is_admin_only()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var request = NewProject();
        request.IsPublished = false;
        var created = await CreateAsync(service, request);

        var agency = await service.GetPublicProjectsAsync(Site.Agency, null, null, null, 1, 100, CancellationToken.None);
        Assert.DoesNotContain(agency.Items, p => p.Id == created.Id);

        var admin = await service.GetAdminProjectsAsync(null, false, created.Title, false, 1, 100, CancellationToken.None);
        Assert.Contains(admin.Items, p => p.Id == created.Id);
    }

    [Fact]
    public async Task Soft_deleted_project_disappears_from_both_surfaces()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var created = await CreateAsync(service, NewProject());

        Assert.True((await service.DeleteAsync(created.Id, CancellationToken.None)).IsSuccess);

        var agency = await service.GetPublicProjectsAsync(Site.Agency, null, null, null, 1, 100, CancellationToken.None);
        Assert.DoesNotContain(agency.Items, p => p.Id == created.Id);

        var byId = await service.GetByIdAsync(created.Id, CancellationToken.None);
        Assert.Equal("not_found", byId.Error!.Code);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Featuring_a_project_on_a_site_it_is_not_shown_on_is_rejected(bool agency, bool personal)
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var request = NewProject(showOnAgency: false, showOnPersonal: false);
        request.FeaturedOnAgency = agency;
        request.FeaturedOnPersonal = personal;

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.Equal("validation_failed", result.Error!.Code);
    }

    [Theory]
    [InlineData(1989)]
    [InlineData(3000)]
    public async Task A_year_outside_the_allowed_range_is_rejected(int year)
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var request = NewProject();
        request.Year = year;

        Assert.Equal("validation_failed", (await service.CreateAsync(request, CancellationToken.None)).Error!.Code);
    }

    [Fact]
    public async Task A_slug_is_generated_from_the_title_and_must_be_unique()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var request = NewProject();
        var created = await CreateAsync(service, request);
        Assert.Equal(SlugGenerator.Generate(request.Title!), created.Slug);

        var duplicate = NewProject();
        duplicate.Slug = created.Slug;
        var result = await service.CreateAsync(duplicate, CancellationToken.None);

        Assert.Equal(ServiceErrorKind.Conflict, result.Error!.Kind);
        Assert.Equal("slug_taken", result.Error.Code);
    }

    [Fact]
    public async Task The_slug_of_a_soft_deleted_project_is_still_taken()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var deleted = await CreateAsync(service, NewProject());
        Assert.True((await service.DeleteAsync(deleted.Id, CancellationToken.None)).IsSuccess);

        var reuse = NewProject();
        reuse.Slug = deleted.Slug;
        var result = await service.CreateAsync(reuse, CancellationToken.None);

        Assert.Equal("slug_taken", result.Error!.Code);
    }

    [Fact]
    public async Task A_title_that_yields_no_slug_is_rejected()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var request = NewProject();
        request.Title = "!!! ???";

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.Equal("validation_failed", result.Error!.Code);
    }

    [Fact]
    public async Task First_publish_stamps_published_at_and_shows_on_both_sites()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var request = NewProject(showOnAgency: false, showOnPersonal: false);
        request.IsPublished = false;
        var draft = await CreateAsync(service, request);
        Assert.Null(draft.PublishedAt);

        var published = await service.SetPublishedAsync(
            draft.Id, new SetPublishedRequest { IsPublished = true }, CancellationToken.None);

        Assert.NotNull(published.Value!.PublishedAt);
        Assert.True(published.Value.ShowOnAgency);
        Assert.True(published.Value.ShowOnPersonal);
    }

    [Fact]
    public async Task Republishing_keeps_the_original_date_and_the_editors_site_choice()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var created = await CreateAsync(service, NewProject(showOnAgency: false, showOnPersonal: true));
        var originalDate = created.PublishedAt;

        await service.SetPublishedAsync(created.Id, new SetPublishedRequest { IsPublished = false }, CancellationToken.None);
        var republished = await service.SetPublishedAsync(
            created.Id, new SetPublishedRequest { IsPublished = true }, CancellationToken.None);

        Assert.Equal(originalDate, republished.Value!.PublishedAt);
        Assert.False(republished.Value.ShowOnAgency);
        Assert.True(republished.Value.ShowOnPersonal);
    }

    [Fact]
    public async Task Reordering_one_site_changes_its_public_order_and_leaves_the_other_alone()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var first = await CreateAsync(service, NewProject(showOnPersonal: true));
        var second = await CreateAsync(service, NewProject(showOnPersonal: true));

        var reordered = await service.ReorderAsync(
            new ReorderRequest
            {
                Site = Site.Agency,
                Items =
                [
                    new ReorderItem { Id = second.Id, SortOrder = -2 },
                    new ReorderItem { Id = first.Id, SortOrder = -1 }
                ]
            },
            CancellationToken.None);
        Assert.True(reordered.IsSuccess);

        var agency = (await service.GetPublicProjectsAsync(Site.Agency, null, null, null, 1, 100, CancellationToken.None)).Items.ToList();
        Assert.True(agency.FindIndex(p => p.Id == second.Id) < agency.FindIndex(p => p.Id == first.Id));

        var firstAfter = await service.GetByIdAsync(first.Id, CancellationToken.None);
        Assert.Equal(first.PersonalSortOrder, firstAfter.Value!.PersonalSortOrder);
    }

    [Fact]
    public async Task Reorder_rejects_a_missing_site_a_duplicate_and_an_unknown_id()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var created = await CreateAsync(service, NewProject());

        var noSite = await service.ReorderAsync(
            new ReorderRequest { Items = [new ReorderItem { Id = created.Id }] }, CancellationToken.None);
        Assert.Equal("validation_failed", noSite.Error!.Code);

        var duplicate = await service.ReorderAsync(
            new ReorderRequest
            {
                Site = Site.Agency,
                Items = [new ReorderItem { Id = created.Id }, new ReorderItem { Id = created.Id, SortOrder = 1 }]
            },
            CancellationToken.None);
        Assert.Equal("validation_failed", duplicate.Error!.Code);

        var unknown = await service.ReorderAsync(
            new ReorderRequest { Site = Site.Agency, Items = [new ReorderItem { Id = Guid.NewGuid() }] },
            CancellationToken.None);
        Assert.Equal("not_found", unknown.Error!.Code);
    }

    [Fact]
    public async Task Public_list_filters_by_tag_and_by_category_tag_only()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);
        var tags = new TagService(db, new CurrentUser());

        var category = (await tags.CreateAsync(
            new CreateTagRequest { Name = $"Backend {Guid.NewGuid():N}" }, CancellationToken.None)).Value!;
        var technology = (await tags.CreateAsync(
            new CreateTagRequest
            {
                Name = $"Postgres {Guid.NewGuid():N}",
                IsTechnology = true,
                TechnologyCategory = TechCategory.Database
            },
            CancellationToken.None)).Value!;

        var tagged = NewProject();
        tagged.TagIds = [category.Id, technology.Id];
        var withTags = await CreateAsync(service, tagged);
        var untagged = await CreateAsync(service, NewProject());

        var byTag = await service.GetPublicProjectsAsync(Site.Agency, technology.Slug, null, null, 1, 100, CancellationToken.None);
        Assert.Contains(byTag.Items, p => p.Id == withTags.Id);
        Assert.DoesNotContain(byTag.Items, p => p.Id == untagged.Id);

        var byCategory = await service.GetPublicProjectsAsync(Site.Agency, null, category.Slug, null, 1, 100, CancellationToken.None);
        Assert.Contains(byCategory.Items, p => p.Id == withTags.Id);

        var technologyAsCategory = await service.GetPublicProjectsAsync(
            Site.Agency, null, technology.Slug, null, 1, 100, CancellationToken.None);
        Assert.DoesNotContain(technologyAsCategory.Items, p => p.Id == withTags.Id);
    }

    [Fact]
    public async Task An_unknown_tag_id_is_rejected()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);

        var request = NewProject();
        request.TagIds = [Guid.NewGuid()];

        Assert.Equal("validation_failed", (await service.CreateAsync(request, CancellationToken.None)).Error!.Code);
    }

    [Fact]
    public async Task The_first_image_is_always_primary()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);
        var project = await CreateAsync(service, NewProject());

        var image = await service.AddImageAsync(project.Id, NewImage("first", isPrimary: false), CancellationToken.None);

        Assert.True(image.Value!.IsPrimary);
    }

    [Fact]
    public async Task Setting_a_new_primary_image_clears_the_previous_one()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);
        var project = await CreateAsync(service, NewProject());

        await service.AddImageAsync(project.Id, NewImage("first", isPrimary: true), CancellationToken.None);
        var second = await service.AddImageAsync(project.Id, NewImage("second", isPrimary: true), CancellationToken.None);

        Assert.Equal([second.Value!.Id], await PrimaryIdsAsync(db, project.Id));
    }

    [Fact]
    public async Task Unsetting_the_primary_flag_does_not_leave_the_project_without_one()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);
        var project = await CreateAsync(service, NewProject());

        var primary = (await service.AddImageAsync(project.Id, NewImage("primary", isPrimary: true), CancellationToken.None)).Value!;
        await service.AddImageAsync(project.Id, NewImage("other", isPrimary: false), CancellationToken.None);

        var update = ImageUpdateFrom(NewImage("primary", isPrimary: false));
        var updated = await service.UpdateImageAsync(project.Id, primary.Id, update, CancellationToken.None);

        Assert.True(updated.IsSuccess);
        Assert.Equal([primary.Id], await PrimaryIdsAsync(db, project.Id));
    }

    [Fact]
    public async Task Deleting_the_primary_image_promotes_the_next_and_removes_the_stored_file()
    {
        await using var db = _fixture.CreateContext();
        var media = new FakeMediaService();
        var service = new ProjectService(db, media, new CurrentUser());
        var project = await CreateAsync(service, NewProject());

        var primaryRequest = NewImage("primary", isPrimary: true);
        var primary = (await service.AddImageAsync(project.Id, primaryRequest, CancellationToken.None)).Value!;

        var laterRequest = NewImage("later", isPrimary: false);
        laterRequest.SortOrder = 5;
        await service.AddImageAsync(project.Id, laterRequest, CancellationToken.None);

        var nextRequest = NewImage("next", isPrimary: false);
        nextRequest.SortOrder = 1;
        var next = (await service.AddImageAsync(project.Id, nextRequest, CancellationToken.None)).Value!;

        var deleted = await service.DeleteImageAsync(project.Id, primary.Id, CancellationToken.None);

        Assert.True(deleted.IsSuccess);
        Assert.Equal([next.Id], await PrimaryIdsAsync(db, project.Id));
        Assert.Equal([primaryRequest.ObjectKey!], media.Deleted);
    }

    [Fact]
    public async Task An_image_without_alt_text_or_with_bad_dimensions_is_rejected()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);
        var project = await CreateAsync(service, NewProject());

        var noAlt = NewImage("no-alt", isPrimary: false);
        noAlt.AltText = "   ";
        Assert.Equal("validation_failed", (await service.AddImageAsync(project.Id, noAlt, CancellationToken.None)).Error!.Code);

        var noSize = NewImage("no-size", isPrimary: false);
        noSize.Width = 0;
        Assert.Equal("validation_failed", (await service.AddImageAsync(project.Id, noSize, CancellationToken.None)).Error!.Code);
    }

    [Fact]
    public async Task Image_reorder_rejects_an_image_from_another_project()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db);
        var project = await CreateAsync(service, NewProject());
        var other = await CreateAsync(service, NewProject());

        var foreign = (await service.AddImageAsync(other.Id, NewImage("foreign", isPrimary: true), CancellationToken.None)).Value!;

        var result = await service.ReorderImagesAsync(
            project.Id,
            new ImageReorderRequest { Items = [new ReorderItem { Id = foreign.Id, SortOrder = 0 }] },
            CancellationToken.None);

        Assert.Equal("not_found", result.Error!.Code);
    }

    private static ProjectService NewService(FrostWoodTechDbContext db) => new(db, new FakeMediaService(), new CurrentUser());

    private static async Task<AdminProjectResponse> CreateAsync(ProjectService service, CreateProjectRequest request)
    {
        var created = await service.CreateAsync(request, CancellationToken.None);
        Assert.True(created.IsSuccess, created.Error?.Message);

        return created.Value!;
    }

    private static Task<List<Guid>> PrimaryIdsAsync(FrostWoodTechDbContext db, Guid projectId) =>
        db.ProjectImages
            .AsNoTracking()
            .Where(i => i.ProjectId == projectId && i.IsPrimary)
            .Select(i => i.Id)
            .ToListAsync(CancellationToken.None);

    private static CreateProjectRequest NewProject(bool showOnAgency = true, bool showOnPersonal = false) => new()
    {
        Title = $"A project {Guid.NewGuid():N}",
        Year = 2026,
        ShortDescription = "Short.",
        Description = "Long.",
        IsPublished = true,
        ShowOnAgency = showOnAgency,
        ShowOnPersonal = showOnPersonal
    };

    private static AddProjectImageRequest NewImage(string name, bool isPrimary) => new()
    {
        ObjectKey = $"frostwoodtech/projects/test/{name}-{Guid.NewGuid():N}",
        Url = "https://fake-storage.test/frostwoodtech/projects/test/shot.png",
        AltText = "A screenshot of the project.",
        Width = 1200,
        Height = 800,
        IsPrimary = isPrimary
    };

    private static UpdateProjectImageRequest ImageUpdateFrom(AddProjectImageRequest source) => new()
    {
        ObjectKey = source.ObjectKey,
        Url = source.Url,
        AltText = source.AltText,
        Width = source.Width,
        Height = source.Height,
        IsPrimary = source.IsPrimary,
        SortOrder = source.SortOrder
    };
}
