using Microsoft.EntityFrameworkCore;

using FrostWoodTech.API.Auth;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Services;

namespace FrostWoodTech.Tests;

[Collection(nameof(PostgresCollection))]
public class CertificateTests
{
    private readonly PostgresFixture _fixture;

    public CertificateTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task A_draft_certificate_is_admin_only()
    {
        await using var db = _fixture.CreateContext();
        var service = new CertificateService(db, new FakeMediaService(), new CurrentUser());

        var request = NewCertificate();
        request.IsPublished = false;
        var created = await service.CreateAsync(request, CancellationToken.None);
        Assert.True(created.IsSuccess);

        var published = await service.GetPublicCertificatesAsync(null, 1, 100, CancellationToken.None);
        Assert.DoesNotContain(published.Items, c => c.Id == created.Value!.Id);

        var admin = await service.GetAdminCertificatesAsync(false, request.Name, 1, 100, CancellationToken.None);
        Assert.Contains(admin.Items, c => c.Id == created.Value!.Id);
    }

    [Fact]
    public async Task The_featured_filter_narrows_the_public_list()
    {
        await using var db = _fixture.CreateContext();
        var service = new CertificateService(db, new FakeMediaService(), new CurrentUser());

        var featuredRequest = NewCertificate();
        featuredRequest.Featured = true;
        var featured = (await service.CreateAsync(featuredRequest, CancellationToken.None)).Value!;
        var plain = (await service.CreateAsync(NewCertificate(), CancellationToken.None)).Value!;

        var onlyFeatured = await service.GetPublicCertificatesAsync(true, 1, 100, CancellationToken.None);

        Assert.Contains(onlyFeatured.Items, c => c.Id == featured.Id);
        Assert.DoesNotContain(onlyFeatured.Items, c => c.Id == plain.Id);
    }

    [Fact]
    public async Task A_new_certificate_is_appended_and_reorder_changes_the_order()
    {
        await using var db = _fixture.CreateContext();
        var service = new CertificateService(db, new FakeMediaService(), new CurrentUser());

        var first = (await service.CreateAsync(NewCertificate(), CancellationToken.None)).Value!;
        var second = (await service.CreateAsync(NewCertificate(), CancellationToken.None)).Value!;
        Assert.True(second.SortOrder > first.SortOrder);

        var reordered = await service.ReorderAsync(
            new CertificateReorderRequest
            {
                Items = [new ReorderItem { Id = second.Id, SortOrder = first.SortOrder }, new ReorderItem { Id = first.Id, SortOrder = second.SortOrder }]
            },
            CancellationToken.None);
        Assert.True(reordered.IsSuccess);

        Assert.Equal(first.SortOrder, (await service.GetByIdAsync(second.Id, CancellationToken.None)).Value!.SortOrder);
    }

    [Fact]
    public async Task Reorder_rejects_an_empty_list_a_duplicate_and_an_unknown_id()
    {
        await using var db = _fixture.CreateContext();
        var service = new CertificateService(db, new FakeMediaService(), new CurrentUser());
        var created = (await service.CreateAsync(NewCertificate(), CancellationToken.None)).Value!;

        var empty = await service.ReorderAsync(new CertificateReorderRequest { Items = [] }, CancellationToken.None);
        Assert.Equal("validation_failed", empty.Error!.Code);

        var duplicate = await service.ReorderAsync(
            new CertificateReorderRequest { Items = [new ReorderItem { Id = created.Id }, new ReorderItem { Id = created.Id }] },
            CancellationToken.None);
        Assert.Equal("validation_failed", duplicate.Error!.Code);

        var unknown = await service.ReorderAsync(
            new CertificateReorderRequest { Items = [new ReorderItem { Id = Guid.NewGuid() }] },
            CancellationToken.None);
        Assert.Equal("not_found", unknown.Error!.Code);
    }

    [Fact]
    public async Task Delete_is_a_soft_delete()
    {
        await using var db = _fixture.CreateContext();
        var service = new CertificateService(db, new FakeMediaService(), new CurrentUser());
        var created = (await service.CreateAsync(NewCertificate(), CancellationToken.None)).Value!;

        Assert.True((await service.DeleteAsync(created.Id, CancellationToken.None)).IsSuccess);

        var published = await service.GetPublicCertificatesAsync(null, 1, 100, CancellationToken.None);
        Assert.DoesNotContain(published.Items, c => c.Id == created.Id);
        Assert.Equal("not_found", (await service.GetByIdAsync(created.Id, CancellationToken.None)).Error!.Code);

        await using var raw = _fixture.CreateContext();
        Assert.True(await raw.Certificates.IgnoreQueryFilters().AnyAsync(c => c.Id == created.Id && c.IsDeleted));
    }

    [Theory]
    [InlineData(nameof(CreateCertificateRequest.Name))]
    [InlineData(nameof(CreateCertificateRequest.IssuedBy))]
    [InlineData(nameof(CreateCertificateRequest.ObjectKey))]
    [InlineData(nameof(CreateCertificateRequest.Url))]
    [InlineData(nameof(CreateCertificateRequest.MimeType))]
    [InlineData(nameof(CreateCertificateRequest.AltText))]
    public async Task A_blank_required_field_is_rejected(string field)
    {
        await using var db = _fixture.CreateContext();
        var service = new CertificateService(db, new FakeMediaService(), new CurrentUser());

        var request = NewCertificate();
        typeof(CreateCertificateRequest).GetProperty(field)!.SetValue(request, "   ");

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.Equal("validation_failed", result.Error!.Code);
    }

    [Fact]
    public async Task A_missing_or_future_issued_date_is_rejected()
    {
        await using var db = _fixture.CreateContext();
        var service = new CertificateService(db, new FakeMediaService(), new CurrentUser());

        var missing = NewCertificate();
        missing.IssuedDate = default;
        Assert.Equal("validation_failed", (await service.CreateAsync(missing, CancellationToken.None)).Error!.Code);

        var future = NewCertificate();
        future.IssuedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1));
        Assert.Equal("validation_failed", (await service.CreateAsync(future, CancellationToken.None)).Error!.Code);
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("/relative/cert.pdf")]
    [InlineData("ftp://example.com/cert.pdf")]
    public async Task A_url_that_is_not_absolute_http_is_rejected(string url)
    {
        await using var db = _fixture.CreateContext();
        var service = new CertificateService(db, new FakeMediaService(), new CurrentUser());

        var request = NewCertificate();
        request.Url = url;

        Assert.Equal("validation_failed", (await service.CreateAsync(request, CancellationToken.None)).Error!.Code);
    }

    [Theory]
    [InlineData("text/html", null, null)]
    [InlineData("image/", 100, 100)]
    [InlineData("image/png", null, null)]
    [InlineData("image/png", 100, null)]
    [InlineData("image/png", 0, 100)]
    [InlineData("application/pdf", -1, -1)]
    public async Task An_unsupported_file_type_or_bad_dimensions_are_rejected(string mimeType, int? width, int? height)
    {
        await using var db = _fixture.CreateContext();
        var service = new CertificateService(db, new FakeMediaService(), new CurrentUser());

        var request = NewCertificate();
        request.MimeType = mimeType;
        request.Width = width;
        request.Height = height;

        Assert.Equal("validation_failed", (await service.CreateAsync(request, CancellationToken.None)).Error!.Code);
    }

    [Theory]
    [InlineData("application/pdf", null, null)]
    [InlineData("image/png", 1600, 1200)]
    public async Task A_pdf_without_dimensions_and_an_image_with_them_are_accepted(string mimeType, int? width, int? height)
    {
        await using var db = _fixture.CreateContext();
        var service = new CertificateService(db, new FakeMediaService(), new CurrentUser());

        var request = NewCertificate();
        request.MimeType = mimeType;
        request.Width = width;
        request.Height = height;

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
    }

    [Fact]
    public async Task Replacing_the_file_deletes_the_old_one_after_saving()
    {
        await using var db = _fixture.CreateContext();
        var media = new FakeMediaService();
        var service = new CertificateService(db, media, new CurrentUser());

        var original = NewCertificate();
        var created = (await service.CreateAsync(original, CancellationToken.None)).Value!;

        var sameFile = UpdateFrom(original);
        Assert.True((await service.UpdateAsync(created.Id, sameFile, CancellationToken.None)).IsSuccess);
        Assert.Empty(media.Deleted);

        var newFile = UpdateFrom(original);
        newFile.ObjectKey = $"frostwoodtech/certificates/{Guid.NewGuid():N}.pdf";
        var updated = await service.UpdateAsync(created.Id, newFile, CancellationToken.None);

        Assert.Equal(newFile.ObjectKey, updated.Value!.ObjectKey);
        Assert.Equal([original.ObjectKey!], media.Deleted);
    }

    [Fact]
    public async Task Soft_delete_keeps_the_stored_file()
    {
        await using var db = _fixture.CreateContext();
        var media = new FakeMediaService();
        var service = new CertificateService(db, media, new CurrentUser());
        var created = (await service.CreateAsync(NewCertificate(), CancellationToken.None)).Value!;

        Assert.True((await service.DeleteAsync(created.Id, CancellationToken.None)).IsSuccess);

        Assert.Empty(media.Deleted);
    }

    [Fact]
    public async Task Equal_sort_orders_fall_back_to_the_newest_issued_date()
    {
        await using var db = _fixture.CreateContext();
        var service = new CertificateService(db, new FakeMediaService(), new CurrentUser());

        var olderRequest = NewCertificate();
        olderRequest.IssuedDate = new DateOnly(2020, 1, 1);
        var older = (await service.CreateAsync(olderRequest, CancellationToken.None)).Value!;

        var newerRequest = NewCertificate();
        newerRequest.IssuedDate = new DateOnly(2024, 1, 1);
        var newer = (await service.CreateAsync(newerRequest, CancellationToken.None)).Value!;

        Assert.True((await service.ReorderAsync(
            new CertificateReorderRequest { Items = [new ReorderItem { Id = older.Id, SortOrder = -500 }, new ReorderItem { Id = newer.Id, SortOrder = -500 }] },
            CancellationToken.None)).IsSuccess);

        var list = (await service.GetPublicCertificatesAsync(null, 1, 2, CancellationToken.None)).Items.ToList();

        Assert.Equal([newer.Id, older.Id], list.Select(c => c.Id));
    }

    [Fact]
    public async Task Updating_an_unknown_certificate_is_not_found()
    {
        await using var db = _fixture.CreateContext();
        var service = new CertificateService(db, new FakeMediaService(), new CurrentUser());

        var result = await service.UpdateAsync(Guid.NewGuid(), UpdateFrom(NewCertificate()), CancellationToken.None);

        Assert.Equal("not_found", result.Error!.Code);
    }

    private static UpdateCertificateRequest UpdateFrom(CreateCertificateRequest source) => new()
    {
        Name = source.Name,
        IssuedBy = source.IssuedBy,
        Category = source.Category,
        IssuedDate = source.IssuedDate,
        ObjectKey = source.ObjectKey,
        Url = source.Url,
        MimeType = source.MimeType,
        Width = source.Width,
        Height = source.Height,
        AltText = source.AltText,
        IsPublished = source.IsPublished
    };

    [Fact]
    public async Task A_certificate_can_be_created_as_an_exam_with_marks()
    {
        await using var db = _fixture.CreateContext();
        var service = new CertificateService(db, new FakeMediaService(), new CurrentUser());
        var request = NewCertificate();
        request.Category = CertificateCategory.Exam;
        request.Marks = "87%";

        var created = (await service.CreateAsync(request, CancellationToken.None)).Value!;

        Assert.Equal(CertificateCategory.Exam, created.Category);
        Assert.Equal("87%", created.Marks);
    }

    [Fact]
    public async Task Marks_stay_optional_for_both_categories()
    {
        await using var db = _fixture.CreateContext();
        var service = new CertificateService(db, new FakeMediaService(), new CurrentUser());

        foreach (var category in Enum.GetValues<CertificateCategory>())
        {
            var request = NewCertificate();
            request.Category = category;
            request.Marks = null;

            var created = await service.CreateAsync(request, CancellationToken.None);

            Assert.True(created.IsSuccess);
            Assert.Null(created.Value!.Marks);
        }
    }

    [Fact]
    public async Task The_category_is_required()
    {
        await using var db = _fixture.CreateContext();
        var service = new CertificateService(db, new FakeMediaService(), new CurrentUser());
        var request = NewCertificate();
        request.Category = null;

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.Equal("validation_failed", result.Error!.Code);
    }

    [Fact]
    public async Task The_public_list_carries_the_category()
    {
        await using var db = _fixture.CreateContext();
        var service = new CertificateService(db, new FakeMediaService(), new CurrentUser());
        var request = NewCertificate();
        request.Category = CertificateCategory.Exam;
        var created = (await service.CreateAsync(request, CancellationToken.None)).Value!;

        var published = await service.GetPublicCertificatesAsync(null, 1, 100, CancellationToken.None);

        Assert.Equal(CertificateCategory.Exam, published.Items.Single(c => c.Id == created.Id).Category);
    }

    [Fact]
    public async Task Deleting_stamps_who_and_when_and_restoring_clears_it()
    {
        var admin = new CurrentUser { UserId = Guid.NewGuid(), Role = UserRole.Admin };
        await using var db = _fixture.CreateContext();
        var service = new CertificateService(db, new FakeMediaService(), admin);
        var created = (await service.CreateAsync(NewCertificate(), CancellationToken.None)).Value!;

        await service.DeleteAsync(created.Id, CancellationToken.None);

        var trash = await service.GetTrashAsync(null, 1, 100, CancellationToken.None);
        var trashed = trash.Items.Single(i => i.Id == created.Id);
        Assert.NotNull(trashed.DeletedAt);
        Assert.Equal(admin.UserId, trashed.DeletedBy);

        var restored = await service.RestoreAsync(created.Id, CancellationToken.None);

        Assert.True(restored.IsSuccess);
        await using var raw = _fixture.CreateContext();
        var row = await raw.Certificates.SingleAsync(c => c.Id == created.Id);
        Assert.False(row.IsDeleted);
        Assert.Null(row.DeletedAt);
        Assert.Null(row.DeletedBy);
    }

    [Fact]
    public async Task Only_a_super_admin_can_purge()
    {
        await using var db = _fixture.CreateContext();
        var media = new FakeMediaService();
        var admin = new CertificateService(db, media, new CurrentUser { Role = UserRole.Admin });
        var created = (await admin.CreateAsync(NewCertificate(), CancellationToken.None)).Value!;
        await admin.DeleteAsync(created.Id, CancellationToken.None);

        var result = await admin.PurgeAsync(created.Id, CancellationToken.None);

        Assert.Equal("forbidden", result.Error!.Code);
        Assert.Empty(media.Deleted);
    }

    [Fact]
    public async Task Purge_removes_the_row_and_the_stored_file()
    {
        await using var db = _fixture.CreateContext();
        var media = new FakeMediaService();
        var superAdmin = new CertificateService(db, media, new CurrentUser { Role = UserRole.SuperAdmin });
        var request = NewCertificate();
        var created = (await superAdmin.CreateAsync(request, CancellationToken.None)).Value!;
        await superAdmin.DeleteAsync(created.Id, CancellationToken.None);

        var result = await superAdmin.PurgeAsync(created.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([request.ObjectKey!], media.Deleted);
        await using var raw = _fixture.CreateContext();
        Assert.False(await raw.Certificates.IgnoreQueryFilters().AnyAsync(c => c.Id == created.Id));
    }

    [Fact]
    public async Task A_live_certificate_cannot_be_purged_or_restored()
    {
        await using var db = _fixture.CreateContext();
        var service = new CertificateService(db, new FakeMediaService(), new CurrentUser { Role = UserRole.SuperAdmin });
        var created = (await service.CreateAsync(NewCertificate(), CancellationToken.None)).Value!;

        Assert.Equal("not_found", (await service.PurgeAsync(created.Id, CancellationToken.None)).Error!.Code);
        Assert.Equal("not_found", (await service.RestoreAsync(created.Id, CancellationToken.None)).Error!.Code);
    }

    private static CreateCertificateRequest NewCertificate() => new()
    {
        Name = $"Certificate {Guid.NewGuid():N}",
        IssuedBy = "Example Academy",
        Category = CertificateCategory.Course,
        IssuedDate = new DateOnly(2025, 6, 1),
        ObjectKey = $"frostwoodtech/certificates/{Guid.NewGuid():N}.pdf",
        Url = "https://fake-storage.test/frostwoodtech/certificates/cert.pdf",
        MimeType = "application/pdf",
        AltText = "The certificate.",
        IsPublished = true
    };
}
