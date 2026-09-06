using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

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

    public CertificateService(FrostWoodTechDbContext db)
    {
        _db = db;
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

        // Sort order is never taken from the client — it's only ever changed via ReorderAsync,
        // so a new certificate is appended to the end of the (single, global) order.
        var nextSortOrder = await _db.Certificates.MaxAsync(c => (int?)c.SortOrder, cancellationToken) + 1 ?? 0;

        var certificate = new Certificate
        {
            Id = Guid.NewGuid(),
            Name = request.Name!.Trim(),
            IssuedBy = request.IssuedBy!.Trim(),
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

        // Sort order is untouched here — it only changes via ReorderAsync.
        certificate.Name = request.Name!.Trim();
        certificate.IssuedBy = request.IssuedBy!.Trim();
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
        await _db.SaveChangesAsync(cancellationToken);

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

    /// <summary>Null when the certificate is valid, otherwise the message to hand back.</summary>
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

        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return "Url is required.";
        }

        if (string.IsNullOrWhiteSpace(request.MimeType))
        {
            return "Mime type is required.";
        }

        if (string.IsNullOrWhiteSpace(request.AltText))
        {
            return "Alt text is required.";
        }

        return null;
    }

    private static ServiceResult<AdminCertificateResponse> NotFound(Guid id) =>
        ServiceResult<AdminCertificateResponse>.NotFound("not_found", $"No certificate with id {id}.");

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Projected inside the query so the SQL stays narrow.</summary>
    private static readonly Expression<Func<Certificate, CertificateResponse>> PublicProjection = c => new CertificateResponse
    {
        Id = c.Id,
        Name = c.Name,
        IssuedBy = c.IssuedBy,
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
