using FrostWoodTech.API.Data;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Services;

namespace FrostWoodTech.Tests;

[Collection(nameof(PostgresCollection))]
public class ServiceCatalogTests
{
    private readonly PostgresFixture _fixture;

    public ServiceCatalogTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Personal_only_and_draft_services_are_not_returned_for_the_agency_site()
    {
        await using var db = _fixture.CreateContext();
        var service = new ServiceCatalogService(db);

        var personalOnly = await CreateAsync(service, NewService(showOnAgency: false, showOnPersonal: true));

        var draftRequest = NewService();
        draftRequest.IsPublished = false;
        var draft = await CreateAsync(service, draftRequest);

        var agency = await service.GetPublicServicesAsync(Site.Agency, null, 1, 100, CancellationToken.None);

        Assert.DoesNotContain(agency.Items, s => s.Id == personalOnly.Id);
        Assert.DoesNotContain(agency.Items, s => s.Id == draft.Id);
        Assert.Equal("not_found", (await service.GetPublicServiceBySlugAsync(Site.Agency, personalOnly.Slug, CancellationToken.None)).Error!.Code);
    }

    [Fact]
    public async Task The_list_cards_carry_no_projects_or_faqs_but_the_detail_does()
    {
        await using var db = _fixture.CreateContext();
        var service = new ServiceCatalogService(db);

        var request = NewService();
        request.ProjectIds = [(await NewProjectAsync(db, published: true, showOnAgency: true)).Id];
        var created = await CreateAsync(service, request);

        var list = await service.GetPublicServicesAsync(Site.Agency, null, 1, 100, CancellationToken.None);
        var card = Assert.Single(list.Items, s => s.Id == created.Id);
        Assert.Null(card.Projects);
        Assert.Null(card.Faqs);

        var detail = await service.GetPublicServiceBySlugAsync(Site.Agency, created.Slug, CancellationToken.None);
        Assert.Single(detail.Value!.Projects!);
        Assert.NotNull(detail.Value.Faqs);
    }

    [Fact]
    public async Task The_detail_only_embeds_projects_and_faqs_that_are_public_on_that_site()
    {
        await using var db = _fixture.CreateContext();
        var service = new ServiceCatalogService(db);
        var faqs = new FaqService(db);

        var visible = await NewProjectAsync(db, published: true, showOnAgency: true);
        var draft = await NewProjectAsync(db, published: false, showOnAgency: true);
        var personalOnly = await NewProjectAsync(db, published: true, showOnAgency: false);

        var request = NewService();
        request.ProjectIds = [visible.Id, draft.Id, personalOnly.Id];
        var created = await CreateAsync(service, request);

        var publicFaq = await faqs.CreateAsync(NewFaq(created.Id, published: true, showOnAgency: true), CancellationToken.None);
        var draftFaq = await faqs.CreateAsync(NewFaq(created.Id, published: false, showOnAgency: true), CancellationToken.None);
        var personalFaq = await faqs.CreateAsync(NewFaq(created.Id, published: true, showOnAgency: false), CancellationToken.None);

        var detail = (await service.GetPublicServiceBySlugAsync(Site.Agency, created.Slug, CancellationToken.None)).Value!;

        Assert.Equal([visible.Id], detail.Projects!.Select(p => p.Id));
        Assert.Equal([publicFaq.Value!.Id], detail.Faqs!.Select(f => f.Id));
        Assert.DoesNotContain(detail.Faqs!, f => f.Id == draftFaq.Value!.Id || f.Id == personalFaq.Value!.Id);

        var admin = (await service.GetByIdAsync(created.Id, CancellationToken.None)).Value!;
        Assert.Equal(3, admin.Projects.Count);
    }

    [Fact]
    public async Task Updating_project_links_adds_and_removes_only_what_changed()
    {
        await using var db = _fixture.CreateContext();
        var service = new ServiceCatalogService(db);

        var kept = await NewProjectAsync(db, published: true, showOnAgency: true);
        var dropped = await NewProjectAsync(db, published: true, showOnAgency: true);
        var added = await NewProjectAsync(db, published: true, showOnAgency: true);

        var request = NewService();
        request.ProjectIds = [kept.Id, dropped.Id];
        var created = await CreateAsync(service, request);

        var update = UpdateFrom(request);
        update.Slug = created.Slug;
        update.ProjectIds = [kept.Id, added.Id];
        var updated = await service.UpdateAsync(created.Id, update, CancellationToken.None);

        Assert.True(updated.IsSuccess, updated.Error?.Message);
        Assert.Equal(
            new[] { kept.Id, added.Id }.Order(),
            updated.Value!.Projects.Select(p => p.Id).Order());
    }

    [Fact]
    public async Task An_unknown_project_id_is_rejected()
    {
        await using var db = _fixture.CreateContext();
        var service = new ServiceCatalogService(db);

        var request = NewService();
        request.ProjectIds = [Guid.NewGuid()];

        Assert.Equal("validation_failed", (await service.CreateAsync(request, CancellationToken.None)).Error!.Code);
    }

    [Fact]
    public async Task Slugs_must_be_unique_including_soft_deleted_services()
    {
        await using var db = _fixture.CreateContext();
        var service = new ServiceCatalogService(db);

        var existing = await CreateAsync(service, NewService());
        var duplicate = NewService();
        duplicate.Slug = existing.Slug;
        Assert.Equal("slug_taken", (await service.CreateAsync(duplicate, CancellationToken.None)).Error!.Code);

        Assert.True((await service.DeleteAsync(existing.Id, CancellationToken.None)).IsSuccess);
        Assert.Equal("slug_taken", (await service.CreateAsync(duplicate, CancellationToken.None)).Error!.Code);
    }

    [Theory]
    [InlineData("icon")]
    [InlineData("cta")]
    [InlineData("featured")]
    public async Task Incomplete_media_cta_or_featured_settings_are_rejected(string problem)
    {
        await using var db = _fixture.CreateContext();
        var service = new ServiceCatalogService(db);

        var request = NewService();
        switch (problem)
        {
            case "icon":
                request.IconObjectKey = "frostwoodtech/services/icon.svg";
                break;
            case "cta":
                request.PrimaryCtaLabel = "Book a call";
                break;
            case "featured":
                request.ShowOnPersonal = false;
                request.FeaturedOnPersonal = true;
                break;
        }

        Assert.Equal("validation_failed", (await service.CreateAsync(request, CancellationToken.None)).Error!.Code);
    }

    [Fact]
    public async Task Reorder_is_per_site_and_requires_a_site()
    {
        await using var db = _fixture.CreateContext();
        var service = new ServiceCatalogService(db);
        var created = await CreateAsync(service, NewService(showOnPersonal: true));

        var noSite = await service.ReorderAsync(
            new ReorderRequest { Items = [new ReorderItem { Id = created.Id }] }, CancellationToken.None);
        Assert.Equal("validation_failed", noSite.Error!.Code);

        var reordered = await service.ReorderAsync(
            new ReorderRequest { Site = Site.Personal, Items = [new ReorderItem { Id = created.Id, SortOrder = 17 }] },
            CancellationToken.None);
        Assert.True(reordered.IsSuccess);

        var after = (await service.GetByIdAsync(created.Id, CancellationToken.None)).Value!;
        Assert.Equal(17, after.PersonalSortOrder);
        Assert.Equal(created.AgencySortOrder, after.AgencySortOrder);
    }

    private static async Task<AdminServiceResponse> CreateAsync(ServiceCatalogService service, CreateServiceRequest request)
    {
        var created = await service.CreateAsync(request, CancellationToken.None);
        Assert.True(created.IsSuccess, created.Error?.Message);

        return created.Value!;
    }

    private static async Task<AdminProjectResponse> NewProjectAsync(FrostWoodTechDbContext db, bool published, bool showOnAgency)
    {
        var created = await new ProjectService(db, new FakeMediaService()).CreateAsync(
            new CreateProjectRequest
            {
                Title = $"Case study {Guid.NewGuid():N}",
                Year = 2025,
                ShortDescription = "Short.",
                Description = "Long.",
                IsPublished = published,
                ShowOnAgency = showOnAgency,
                ShowOnPersonal = !showOnAgency
            },
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.Error?.Message);

        return created.Value!;
    }

    private static CreateFaqRequest NewFaq(Guid serviceId, bool published, bool showOnAgency) => new()
    {
        Question = $"Question {Guid.NewGuid():N}?",
        Answer = "An answer.",
        ServiceId = serviceId,
        IsPublished = published,
        ShowOnAgency = showOnAgency,
        ShowOnPersonal = !showOnAgency
    };

    private static CreateServiceRequest NewService(bool showOnAgency = true, bool showOnPersonal = false) => new()
    {
        Name = $"A service {Guid.NewGuid():N}",
        ShortDescription = "What it is.",
        IsPublished = true,
        ShowOnAgency = showOnAgency,
        ShowOnPersonal = showOnPersonal
    };

    private static UpdateServiceRequest UpdateFrom(CreateServiceRequest source) => new()
    {
        Name = source.Name,
        ShortDescription = source.ShortDescription,
        IsPublished = source.IsPublished,
        ShowOnAgency = source.ShowOnAgency,
        ShowOnPersonal = source.ShowOnPersonal,
        ProjectIds = source.ProjectIds
    };
}
