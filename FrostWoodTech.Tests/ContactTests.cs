using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using FrostWoodTech.API.Auth;
using FrostWoodTech.API.Common;
using FrostWoodTech.API.Data;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;
using FrostWoodTech.API.Email;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Services;

namespace FrostWoodTech.Tests;

[Collection(nameof(PostgresCollection))]
public class ContactTests
{
    private readonly PostgresFixture _fixture;

    public ContactTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task A_valid_submission_lands_as_new_and_is_admin_only()
    {
        await using var db = _fixture.CreateContext();
        var service = CreateService(db);

        var result = await service.SubmitAsync(NewSubmission(), IpAddress(), CancellationToken.None);
        Assert.True(result.IsSuccess);

        var admin = await service.GetByIdAsync(result.Value!.Id, CancellationToken.None);
        Assert.True(admin.IsSuccess);
        Assert.Equal(ContactSubmissionStatus.New, admin.Value!.Status);
    }

    [Fact]
    public async Task The_public_response_carries_only_an_id()
    {
        await using var db = _fixture.CreateContext();
        var service = CreateService(db);

        var result = await service.SubmitAsync(NewSubmission(), IpAddress(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        // ContactSubmissionResponse only has an Id property — this is a compile-time guarantee,
        // but the assertion documents the intent: nothing admin-only rides along in the ack.
        Assert.NotEqual(Guid.Empty, result.Value!.Id);
    }

    [Theory]
    [InlineData(null, "a@b.test", "hello")]
    [InlineData("Name", null, "hello")]
    [InlineData("Name", "not-an-email", "hello")]
    [InlineData("Name", "a@b.test", null)]
    public async Task Missing_or_invalid_required_fields_are_rejected(string? name, string? email, string? message)
    {
        await using var db = _fixture.CreateContext();
        var service = CreateService(db);

        var request = NewSubmission();
        request.Name = name;
        request.Email = email;
        request.Message = message;

        var result = await service.SubmitAsync(request, IpAddress(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation_failed", result.Error!.Code);
    }

    [Fact]
    public async Task A_missing_site_is_rejected()
    {
        await using var db = _fixture.CreateContext();
        var service = CreateService(db);

        var request = NewSubmission();
        request.Site = null;

        var result = await service.SubmitAsync(request, IpAddress(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation_failed", result.Error!.Code);
    }

    [Fact]
    public async Task A_sixth_submission_from_the_same_ip_inside_the_window_is_rejected()
    {
        await using var db = _fixture.CreateContext();
        var service = CreateService(db);

        var ip = IpAddress();

        for (var i = 0; i < 5; i++)
        {
            var result = await service.SubmitAsync(NewSubmission(), ip, CancellationToken.None);
            Assert.True(result.IsSuccess);
        }

        var blocked = await service.SubmitAsync(NewSubmission(), ip, CancellationToken.None);

        Assert.False(blocked.IsSuccess);
        Assert.Equal(ServiceErrorKind.Forbidden, blocked.Error!.Kind);
        Assert.Equal("too_many_submissions", blocked.Error!.Code);
    }

    [Fact]
    public async Task A_filled_honeypot_is_accepted_but_filed_as_spam()
    {
        await using var db = _fixture.CreateContext();
        var service = CreateService(db);

        var request = NewSubmission();
        request.Website = "http://bot.example";

        var result = await service.SubmitAsync(request, IpAddress(), CancellationToken.None);
        Assert.True(result.IsSuccess);

        var admin = await service.GetByIdAsync(result.Value!.Id, CancellationToken.None);
        Assert.True(admin.IsSuccess);
        Assert.Equal(ContactSubmissionStatus.Spam, admin.Value!.Status);
    }

    [Fact]
    public async Task Honeypot_submissions_do_not_count_against_the_rate_limit()
    {
        await using var db = _fixture.CreateContext();
        var service = CreateService(db);

        var ip = IpAddress();

        for (var i = 0; i < 10; i++)
        {
            var honeypot = NewSubmission();
            honeypot.Website = "http://bot.example";
            var result = await service.SubmitAsync(honeypot, ip, CancellationToken.None);
            Assert.True(result.IsSuccess);
        }

        var genuine = await service.SubmitAsync(NewSubmission(), ip, CancellationToken.None);
        Assert.True(genuine.IsSuccess);
    }

    [Fact]
    public async Task Updating_status_to_replied_stamps_repliedAt()
    {
        await using var db = _fixture.CreateContext();
        var service = CreateService(db);

        var submitted = await service.SubmitAsync(NewSubmission(), IpAddress(), CancellationToken.None);
        Assert.True(submitted.IsSuccess);

        var updated = await service.UpdateAsync(
            submitted.Value!.Id,
            new UpdateContactSubmissionRequest { Status = ContactSubmissionStatus.Replied, AdminNotes = "Called back." },
            CancellationToken.None);

        Assert.True(updated.IsSuccess);
        Assert.Equal(ContactSubmissionStatus.Replied, updated.Value!.Status);
        Assert.NotNull(updated.Value!.RepliedAt);
        Assert.Equal("Called back.", updated.Value!.AdminNotes);
    }

    [Fact]
    public async Task Delete_is_a_soft_delete()
    {
        await using var db = _fixture.CreateContext();
        var service = CreateService(db);

        var submitted = await service.SubmitAsync(NewSubmission(), IpAddress(), CancellationToken.None);
        Assert.True(submitted.IsSuccess);

        var deleted = await service.DeleteAsync(submitted.Value!.Id, CancellationToken.None);
        Assert.True(deleted.IsSuccess);

        var afterDelete = await service.GetByIdAsync(submitted.Value!.Id, CancellationToken.None);
        Assert.False(afterDelete.IsSuccess);
        Assert.Equal("not_found", afterDelete.Error!.Code);
    }

    private static ContactService CreateService(FrostWoodTechDbContext db) => new(
        db,
        new LoggingEmailService(
            Options.Create(new EmailOptions { FromAddress = "no-reply@example.test", BaseUrl = "https://admin.example.test" }),
            NullLogger<LoggingEmailService>.Instance),
        Options.Create(new EmailOptions { FromAddress = "no-reply@example.test", BaseUrl = "https://admin.example.test" }),
        Options.Create(new ContactOptions { NotifyAddress = "team@example.test" }),
        Options.Create(new SuperAdminOptions()),
        new CurrentUser(),
        NullLogger<ContactService>.Instance);

    private static string IpAddress() => $"203.0.113.{Random.Shared.Next(1, 255)}-{Guid.NewGuid():N}";

    private static CreateContactSubmissionRequest NewSubmission() => new()
    {
        Name = $"Visitor {Guid.NewGuid():N}",
        Email = "visitor@example.test",
        Phone = "+94 71 234 5678",
        Company = "Acme Co",
        Subject = "New project enquiry",
        Message = "We'd like a quote for a new website.",
        BudgetRange = ContactBudgetRange.OneToFiveK,
        Site = Site.Agency
    };
}
