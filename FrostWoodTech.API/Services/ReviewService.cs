using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Data;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;
using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Services;

public class ReviewService : IReviewService
{
    /// <summary>
    /// Rows are already kept forever (a review is real content, not a disposable attempt record),
    /// so the submission rate limit counts them directly instead of a second attempts table.
    /// </summary>
    private static readonly TimeSpan SubmissionWindow = TimeSpan.FromHours(24);

    private const int MaxSubmissionsPerIpPerWindow = 3;

    private readonly FrostWoodTechDbContext _db;

    public ReviewService(FrostWoodTechDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<ReviewSubmissionResponse>> SubmitAsync(
        DTOs.Public.CreateReviewRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var name = Blank(request.Name);
        var country = Blank(request.Country);
        var countryCode = NormalizeCountryCode(request.CountryCode);
        var position = Blank(request.Position);
        var reviewText = Blank(request.ReviewText);

        var validationError = Validate(name, country, countryCode, reviewText, request.Rating);
        if (validationError is not null)
        {
            return ServiceResult<ReviewSubmissionResponse>.Validation(validationError);
        }

        if (ipAddress is not null)
        {
            var since = DateTimeOffset.UtcNow - SubmissionWindow;
            var recentCount = await _db.Reviews
                .CountAsync(r => r.SubmitterIp == ipAddress && r.CreatedAt >= since, cancellationToken);

            if (recentCount >= MaxSubmissionsPerIpPerWindow)
            {
                return ServiceResult<ReviewSubmissionResponse>.Forbidden(
                    "too_many_submissions",
                    "Too many reviews submitted from this address. Try again later.");
            }
        }

        var review = new Review
        {
            Id = Guid.NewGuid(),
            Name = name!,
            Country = country!,
            CountryCode = countryCode!,
            Position = position,
            Rating = request.Rating,
            ReviewText = reviewText!,
            IsPublished = false,
            IsFeatured = false,
            SortOrder = 0,
            SubmitterIp = ipAddress
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<ReviewSubmissionResponse>.Success(new ReviewSubmissionResponse { Id = review.Id });
    }

    public async Task<PagedResult<ReviewResponse>> GetPublicReviewsAsync(
        ReviewSortOption sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // is_deleted is handled by the DbContext's global filter; is_published is applied here
        // and is not optional.
        var query = _db.Reviews.AsNoTracking().Where(r => r.IsPublished);

        var total = await query.CountAsync(cancellationToken);

        var items = await OrderBy(query, sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(PublicProjection)
            .ToListAsync(cancellationToken);

        return new PagedResult<ReviewResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<IReadOnlyList<ReviewResponse>> GetFeaturedForHomeAsync(
        int take,
        CancellationToken cancellationToken) =>
        await _db.Reviews.AsNoTracking()
            .Where(r => r.IsPublished && r.IsFeatured)
            .OrderBy(r => r.SortOrder)
            .Take(take)
            .Select(PublicProjection)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<AdminReviewResponse>> GetAdminReviewsAsync(
        bool? isPublished,
        bool? isFeatured,
        string? country,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.Reviews.AsNoTracking();

        if (isPublished is not null)
        {
            query = query.Where(r => r.IsPublished == isPublished);
        }

        if (isFeatured is not null)
        {
            query = query.Where(r => r.IsFeatured == isFeatured);
        }

        if (country is not null)
        {
            query = query.Where(r => r.Country == country || r.CountryCode == country);
        }

        if (search is not null)
        {
            query = query.Where(r =>
                EF.Functions.ILike(r.Name, $"%{search}%")
                || EF.Functions.ILike(r.ReviewText, $"%{search}%"));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(r => r.SortOrder)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(AdminProjection)
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminReviewResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<ServiceResult<AdminReviewResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var review = await _db.Reviews
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(AdminProjection)
            .FirstOrDefaultAsync(cancellationToken);

        return review is null ? NotFound(id) : ServiceResult<AdminReviewResponse>.Success(review);
    }

    public async Task<ServiceResult<AdminReviewResponse>> CreateAsync(
        DTOs.Admin.CreateReviewRequest request,
        CancellationToken cancellationToken)
    {
        var name = Blank(request.Name);
        var country = Blank(request.Country);
        var countryCode = NormalizeCountryCode(request.CountryCode);
        var position = Blank(request.Position);
        var reviewText = Blank(request.ReviewText);

        var validationError = Validate(name, country, countryCode, reviewText, request.Rating);
        if (validationError is not null)
        {
            return ServiceResult<AdminReviewResponse>.Validation(validationError);
        }

        // Sort order is never taken from the client — it's only ever changed via ReorderAsync,
        // so a new review is simply appended to the end of the shared order.
        var nextSortOrder = await _db.Reviews.MaxAsync(r => (int?)r.SortOrder, cancellationToken) + 1 ?? 0;

        var review = new Review
        {
            Id = Guid.NewGuid(),
            Name = name!,
            Country = country!,
            CountryCode = countryCode!,
            Position = position,
            Rating = request.Rating,
            ReviewText = reviewText!,
            IsPublished = request.IsPublished,
            IsFeatured = request.IsFeatured,
            SortOrder = nextSortOrder
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<AdminReviewResponse>.Success(ToAdminResponse(review));
    }

    public async Task<ServiceResult<AdminReviewResponse>> UpdateAsync(
        Guid id,
        UpdateReviewRequest request,
        CancellationToken cancellationToken)
    {
        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (review is null)
        {
            return NotFound(id);
        }

        var name = Blank(request.Name);
        var country = Blank(request.Country);
        var countryCode = NormalizeCountryCode(request.CountryCode);
        var position = Blank(request.Position);
        var reviewText = Blank(request.ReviewText);

        var validationError = Validate(name, country, countryCode, reviewText, request.Rating);
        if (validationError is not null)
        {
            return ServiceResult<AdminReviewResponse>.Validation(validationError);
        }

        // Sort order is untouched here — it only changes via ReorderAsync.
        review.Name = name!;
        review.Country = country!;
        review.CountryCode = countryCode!;
        review.Position = position;
        review.Rating = request.Rating;
        review.ReviewText = reviewText!;
        review.IsPublished = request.IsPublished;
        review.IsFeatured = request.IsFeatured;

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<AdminReviewResponse>.Success(ToAdminResponse(review));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (review is null)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No review with id {id}.");
        }

        review.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> ReorderAsync(
        ReviewReorderRequest request,
        CancellationToken cancellationToken)
    {
        var items = request.Items;
        if (items is null || items.Count == 0)
        {
            return ServiceResult<bool>.Validation("At least one item is required.");
        }

        var ids = items.Select(i => i.Id).ToList();
        if (ids.Distinct().Count() != ids.Count)
        {
            return ServiceResult<bool>.Validation("The same review id appears more than once.");
        }

        var reviews = await _db.Reviews
            .Where(r => ids.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        var missing = ids.Where(id => !reviews.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No review with id {string.Join(", ", missing)}.");
        }

        foreach (var item in items)
        {
            reviews[item.Id].SortOrder = item.SortOrder;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    private static IOrderedQueryable<Review> OrderBy(IQueryable<Review> query, ReviewSortOption sort) => sort switch
    {
        ReviewSortOption.Rating => query.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedAt),
        ReviewSortOption.Country => query.OrderBy(r => r.Country).ThenByDescending(r => r.CreatedAt),
        _ => query.OrderByDescending(r => r.CreatedAt)
    };

    /// <summary>Null when the review is valid, otherwise the message to hand back.</summary>
    private static string? Validate(
        string? name,
        string? country,
        string? countryCode,
        string? reviewText,
        int rating)
    {
        if (name is null)
        {
            return "Name is required.";
        }

        if (country is null)
        {
            return "Country is required.";
        }

        if (countryCode is null)
        {
            return "A 2-letter countryCode is required.";
        }

        if (reviewText is null)
        {
            return "Review text is required.";
        }

        if (rating is < 1 or > 5)
        {
            return "Rating must be between 1 and 5.";
        }

        return null;
    }

    private static string? NormalizeCountryCode(string? value)
    {
        var trimmed = Blank(value)?.ToUpperInvariant();

        return trimmed is { Length: 2 } && trimmed.All(char.IsAsciiLetter) ? trimmed : null;
    }

    private static ServiceResult<AdminReviewResponse> NotFound(Guid id) =>
        ServiceResult<AdminReviewResponse>.NotFound("not_found", $"No review with id {id}.");

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static readonly Expression<Func<Review, ReviewResponse>> PublicProjection = r => new ReviewResponse
    {
        Id = r.Id,
        Name = r.Name,
        Country = r.Country,
        CountryCode = r.CountryCode,
        Position = r.Position,
        Rating = r.Rating,
        ReviewText = r.ReviewText,
        CreatedAt = r.CreatedAt
    };

    private static readonly Expression<Func<Review, AdminReviewResponse>> AdminProjection = r => new AdminReviewResponse
    {
        Id = r.Id,
        Name = r.Name,
        Country = r.Country,
        CountryCode = r.CountryCode,
        Position = r.Position,
        Rating = r.Rating,
        ReviewText = r.ReviewText,
        IsPublished = r.IsPublished,
        IsFeatured = r.IsFeatured,
        SortOrder = r.SortOrder,
        SubmitterIp = r.SubmitterIp,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };

    /// <summary>The same shape for an entity already in memory after a write.</summary>
    private static readonly Func<Review, AdminReviewResponse> ToAdminResponse = AdminProjection.Compile();
}
