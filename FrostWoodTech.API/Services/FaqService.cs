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

public class FaqService : IFaqService
{
    private readonly FrostWoodTechDbContext _db;

    public FaqService(FrostWoodTechDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<FaqResponse>> GetPublicFaqsAsync(
        Site site,
        CancellationToken cancellationToken)
    {
        // Public list is global FAQs only; service-scoped FAQs render on their service page.
        var query = ForSite(_db.Faqs.AsNoTracking().Where(f => f.IsPublished && f.ServiceId == null), site);

        // Not paged: the FAQ page renders the whole list.
        return await query
            .OrderBy(f => f.SortOrder)
            .Select(PublicProjection)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<AdminFaqResponse>> GetAdminFaqsAsync(
        Site? site,
        bool? isPublished,
        string? search,
        Guid? serviceId,
        bool globalOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.Faqs.AsNoTracking();

        if (site is not null)
        {
            query = ForSite(query, site.Value);
        }

        // serviceId and globalOnly are separate filters; a missing serviceId never means "global".
        if (serviceId is not null)
        {
            query = query.Where(f => f.ServiceId == serviceId);
        }
        else if (globalOnly)
        {
            query = query.Where(f => f.ServiceId == null);
        }

        if (isPublished is not null)
        {
            query = query.Where(f => f.IsPublished == isPublished);
        }

        if (search is not null)
        {
            query = query.Where(f =>
                EF.Functions.ILike(f.Question, $"%{search}%")
                || EF.Functions.ILike(f.Answer, $"%{search}%"));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(f => f.SortOrder)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(AdminProjection)
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminFaqResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<ServiceResult<AdminFaqResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var faq = await _db.Faqs
            .AsNoTracking()
            .Where(f => f.Id == id)
            .Select(AdminProjection)
            .FirstOrDefaultAsync(cancellationToken);

        return faq is null ? NotFound(id) : ServiceResult<AdminFaqResponse>.Success(faq);
    }

    public async Task<ServiceResult<AdminFaqResponse>> CreateAsync(
        CreateFaqRequest request,
        CancellationToken cancellationToken)
    {
        var question = Blank(request.Question);
        var answer = Blank(request.Answer);

        var validationError = Validate(question, answer, request);
        if (validationError is not null)
        {
            return ServiceResult<AdminFaqResponse>.Validation(validationError);
        }

        if (request.ServiceId is not null && !await _db.Services.AnyAsync(s => s.Id == request.ServiceId, cancellationToken))
        {
            return ServiceResult<AdminFaqResponse>.Validation($"Unknown service id: {request.ServiceId}.");
        }

        // New FAQs go to the end of their own scope's order.
        var nextSortOrder = await _db.Faqs
            .Where(f => f.ServiceId == request.ServiceId)
            .MaxAsync(f => (int?)f.SortOrder, cancellationToken) + 1 ?? 0;

        var faq = new Faq
        {
            Id = Guid.NewGuid(),
            ServiceId = request.ServiceId,
            Question = question!,
            Answer = answer!,
            SortOrder = nextSortOrder,
            IsPublished = request.IsPublished,
            ShowOnAgency = request.ShowOnAgency,
            ShowOnPersonal = request.ShowOnPersonal
        };

        _db.Faqs.Add(faq);
        await _db.SaveChangesAsync(cancellationToken);

        // Re-read: the Service navigation isn't loaded for a link added by id.
        return await GetByIdAsync(faq.Id, cancellationToken);
    }

    public async Task<ServiceResult<AdminFaqResponse>> UpdateAsync(
        Guid id,
        UpdateFaqRequest request,
        CancellationToken cancellationToken)
    {
        var faq = await _db.Faqs.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (faq is null)
        {
            return NotFound(id);
        }

        var question = Blank(request.Question);
        var answer = Blank(request.Answer);

        var validationError = Validate(question, answer, request);
        if (validationError is not null)
        {
            return ServiceResult<AdminFaqResponse>.Validation(validationError);
        }

        if (request.ServiceId is not null && !await _db.Services.AnyAsync(s => s.Id == request.ServiceId, cancellationToken))
        {
            return ServiceResult<AdminFaqResponse>.Validation($"Unknown service id: {request.ServiceId}.");
        }

        // Re-scoping keeps sort_order; the reorder screen for the new scope fixes it.
        faq.ServiceId = request.ServiceId;
        faq.Question = question!;
        faq.Answer = answer!;
        faq.IsPublished = request.IsPublished;
        faq.ShowOnAgency = request.ShowOnAgency;
        faq.ShowOnPersonal = request.ShowOnPersonal;

        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(faq.Id, cancellationToken);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var faq = await _db.Faqs.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (faq is null)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No FAQ with id {id}.");
        }

        faq.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> ReorderAsync(
        FaqReorderRequest request,
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
            return ServiceResult<bool>.Validation("The same FAQ id appears more than once.");
        }

        var faqs = await _db.Faqs
            .Where(f => ids.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, cancellationToken);

        var missing = ids.Where(id => !faqs.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No FAQ with id {string.Join(", ", missing)}.");
        }

        foreach (var item in items)
        {
            faqs[item.Id].SortOrder = item.SortOrder;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    private static IQueryable<Faq> ForSite(IQueryable<Faq> query, Site site) =>
        site == Site.Agency
            ? query.Where(f => f.ShowOnAgency)
            : query.Where(f => f.ShowOnPersonal);

    private static string? Validate(string? question, string? answer, CreateFaqRequest request)
    {
        if (question is null)
        {
            return "Question is required.";
        }

        if (answer is null)
        {
            return "Answer is required.";
        }

        return null;
    }

    private static ServiceResult<AdminFaqResponse> NotFound(Guid id) =>
        ServiceResult<AdminFaqResponse>.NotFound("not_found", $"No FAQ with id {id}.");

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static readonly Expression<Func<Faq, FaqResponse>> PublicProjection = f => new FaqResponse
    {
        Id = f.Id,
        Question = f.Question,
        Answer = f.Answer,
        SortOrder = f.SortOrder
    };

    private static readonly Expression<Func<Faq, AdminFaqResponse>> AdminProjection = f => new AdminFaqResponse
    {
        Id = f.Id,
        ServiceId = f.ServiceId,
        ServiceName = f.Service == null ? null : f.Service.Name,
        Question = f.Question,
        Answer = f.Answer,
        SortOrder = f.SortOrder,
        IsPublished = f.IsPublished,
        ShowOnAgency = f.ShowOnAgency,
        ShowOnPersonal = f.ShowOnPersonal,
        CreatedAt = f.CreatedAt,
        UpdatedAt = f.UpdatedAt
    };
}
