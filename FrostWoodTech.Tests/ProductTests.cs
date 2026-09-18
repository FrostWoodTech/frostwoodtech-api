using Microsoft.EntityFrameworkCore;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Services;

namespace FrostWoodTech.Tests;

[Collection(nameof(PostgresCollection))]
public class ProductTests
{
    private readonly PostgresFixture _fixture;

    public ProductTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Personal_only_product_is_not_returned_for_the_agency_site()
    {
        await using var db = _fixture.CreateContext();
        var service = new ProductService(db, new FakeMediaService());

        var created = await service.CreateAsync(
            NewProduct(showOnAgency: false, showOnPersonal: true),
            CancellationToken.None);

        Assert.True(created.IsSuccess);

        var personal = await service.GetPublicProductBySlugAsync(Site.Personal, created.Value!.Slug, CancellationToken.None);
        var agency = await service.GetPublicProductBySlugAsync(Site.Agency, created.Value.Slug, CancellationToken.None);
        var agencyList = await service.GetPublicProductsAsync(Site.Agency, null, 1, 100, CancellationToken.None);

        Assert.True(personal.IsSuccess);
        Assert.Equal("not_found", agency.Error!.Code);
        Assert.DoesNotContain(agencyList.Items, p => p.Id == created.Value.Id);
    }

    [Fact]
    public async Task Public_projection_flattens_the_requested_sites_flags()
    {
        await using var db = _fixture.CreateContext();
        var service = new ProductService(db, new FakeMediaService());

        var request = NewProduct(showOnAgency: true, showOnPersonal: true);
        request.FeaturedOnAgency = true;

        var created = await service.CreateAsync(request, CancellationToken.None);
        Assert.True(created.IsSuccess);

        var agency = await service.GetPublicProductBySlugAsync(
            Site.Agency, created.Value!.Slug, CancellationToken.None);

        var personal = await service.GetPublicProductBySlugAsync(
            Site.Personal, created.Value.Slug, CancellationToken.None);

        Assert.True(agency.Value!.Featured);
        Assert.False(personal.Value!.Featured);
        Assert.Equal(created.Value.AgencySortOrder, agency.Value.SortOrder);
        Assert.Equal(created.Value.PersonalSortOrder, personal.Value.SortOrder);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Featuring_a_product_on_a_site_it_is_not_shown_on_is_rejected(bool agency, bool personal)
    {
        await using var db = _fixture.CreateContext();
        var service = new ProductService(db, new FakeMediaService());

        var request = NewProduct(showOnAgency: false, showOnPersonal: false);
        request.FeaturedOnAgency = agency;
        request.FeaturedOnPersonal = personal;

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation_failed", result.Error!.Code);
    }

    [Fact]
    public async Task A_second_product_with_the_same_slug_is_rejected()
    {
        await using var db = _fixture.CreateContext();
        var service = new ProductService(db, new FakeMediaService());

        var first = await service.CreateAsync(NewProduct(), CancellationToken.None);
        Assert.True(first.IsSuccess);

        var duplicate = NewProduct();
        duplicate.Slug = first.Value!.Slug;

        var result = await service.CreateAsync(duplicate, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("slug_taken", result.Error!.Code);
    }

    [Fact]
    public async Task A_new_product_is_appended_to_the_end_of_both_orders()
    {
        await using var db = _fixture.CreateContext();
        var service = new ProductService(db, new FakeMediaService());

        var first = await service.CreateAsync(NewProduct(), CancellationToken.None);
        var second = await service.CreateAsync(NewProduct(), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);

        Assert.True(second.Value!.AgencySortOrder > first.Value!.AgencySortOrder);
        Assert.True(second.Value.PersonalSortOrder > first.Value.PersonalSortOrder);
    }

    [Fact]
    public async Task Reordering_one_site_leaves_the_other_sites_order_alone()
    {
        await using var db = _fixture.CreateContext();
        var service = new ProductService(db, new FakeMediaService());

        var created = await service.CreateAsync(NewProduct(), CancellationToken.None);
        Assert.True(created.IsSuccess);

        var before = created.Value!.AgencySortOrder;

        var result = await service.ReorderAsync(
            new ReorderRequest
            {
                Site = Site.Personal,
                Items = [new ReorderItem { Id = created.Value.Id, SortOrder = 41 }]
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var after = await service.GetByIdAsync(created.Value.Id, CancellationToken.None);

        Assert.Equal(41, after.Value!.PersonalSortOrder);
        Assert.Equal(before, after.Value.AgencySortOrder);
    }

    [Fact]
    public async Task Reorder_without_a_site_is_rejected()
    {
        await using var db = _fixture.CreateContext();
        var service = new ProductService(db, new FakeMediaService());

        var created = await service.CreateAsync(NewProduct(), CancellationToken.None);
        Assert.True(created.IsSuccess);

        var result = await service.ReorderAsync(
            new ReorderRequest
            {
                Site = null,
                Items = [new ReorderItem { Id = created.Value!.Id, SortOrder = 0 }]
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation_failed", result.Error!.Code);
    }

    [Fact]
    public async Task Setting_a_new_primary_image_clears_the_previous_one()
    {
        await using var db = _fixture.CreateContext();
        var service = new ProductService(db, new FakeMediaService());

        var product = await service.CreateAsync(NewProduct(), CancellationToken.None);
        Assert.True(product.IsSuccess);

        var first = await service.AddImageAsync(
            product.Value!.Id, NewImage("first", isPrimary: true), CancellationToken.None);

        var second = await service.AddImageAsync(
            product.Value.Id, NewImage("second", isPrimary: true), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);

        var primaries = await db.ProductImages
            .AsNoTracking()
            .Where(i => i.ProductId == product.Value.Id && i.IsPrimary)
            .Select(i => i.Id)
            .ToListAsync(CancellationToken.None);

        Assert.Equal([second.Value!.Id], primaries);
    }

    [Fact]
    public async Task An_image_without_alt_text_is_rejected()
    {
        await using var db = _fixture.CreateContext();
        var service = new ProductService(db, new FakeMediaService());

        var product = await service.CreateAsync(NewProduct(), CancellationToken.None);
        Assert.True(product.IsSuccess);

        var request = NewImage("no-alt", isPrimary: false);
        request.AltText = "   ";

        var result = await service.AddImageAsync(product.Value!.Id, request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation_failed", result.Error!.Code);
    }

    [Fact]
    public async Task Soft_deleted_product_disappears_from_both_surfaces()
    {
        await using var db = _fixture.CreateContext();
        var service = new ProductService(db, new FakeMediaService());

        var created = await service.CreateAsync(NewProduct(), CancellationToken.None);
        Assert.True(created.IsSuccess);

        var deleted = await service.DeleteAsync(created.Value!.Id, CancellationToken.None);
        Assert.True(deleted.IsSuccess);

        var agency = await service.GetPublicProductBySlugAsync(Site.Agency, created.Value.Slug, CancellationToken.None);
        var admin = await service.GetAdminProductsAsync(null, null, created.Value.Name, false, 1, 20, CancellationToken.None);

        Assert.Equal("not_found", agency.Error!.Code);
        Assert.DoesNotContain(admin.Items, p => p.Id == created.Value.Id);
    }

    [Fact]
    public async Task The_slug_of_a_soft_deleted_product_is_still_taken()
    {
        await using var db = _fixture.CreateContext();
        var service = new ProductService(db, new FakeMediaService());

        var deleted = await service.CreateAsync(NewProduct(), CancellationToken.None);
        Assert.True((await service.DeleteAsync(deleted.Value!.Id, CancellationToken.None)).IsSuccess);

        var reuse = NewProduct();
        reuse.Slug = deleted.Value.Slug;
        var result = await service.CreateAsync(reuse, CancellationToken.None);

        Assert.Equal(ServiceErrorKind.Conflict, result.Error!.Kind);
        Assert.Equal("slug_taken", result.Error.Code);
    }

    [Theory]
    [InlineData("???")]
    [InlineData("日本語")]
    public async Task A_name_that_yields_no_slug_is_rejected(string name)
    {
        await using var db = _fixture.CreateContext();
        var service = new ProductService(db, new FakeMediaService());

        var request = NewProduct();
        request.Name = name;

        Assert.Equal("validation_failed", (await service.CreateAsync(request, CancellationToken.None)).Error!.Code);
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://example.com/file")]
    [InlineData("/relative/path")]
    public async Task A_product_url_that_is_not_absolute_http_is_rejected(string url)
    {
        await using var db = _fixture.CreateContext();
        var service = new ProductService(db, new FakeMediaService());

        var request = NewProduct();
        request.ProductUrl = url;

        Assert.Equal("validation_failed", (await service.CreateAsync(request, CancellationToken.None)).Error!.Code);
    }

    [Fact]
    public async Task Republishing_keeps_the_original_date_and_the_editors_site_choice()
    {
        await using var db = _fixture.CreateContext();
        var service = new ProductService(db, new FakeMediaService());

        var created = (await service.CreateAsync(NewProduct(showOnAgency: false, showOnPersonal: true), CancellationToken.None)).Value!;

        await service.SetPublishedAsync(created.Id, new SetPublishedRequest { IsPublished = false }, CancellationToken.None);
        var republished = (await service.SetPublishedAsync(
            created.Id, new SetPublishedRequest { IsPublished = true }, CancellationToken.None)).Value!;

        Assert.Equal(created.PublishedAt, republished.PublishedAt);
        Assert.False(republished.ShowOnAgency);
        Assert.True(republished.ShowOnPersonal);
    }

    [Fact]
    public async Task Unsetting_the_primary_flag_does_not_leave_the_product_without_one()
    {
        await using var db = _fixture.CreateContext();
        var service = new ProductService(db, new FakeMediaService());
        var product = (await service.CreateAsync(NewProduct(), CancellationToken.None)).Value!;

        var primary = (await service.AddImageAsync(product.Id, NewImage("primary", isPrimary: true), CancellationToken.None)).Value!;
        await service.AddImageAsync(product.Id, NewImage("other", isPrimary: false), CancellationToken.None);

        var source = NewImage("primary", isPrimary: false);
        var updated = await service.UpdateImageAsync(
            product.Id,
            primary.Id,
            new UpdateProductImageRequest
            {
                ObjectKey = source.ObjectKey,
                Url = source.Url,
                AltText = source.AltText,
                Width = source.Width,
                Height = source.Height,
                IsPrimary = false
            },
            CancellationToken.None);

        Assert.True(updated.IsSuccess);
        Assert.Equal([primary.Id], await PrimaryIdsAsync(db, product.Id));
    }

    [Fact]
    public async Task Deleting_the_primary_image_promotes_the_next_and_removes_the_stored_file()
    {
        await using var db = _fixture.CreateContext();
        var media = new FakeMediaService();
        var service = new ProductService(db, media);
        var product = (await service.CreateAsync(NewProduct(), CancellationToken.None)).Value!;

        var primaryRequest = NewImage("primary", isPrimary: true);
        var primary = (await service.AddImageAsync(product.Id, primaryRequest, CancellationToken.None)).Value!;

        var nextRequest = NewImage("next", isPrimary: false);
        nextRequest.SortOrder = 1;
        var next = (await service.AddImageAsync(product.Id, nextRequest, CancellationToken.None)).Value!;

        Assert.True((await service.DeleteImageAsync(product.Id, primary.Id, CancellationToken.None)).IsSuccess);

        Assert.Equal([next.Id], await PrimaryIdsAsync(db, product.Id));
        Assert.Equal([primaryRequest.ObjectKey!], media.Deleted);
    }

    [Fact]
    public async Task Deleting_an_unknown_image_is_not_found_and_touches_no_storage()
    {
        await using var db = _fixture.CreateContext();
        var media = new FakeMediaService();
        var service = new ProductService(db, media);
        var product = (await service.CreateAsync(NewProduct(), CancellationToken.None)).Value!;

        var result = await service.DeleteImageAsync(product.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.Equal("not_found", result.Error!.Code);
        Assert.Empty(media.Deleted);
    }

    [Fact]
    public async Task Image_reorder_applies_sort_orders_and_rejects_duplicates()
    {
        await using var db = _fixture.CreateContext();
        var service = new ProductService(db, new FakeMediaService());
        var product = (await service.CreateAsync(NewProduct(), CancellationToken.None)).Value!;

        var image = (await service.AddImageAsync(product.Id, NewImage("one", isPrimary: true), CancellationToken.None)).Value!;

        var duplicate = await service.ReorderImagesAsync(
            product.Id,
            new ImageReorderRequest { Items = [new ReorderItem { Id = image.Id }, new ReorderItem { Id = image.Id }] },
            CancellationToken.None);
        Assert.Equal("validation_failed", duplicate.Error!.Code);

        var reordered = await service.ReorderImagesAsync(
            product.Id,
            new ImageReorderRequest { Items = [new ReorderItem { Id = image.Id, SortOrder = 9 }] },
            CancellationToken.None);
        Assert.True(reordered.IsSuccess);

        var after = (await service.GetByIdAsync(product.Id, CancellationToken.None)).Value!;
        Assert.Equal(9, Assert.Single(after.Images).SortOrder);
    }

    private static Task<List<Guid>> PrimaryIdsAsync(FrostWoodTech.API.Data.FrostWoodTechDbContext db, Guid productId) =>
        db.ProductImages
            .AsNoTracking()
            .Where(i => i.ProductId == productId && i.IsPrimary)
            .Select(i => i.Id)
            .ToListAsync(CancellationToken.None);

    private static CreateProductRequest NewProduct(bool showOnAgency = true, bool showOnPersonal = false) => new()
    {
        Name = $"A product {Guid.NewGuid():N}",
        Tagline = "Does one thing well.",
        Description = "Long **markdown** write-up.",
        PriceDetails = "Free while in beta.",
        ProductUrl = "https://example.com/the-product",
        IsPublished = true,
        ShowOnAgency = showOnAgency,
        ShowOnPersonal = showOnPersonal
    };

    private static AddProductImageRequest NewImage(string name, bool isPrimary) => new()
    {
        ObjectKey = $"frostwoodtech/products/test/{name}-{Guid.NewGuid():N}",
        Url = "https://fake-storage.test/frostwoodtech/products/test/shot.png",
        AltText = "A screenshot of the product.",
        Width = 1200,
        Height = 800,
        IsPrimary = isPrimary
    };
}
