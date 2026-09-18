using FrostWoodTech.API.Auth;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Services;

namespace FrostWoodTech.Tests;

public class FaqTests(PostgresFixture fixture) : DatabaseTest(fixture)
{
    [Fact]
    public async Task A_draft_faq_is_admin_only_until_it_is_published()
    {
        await using var db = _fixture.CreateContext();
        var service = new FaqService(db, new CurrentUser());

        var request = NewFaq();
        request.IsPublished = false;

        var created = await service.CreateAsync(request, CancellationToken.None);
        Assert.True(created.IsSuccess);

        var publicBefore = await service.GetPublicFaqsAsync(Site.Agency, CancellationToken.None);
        Assert.DoesNotContain(publicBefore, f => f.Id == created.Value!.Id);

        var admin = await service.GetAdminFaqsAsync(null, null, request.Question, null, false, 1, 20, CancellationToken.None);
        Assert.Contains(admin.Items, f => f.Id == created.Value!.Id);

        var published = await service.UpdateAsync(
            created.Value!.Id,
            new UpdateFaqRequest
            {
                Question = request.Question,
                Answer = request.Answer,
                IsPublished = true,
                ShowOnAgency = true
            },
            CancellationToken.None);

        Assert.True(published.IsSuccess);

        var publicAfter = await service.GetPublicFaqsAsync(Site.Agency, CancellationToken.None);
        Assert.Contains(publicAfter, f => f.Id == created.Value.Id);
    }

    [Fact]
    public async Task Reordering_faqs_changes_their_public_order()
    {
        await using var db = _fixture.CreateContext();
        var service = new FaqService(db, new CurrentUser());

        var createdFirst = await service.CreateAsync(NewFaq(), CancellationToken.None);
        var createdSecond = await service.CreateAsync(NewFaq(), CancellationToken.None);
        Assert.True(createdFirst.IsSuccess);
        Assert.True(createdSecond.IsSuccess);

        var reordered = await service.ReorderAsync(
            new FaqReorderRequest
            {
                Items =
                [
                    new ReorderItem { Id = createdSecond.Value!.Id, SortOrder = 0 },
                    new ReorderItem { Id = createdFirst.Value!.Id, SortOrder = 1 }
                ]
            },
            CancellationToken.None);

        Assert.True(reordered.IsSuccess);

        var faqs = await service.GetPublicFaqsAsync(Site.Agency, CancellationToken.None);
        var firstIndex = faqs.ToList().FindIndex(f => f.Id == createdFirst.Value!.Id);
        var secondIndex = faqs.ToList().FindIndex(f => f.Id == createdSecond.Value!.Id);

        Assert.True(secondIndex < firstIndex);
    }

    [Fact]
    public async Task A_service_scoped_faq_never_appears_in_the_global_public_list()
    {
        await using var db = _fixture.CreateContext();
        var service = new FaqService(db, new CurrentUser());

        var serviceOffering = new ServiceOffering
        {
            Id = Guid.NewGuid(),
            Slug = $"service-{Guid.NewGuid():N}",
            Name = "A service",
            ShortDescription = "Short description.",
            ShowOnAgency = true
        };
        db.Services.Add(serviceOffering);
        await db.SaveChangesAsync(CancellationToken.None);

        var globalFaq = await service.CreateAsync(NewFaq(), CancellationToken.None);
        Assert.True(globalFaq.IsSuccess);

        var scopedRequest = NewFaq();
        scopedRequest.ServiceId = serviceOffering.Id;
        var scopedFaq = await service.CreateAsync(scopedRequest, CancellationToken.None);
        Assert.True(scopedFaq.IsSuccess);
        Assert.Equal(serviceOffering.Id, scopedFaq.Value!.ServiceId);
        Assert.Equal(serviceOffering.Name, scopedFaq.Value.ServiceName);

        var publicFaqs = await service.GetPublicFaqsAsync(Site.Agency, CancellationToken.None);
        Assert.Contains(publicFaqs, f => f.Id == globalFaq.Value!.Id);
        Assert.DoesNotContain(publicFaqs, f => f.Id == scopedFaq.Value.Id);

        var adminGlobalOnly = await service.GetAdminFaqsAsync(
            null, null, null, null, globalOnly: true, 1, 50, CancellationToken.None);
        Assert.Contains(adminGlobalOnly.Items, f => f.Id == globalFaq.Value!.Id);
        Assert.DoesNotContain(adminGlobalOnly.Items, f => f.Id == scopedFaq.Value.Id);

        var adminForService = await service.GetAdminFaqsAsync(
            null, null, null, serviceOffering.Id, globalOnly: false, 1, 50, CancellationToken.None);
        Assert.Contains(adminForService.Items, f => f.Id == scopedFaq.Value.Id);
        Assert.DoesNotContain(adminForService.Items, f => f.Id == globalFaq.Value!.Id);
    }

    [Fact]
    public async Task A_personal_only_faq_is_not_returned_for_the_agency_site()
    {
        await using var db = _fixture.CreateContext();
        var service = new FaqService(db, new CurrentUser());

        var request = NewFaq();
        request.ShowOnAgency = false;
        request.ShowOnPersonal = true;
        var created = await service.CreateAsync(request, CancellationToken.None);
        Assert.True(created.IsSuccess);

        Assert.DoesNotContain(await service.GetPublicFaqsAsync(Site.Agency, CancellationToken.None), f => f.Id == created.Value!.Id);
        Assert.Contains(await service.GetPublicFaqsAsync(Site.Personal, CancellationToken.None), f => f.Id == created.Value!.Id);
    }

    [Fact]
    public async Task A_soft_deleted_faq_disappears_from_both_surfaces()
    {
        await using var db = _fixture.CreateContext();
        var service = new FaqService(db, new CurrentUser());

        var request = NewFaq();
        var created = await service.CreateAsync(request, CancellationToken.None);
        Assert.True((await service.DeleteAsync(created.Value!.Id, CancellationToken.None)).IsSuccess);

        Assert.DoesNotContain(await service.GetPublicFaqsAsync(Site.Agency, CancellationToken.None), f => f.Id == created.Value.Id);

        var admin = await service.GetAdminFaqsAsync(null, null, request.Question, null, false, 1, 20, CancellationToken.None);
        Assert.DoesNotContain(admin.Items, f => f.Id == created.Value.Id);
    }

    [Fact]
    public async Task A_faq_for_an_unknown_service_or_without_an_answer_is_rejected()
    {
        await using var db = _fixture.CreateContext();
        var service = new FaqService(db, new CurrentUser());

        var unknownService = NewFaq();
        unknownService.ServiceId = Guid.NewGuid();
        Assert.Equal("validation_failed", (await service.CreateAsync(unknownService, CancellationToken.None)).Error!.Code);

        var noAnswer = NewFaq();
        noAnswer.Answer = "  ";
        Assert.Equal("validation_failed", (await service.CreateAsync(noAnswer, CancellationToken.None)).Error!.Code);
    }

    private static CreateFaqRequest NewFaq() => new()
    {
        Question = $"How much does it cost? {Guid.NewGuid():N}",
        Answer = "It depends on the scope.",
        IsPublished = true,
        ShowOnAgency = true
    };
}
