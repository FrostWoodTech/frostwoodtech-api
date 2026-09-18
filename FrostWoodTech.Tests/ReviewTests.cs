using FrostWoodTech.API.Auth;
using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Services;

namespace FrostWoodTech.Tests;

[Collection(nameof(PostgresCollection))]
public class ReviewTests
{
    private readonly PostgresFixture _fixture;

    public ReviewTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task A_submitted_review_is_admin_only_until_it_is_published()
    {
        await using var db = _fixture.CreateContext();
        var service = new ReviewService(db, new CurrentUser());

        var submitted = await service.SubmitAsync(NewReview(), IpAddress(), CancellationToken.None);
        Assert.True(submitted.IsSuccess);

        var publicBefore = await service.GetPublicReviewsAsync(ReviewSortOption.Latest, 1, 500, CancellationToken.None);
        Assert.DoesNotContain(publicBefore.Items, r => r.Id == submitted.Value!.Id);

        var admin = await service.GetAdminReviewsAsync(null, null, null, null, 1, 500, CancellationToken.None);
        Assert.Contains(admin.Items, r => r.Id == submitted.Value!.Id);

        var published = await service.UpdateAsync(
            submitted.Value!.Id,
            new UpdateReviewRequest
            {
                Name = admin.Items.First(r => r.Id == submitted.Value!.Id).Name,
                Country = "Sri Lanka",
                CountryCode = "LK",
                Rating = 5,
                ReviewText = "Great service!",
                IsPublished = true
            },
            CancellationToken.None);

        Assert.True(published.IsSuccess);

        var publicAfter = await service.GetPublicReviewsAsync(ReviewSortOption.Latest, 1, 500, CancellationToken.None);
        Assert.Contains(publicAfter.Items, r => r.Id == submitted.Value!.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task A_rating_outside_1_to_5_is_rejected(int rating)
    {
        await using var db = _fixture.CreateContext();
        var service = new ReviewService(db, new CurrentUser());

        var request = NewReview();
        request.Rating = rating;

        var result = await service.SubmitAsync(request, IpAddress(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation_failed", result.Error!.Code);
    }

    [Theory]
    [InlineData("USA")]
    [InlineData("")]
    public async Task A_country_code_that_is_not_two_letters_is_rejected(string countryCode)
    {
        await using var db = _fixture.CreateContext();
        var service = new ReviewService(db, new CurrentUser());

        var request = NewReview();
        request.CountryCode = countryCode;

        var result = await service.SubmitAsync(request, IpAddress(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation_failed", result.Error!.Code);
    }

    [Fact]
    public async Task A_fourth_submission_from_the_same_ip_inside_the_window_is_rejected()
    {
        await using var db = _fixture.CreateContext();
        var service = new ReviewService(db, new CurrentUser());

        var ip = IpAddress();

        for (var i = 0; i < 3; i++)
        {
            var result = await service.SubmitAsync(NewReview(), ip, CancellationToken.None);
            Assert.True(result.IsSuccess);
        }

        var blocked = await service.SubmitAsync(NewReview(), ip, CancellationToken.None);

        Assert.False(blocked.IsSuccess);
        Assert.Equal(ServiceErrorKind.Forbidden, blocked.Error!.Kind);
        Assert.Equal("too_many_submissions", blocked.Error!.Code);
    }

    [Fact]
    public async Task Public_reviews_can_be_sorted_by_rating_or_country()
    {
        await using var db = _fixture.CreateContext();
        var service = new ReviewService(db, new CurrentUser());

        var low = await service.CreateAsync(NewAdminReview(rating: 1, country: "Alpha Country"), CancellationToken.None);
        var mid = await service.CreateAsync(NewAdminReview(rating: 3, country: "Bravo Country"), CancellationToken.None);
        var high = await service.CreateAsync(NewAdminReview(rating: 5, country: "Charlie Country"), CancellationToken.None);

        Assert.True(low.IsSuccess);
        Assert.True(mid.IsSuccess);
        Assert.True(high.IsSuccess);

        var byRating = await service.GetPublicReviewsAsync(ReviewSortOption.Rating, 1, 500, CancellationToken.None);
        var ratingIds = byRating.Items.Select(r => r.Id).ToList();

        Assert.True(ratingIds.IndexOf(high.Value!.Id) < ratingIds.IndexOf(mid.Value!.Id));
        Assert.True(ratingIds.IndexOf(mid.Value!.Id) < ratingIds.IndexOf(low.Value!.Id));

        var byCountry = await service.GetPublicReviewsAsync(ReviewSortOption.Country, 1, 500, CancellationToken.None);
        var countryIds = byCountry.Items.Select(r => r.Id).ToList();

        Assert.True(countryIds.IndexOf(low.Value!.Id) < countryIds.IndexOf(mid.Value!.Id));
        Assert.True(countryIds.IndexOf(mid.Value!.Id) < countryIds.IndexOf(high.Value!.Id));
    }

    [Fact]
    public async Task Only_published_and_featured_reviews_appear_in_the_home_slice()
    {
        await using var db = _fixture.CreateContext();
        var service = new ReviewService(db, new CurrentUser());

        var featured = await service.CreateAsync(
            NewAdminReview(isPublished: true, isFeatured: true),
            CancellationToken.None);

        var notFeatured = await service.CreateAsync(
            NewAdminReview(isPublished: true, isFeatured: false),
            CancellationToken.None);

        Assert.True(featured.IsSuccess);
        Assert.True(notFeatured.IsSuccess);

        var home = await service.GetFeaturedForHomeAsync(500, CancellationToken.None);

        Assert.Contains(home, r => r.Id == featured.Value!.Id);
        Assert.DoesNotContain(home, r => r.Id == notFeatured.Value!.Id);
    }

    [Fact]
    public async Task Reorder_updates_sort_order_in_one_call()
    {
        await using var db = _fixture.CreateContext();
        var service = new ReviewService(db, new CurrentUser());

        var first = await service.CreateAsync(NewAdminReview(), CancellationToken.None);
        var second = await service.CreateAsync(NewAdminReview(), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);

        var reordered = await service.ReorderAsync(
            new ReviewReorderRequest
            {
                Items =
                [
                    new ReorderItem { Id = first.Value!.Id, SortOrder = 10 },
                    new ReorderItem { Id = second.Value!.Id, SortOrder = 5 }
                ]
            },
            CancellationToken.None);

        Assert.True(reordered.IsSuccess);

        var firstAfter = await service.GetByIdAsync(first.Value!.Id, CancellationToken.None);
        var secondAfter = await service.GetByIdAsync(second.Value!.Id, CancellationToken.None);

        Assert.Equal(10, firstAfter.Value!.SortOrder);
        Assert.Equal(5, secondAfter.Value!.SortOrder);
    }

    [Fact]
    public async Task Reorder_rejects_a_duplicate_id()
    {
        await using var db = _fixture.CreateContext();
        var service = new ReviewService(db, new CurrentUser());

        var created = await service.CreateAsync(NewAdminReview(), CancellationToken.None);
        Assert.True(created.IsSuccess);

        var result = await service.ReorderAsync(
            new ReviewReorderRequest
            {
                Items =
                [
                    new ReorderItem { Id = created.Value!.Id, SortOrder = 1 },
                    new ReorderItem { Id = created.Value!.Id, SortOrder = 2 }
                ]
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation_failed", result.Error!.Code);
    }

    [Fact]
    public async Task Reorder_rejects_an_unknown_id()
    {
        await using var db = _fixture.CreateContext();
        var service = new ReviewService(db, new CurrentUser());

        var result = await service.ReorderAsync(
            new ReviewReorderRequest
            {
                Items = [new ReorderItem { Id = Guid.NewGuid(), SortOrder = 1 }]
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("not_found", result.Error!.Code);
    }

    private static string IpAddress() => $"203.0.113.{Random.Shared.Next(1, 255)}-{Guid.NewGuid():N}";

    private static FrostWoodTech.API.DTOs.Public.CreateReviewRequest NewReview() => new()
    {
        Name = $"Reviewer {Guid.NewGuid():N}",
        Country = "Sri Lanka",
        CountryCode = "LK",
        Position = "CEO",
        Rating = 5,
        ReviewText = "Great service, would recommend."
    };

    private static CreateReviewRequest NewAdminReview(
        int rating = 5,
        string country = "Sri Lanka",
        bool isPublished = true,
        bool isFeatured = false) => new()
    {
        Name = $"Reviewer {Guid.NewGuid():N}",
        Country = country,
        CountryCode = "LK",
        Position = "CEO",
        Rating = rating,
        ReviewText = "Great service, would recommend.",
        IsPublished = isPublished,
        IsFeatured = isFeatured
    };
}
