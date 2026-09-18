using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using FrostWoodTech.API.Auth;
using FrostWoodTech.API.Common;
using FrostWoodTech.API.Data;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;
using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Services;

public class CertificateService : ICertificateService
{
    private readonly FrostWoodTechDbContext _db;
    private readonly IMediaService _mediaService;
    private readonly CurrentUser _currentUser;

    public CertificateService(FrostWoodTechDbContext db, IMediaService mediaService, CurrentUser currentUser)
    {
        _db = db;
        _mediaService = mediaService;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<CertificateResponse>> GetPublicCertificatesAsync(
        bool? featured,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.Certificates.AsNoTracking().Where(c => c.IsPublished);

        if (featured is not null)
        {
            query = query.Where(c => c.Featured == featured);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.SortOrder)
            .ThenByDescending(c => c.IssuedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(PublicProjection)
            .ToListAsync(cancellationToken);

        return new PagedResult<CertificateResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<PagedResult<AdminCertificateResponse>> GetAdminCertificatesAsync(
        bool? isPublished,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.Certificates.AsNoTracking();

        if (isPublished is not null)
        {
            query = query.Where(c => c.IsPublished == isPublished);
        }

        if (search is not null)
        {
            query = query.Where(c =>
                EF.Functions.ILike(c.Name, $"%{search}%")
                || EF.Functions.ILike(c.IssuedBy, $"%{search}%"));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.SortOrder)
            .ThenByDescending(c => c.IssuedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(AdminProjection)
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminCertificateResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<ServiceResult<AdminCertificateResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var certificate = await _db.Certificates
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(AdminProjection)
            .FirstOrDefaultAsync(cancellationToken);

        return certificate is null ? NotFound(id) : ServiceResult<AdminCertificateResponse>.Success(certificate);
    }

    public async Task<ServiceResult<AdminCertificateResponse>> CreateAsync(
        CreateCertificateRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = Validate(request);
        if (validationError is not null)
        {
            return ServiceResult<AdminCertificateResponse>.Validation(validationError);
        }

        // Sort order never comes from the client; new rows go to the end.
        var nextSortOrder = await _db.Certificates.MaxAsync(c => (int?)c.SortOrder, cancellationToken) + 1 ?? 0;

        var certificate = new Certificate
        {
            Id = Guid.NewGuid(),
            Name = request.Name!.Trim(),
            IssuedBy = request.IssuedBy!.Trim(),
            Category = request.Category!.Value,
            IssuedDate = request.IssuedDate,
            Marks = Blank(request.Marks),
            ObjectKey = request.ObjectKey!.Trim(),
            Url = request.Url!.Trim(),
            MimeType = request.MimeType!.Trim(),
            Width = request.Width,
            Height = request.Height,
            AltText = request.AltText!.Trim(),
            IsPublished = request.IsPublished,
            Featured = request.Featured,
            SortOrder = nextSortOrder
        };

        _db.Certificates.Add(certificate);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(certificate.Id, cancellationToken);
    }

    public async Task<ServiceResult<AdminCertificateResponse>> UpdateAsync(
        Guid id,
        UpdateCertificateRequest request,
        CancellationToken cancellationToken)
    {
        var certificate = await _db.Certificates.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (certificate is null)
        {
            return NotFound(id);
        }

        var validationError = Validate(request);
        if (validationError is not null)
        {
            return ServiceResult<AdminCertificateResponse>.Validation(validationError);
        }

        var previousObjectKey = certificate.ObjectKey;

        certificate.Name = request.Name!.Trim();
        certificate.IssuedBy = request.IssuedBy!.Trim();
        certificate.Category = request.Category!.Value;
        certificate.IssuedDate = request.IssuedDate;
        certificate.Marks = Blank(request.Marks);
        certificate.ObjectKey = request.ObjectKey!.Trim();
        certificate.Url = request.Url!.Trim();
        certificate.MimeType = request.MimeType!.Trim();
        certificate.Width = request.Width;
        certificate.Height = request.Height;
        certificate.AltText = request.AltText!.Trim();
        certificate.IsPublished = request.IsPublished;
        certificate.Featured = request.Featured;

        await _db.SaveChangesAsync(cancellationToken);

        // After the commit: a failed delete only orphans the old file, never the row.
        if (previousObjectKey != certificate.ObjectKey)
        {
            await _mediaService.DeleteFileAsync(previousObjectKey, cancellationToken);
        }

        return await GetByIdAsync(certificate.Id, cancellationToken);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var certificate = await _db.Certificates.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (certificate is null)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No certificate with id {id}.");
        }

        certificate.IsDeleted = true;
        certificate.DeletedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<PagedResult<TrashedItemResponse>> GetTrashAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.Certificates.Trashed().AsNoTracking();

        if (search is not null)
        {
            query = query.Where(c =>
                EF.Functions.ILike(c.Name, $"%{search}%")
                || EF.Functions.ILike(c.IssuedBy, $"%{search}%"));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.DeletedAt)
            .ThenByDescending(c => c.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new TrashedItemResponse
            {
                Id = c.Id,
                Label = c.Name,
                DeletedAt = c.DeletedAt,
                DeletedBy = c.DeletedBy,
                DeletedByEmail = _db.Users.Where(u => u.Id == c.DeletedBy).Select(u => u.Email).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<TrashedItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<ServiceResult<AdminCertificateResponse>> RestoreAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var certificate = await _db.Certificates.FindTrashedAsync(id, cancellationToken);
        if (certificate is null)
        {
            return ServiceResult<AdminCertificateResponse>.NotFound(
                "not_found", $"No deleted certificate with id {id}.");
        }

        certificate.IsDeleted = false;
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ServiceResult<bool>> PurgeAsync(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.RequireSuperAdmin<bool>() is { } denied)
        {
            return denied;
        }

        var certificate = await _db.Certificates.FindTrashedAsync(id, cancellationToken);
        if (certificate is null)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No deleted certificate with id {id}.");
        }

        var objectKey = certificate.ObjectKey;

        _db.Certificates.Remove(certificate);
        await _db.SaveChangesAsync(cancellationToken);

        // After the commit: a failed delete only orphans the file, never keeps the row.
        await _mediaService.DeleteFileAsync(objectKey, cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> ReorderAsync(
        CertificateReorderRequest request,
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
            return ServiceResult<bool>.Validation("The same certificate id appears more than once.");
        }

        var certificates = await _db.Certificates
            .Where(c => ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var missing = ids.Where(id => !certificates.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No certificate with id {string.Join(", ", missing)}.");
        }

        foreach (var item in items)
        {
            certificates[item.Id].SortOrder = item.SortOrder;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    private static string? Validate(CreateCertificateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Name is required.";
        }

        if (string.IsNullOrWhiteSpace(request.IssuedBy))
        {
            return "Issued by is required.";
        }

        if (string.IsNullOrWhiteSpace(request.ObjectKey))
        {
            return "Object key is required.";
        }

        // Required, not defaulted: an omitted category must never silently become a course.
        if (request.Category is null)
        {
            return "category is required.";
        }

        if (request.IssuedDate == default)
        {
            return "issuedDate is required.";
        }

        if (request.IssuedDate > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
        {
            return "issuedDate cannot be in the future.";
        }

        if (string.IsNullOrWhiteSpace(request.Url) || !IsAbsoluteHttpUrl(request.Url.Trim()))
        {
            return "url is required and must be an absolute http(s) URL.";
        }

        var mimeType = request.MimeType?.Trim().ToLowerInvariant();
        var isPdf = mimeType == "application/pdf";
        var isImage = mimeType is not null && mimeType.StartsWith("image/", StringComparison.Ordinal) && mimeType.Length > "image/".Length;

        if (!isPdf && !isImage)
        {
            return "mimeType must be application/pdf or an image/* type.";
        }

        if (request.Width is null != request.Height is null)
        {
            return "width and height must be set together.";
        }

        if (request.Width <= 0 || request.Height <= 0)
        {
            return "width and height must be greater than zero.";
        }

        if (isImage && request.Width is null)
        {
            return "width and height are required for an image.";
        }

        if (string.IsNullOrWhiteSpace(request.AltText))
        {
            return "Alt text is required.";
        }

        return null;
    }

    private static bool IsAbsoluteHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static ServiceResult<AdminCertificateResponse> NotFound(Guid id) =>
        ServiceResult<AdminCertificateResponse>.NotFound("not_found", $"No certificate with id {id}.");

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static readonly Expression<Func<Certificate, CertificateResponse>> PublicProjection = c => new CertificateResponse
    {
        Id = c.Id,
        Name = c.Name,
        IssuedBy = c.IssuedBy,
        Category = c.Category,
        IssuedDate = c.IssuedDate,
        Marks = c.Marks,
        Url = c.Url,
        MimeType = c.MimeType,
        Width = c.Width,
        Height = c.Height,
        AltText = c.AltText,
        Featured = c.Featured,
        SortOrder = c.SortOrder
    };

    private static readonly Expression<Func<Certificate, AdminCertificateResponse>> AdminProjection = c => new AdminCertificateResponse
    {
        Id = c.Id,
        Name = c.Name,
        IssuedBy = c.IssuedBy,
        Category = c.Category,
        IssuedDate = c.IssuedDate,
        Marks = c.Marks,
        ObjectKey = c.ObjectKey,
        Url = c.Url,
        MimeType = c.MimeType,
        Width = c.Width,
        Height = c.Height,
        AltText = c.AltText,
        IsPublished = c.IsPublished,
        Featured = c.Featured,
        SortOrder = c.SortOrder,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };
}
