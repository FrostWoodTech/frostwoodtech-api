using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using FrostWoodTech.API.Auth;
using FrostWoodTech.API.Common;
using FrostWoodTech.API.Data;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;
using FrostWoodTech.API.Email;
using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Services;

public class ContactService : IContactService
{
    /// <summary>
    /// Rows are kept forever for the admin inbox, so the submission rate limit counts them
    /// directly instead of a second attempts table — same reasoning as <see cref="ReviewService"/>.
    /// </summary>
    private static readonly TimeSpan SubmissionWindow = TimeSpan.FromHours(24);

    /// <summary>
    /// Higher than reviews' 3 — a genuine prospect may legitimately follow up more than once.
    /// </summary>
    private const int MaxSubmissionsPerIpPerWindow = 5;

    private readonly FrostWoodTechDbContext _db;
    private readonly IEmailService _email;
    private readonly EmailOptions _emailOptions;
    private readonly ContactOptions _contactOptions;
    private readonly SuperAdminOptions _superAdminOptions;
    private readonly CurrentUser _currentUser;
    private readonly ILogger<ContactService> _logger;

    public ContactService(
        FrostWoodTechDbContext db,
        IEmailService email,
        IOptions<EmailOptions> emailOptions,
        IOptions<ContactOptions> contactOptions,
        IOptions<SuperAdminOptions> superAdminOptions,
        CurrentUser currentUser,
        ILogger<ContactService> logger)
    {
        _db = db;
        _email = email;
        _emailOptions = emailOptions.Value;
        _contactOptions = contactOptions.Value;
        _superAdminOptions = superAdminOptions.Value;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ServiceResult<ContactSubmissionResponse>> SubmitAsync(
        CreateContactSubmissionRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var name = Blank(request.Name);
        var email = Blank(request.Email);
        var message = Blank(request.Message);
        var isHoneypot = !string.IsNullOrEmpty(request.Website);

        var validationError = Validate(name, email, message, request.Site);
        if (validationError is not null)
        {
            return ServiceResult<ContactSubmissionResponse>.Validation(validationError);
        }

        if (!isHoneypot && ipAddress is not null)
        {
            var since = DateTimeOffset.UtcNow - SubmissionWindow;
            var recentCount = await _db.ContactSubmissions
                .CountAsync(c => c.SubmitterIp == ipAddress && c.CreatedAt >= since, cancellationToken);

            if (recentCount >= MaxSubmissionsPerIpPerWindow)
            {
                return ServiceResult<ContactSubmissionResponse>.Forbidden(
                    "too_many_submissions",
                    "Too many enquiries submitted from this address. Try again later.");
            }
        }

        Guid? serviceId = null;
        if (request.ServiceId is { } requestedServiceId)
        {
            var serviceExists = await _db.Services
                .AnyAsync(s => s.Id == requestedServiceId, cancellationToken);

            if (!serviceExists)
            {
                return ServiceResult<ContactSubmissionResponse>.Validation("serviceId does not match a known service.");
            }

            serviceId = requestedServiceId;
        }

        var submission = new ContactSubmission
        {
            Id = Guid.NewGuid(),
            Name = name!,
            Email = email!,
            Phone = Blank(request.Phone),
            Company = Blank(request.Company),
            Subject = Blank(request.Subject),
            Message = message!,
            ServiceId = serviceId,
            BudgetRange = request.BudgetRange,
            Site = request.Site!.Value,
            // A filled honeypot is filed as spam and still answered normally — telling a bot it
            // was caught just teaches it to stop filling the field.
            Status = isHoneypot ? ContactSubmissionStatus.Spam : ContactSubmissionStatus.New,
            SubmitterIp = ipAddress
        };

        _db.ContactSubmissions.Add(submission);
        await _db.SaveChangesAsync(cancellationToken);

        if (!isHoneypot)
        {
            await NotifyAdminsAsync(submission, cancellationToken);
        }

        return ServiceResult<ContactSubmissionResponse>.Success(
            new ContactSubmissionResponse { Id = submission.Id });
    }

    public async Task<PagedResult<AdminContactSubmissionResponse>> GetAdminSubmissionsAsync(
        ContactSubmissionStatus? status,
        Site? site,
        Guid? serviceId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.ContactSubmissions.AsNoTracking();

        if (status is not null)
        {
            query = query.Where(c => c.Status == status);
        }

        if (site is not null)
        {
            query = query.Where(c => c.Site == site);
        }

        if (serviceId is not null)
        {
            query = query.Where(c => c.ServiceId == serviceId);
        }

        if (search is not null)
        {
            query = query.Where(c =>
                EF.Functions.ILike(c.Name, $"%{search}%")
                || EF.Functions.ILike(c.Email, $"%{search}%")
                || EF.Functions.ILike(c.Message, $"%{search}%"));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(AdminProjection)
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminContactSubmissionResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<ServiceResult<AdminContactSubmissionResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var submission = await _db.ContactSubmissions
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(AdminProjection)
            .FirstOrDefaultAsync(cancellationToken);

        return submission is null ? NotFound(id) : ServiceResult<AdminContactSubmissionResponse>.Success(submission);
    }

    public async Task<ServiceResult<AdminContactSubmissionResponse>> UpdateAsync(
        Guid id,
        UpdateContactSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        var submission = await _db.ContactSubmissions.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (submission is null)
        {
            return NotFound(id);
        }

        submission.Status = request.Status;
        submission.AdminNotes = Blank(request.AdminNotes);

        if (request.Status == ContactSubmissionStatus.Replied)
        {
            submission.RepliedAt = DateTimeOffset.UtcNow;
            submission.RepliedBy = _currentUser.UserId;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var updated = await _db.ContactSubmissions
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(AdminProjection)
            .FirstAsync(cancellationToken);

        return ServiceResult<AdminContactSubmissionResponse>.Success(updated);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var submission = await _db.ContactSubmissions.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (submission is null)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No contact submission with id {id}.");
        }

        submission.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    /// <summary>
    /// Best-effort: a bounced notification must never fail the visitor's submission, so any
    /// failure is logged and swallowed.
    /// </summary>
    private async Task NotifyAdminsAsync(ContactSubmission submission, CancellationToken cancellationToken)
    {
        var to = Blank(_contactOptions.NotifyAddress) ?? Blank(_superAdminOptions.Email);
        if (to is null)
        {
            _logger.LogWarning(
                "New contact submission {Id} not emailed — no Contact:NotifyAddress or SuperAdmin:Email is configured.",
                submission.Id);
            return;
        }

        var adminLink = $"{_emailOptions.BaseUrl.TrimEnd('/')}/contact/{submission.Id}";

        var sent = await _email.SendAsync(new EmailMessage
        {
            To = to,
            Subject = $"New enquiry from {submission.Name}",
            HtmlBody =
                $"<p>New contact form submission from <strong>{submission.Name}</strong> ({submission.Email}).</p>" +
                $"<p>{System.Net.WebUtility.HtmlEncode(submission.Message)}</p>" +
                $"<p><a href=\"{adminLink}\">View in the admin dashboard</a></p>",
            TextBody =
                $"New contact form submission from {submission.Name} ({submission.Email}).\n\n" +
                $"{submission.Message}\n\n" +
                $"View in the admin dashboard: {adminLink}",
            ReplyTo = submission.Email
        }, cancellationToken);

        if (!sent.IsSuccess)
        {
            _logger.LogWarning(
                "Contact notification for submission {Id} could not be sent: {Code}",
                submission.Id,
                sent.Error!.Code);
        }
    }

    /// <summary>Null when the submission is valid, otherwise the message to hand back.</summary>
    private static string? Validate(string? name, string? email, string? message, Site? site)
    {
        if (name is null)
        {
            return "Name is required.";
        }

        if (email is null || !email.Contains('@', StringComparison.Ordinal))
        {
            return "A valid email is required.";
        }

        if (message is null)
        {
            return "Message is required.";
        }

        if (site is null)
        {
            return "site is required.";
        }

        return null;
    }

    private static ServiceResult<AdminContactSubmissionResponse> NotFound(Guid id) =>
        ServiceResult<AdminContactSubmissionResponse>.NotFound("not_found", $"No contact submission with id {id}.");

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static readonly Expression<Func<ContactSubmission, AdminContactSubmissionResponse>> AdminProjection =
        c => new AdminContactSubmissionResponse
        {
            Id = c.Id,
            Name = c.Name,
            Email = c.Email,
            Phone = c.Phone,
            Company = c.Company,
            Subject = c.Subject,
            Message = c.Message,
            ServiceId = c.ServiceId,
            ServiceName = c.Service != null ? c.Service.Name : null,
            BudgetRange = c.BudgetRange,
            Site = c.Site,
            Status = c.Status,
            AdminNotes = c.AdminNotes,
            RepliedAt = c.RepliedAt,
            RepliedBy = c.RepliedBy,
            SubmitterIp = c.SubmitterIp,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        };
}
