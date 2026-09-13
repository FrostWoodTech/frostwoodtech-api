using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using FrostWoodTech.API.Auth;
using FrostWoodTech.API.Common;
using FrostWoodTech.API.Data;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Email;
using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Services;

public class UserService : IUserService
{
    private const int MinimumPasswordLength = 8;

    private const string SuperAdminOnly = "Only the super admin can manage users.";

    private static readonly TimeSpan VerificationTokenLifetime = TimeSpan.FromHours(24);

    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromHours(1);

    private static readonly TimeSpan ResendWindow = TimeSpan.FromHours(1);

    private const int MaxResendsPerEmailPerWindow = 3;

    private static readonly Expression<Func<User, AdminUserResponse>> AdminProjection = user =>
        new AdminUserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            Status = user.Status,
            LastLoginAt = user.LastLoginAt,
            ApprovedAt = user.ApprovedAt,
            RejectionReason = user.RejectionReason,
            EmailVerifiedAt = user.EmailVerifiedAt,
            CreatedAt = user.CreatedAt
        };

    private static readonly Func<User, AdminUserResponse> ToResponse = AdminProjection.Compile();

    private readonly FrostWoodTechDbContext _db;
    private readonly IJwtTokenService _tokens;
    private readonly IGoogleTokenValidator _google;
    private readonly ILoginRateLimiter _rateLimiter;
    private readonly CurrentUser _currentUser;
    private readonly JwtOptions _jwtOptions;
    private readonly IEmailService _email;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<UserService> _logger;

    public UserService(
        FrostWoodTechDbContext db,
        IJwtTokenService tokens,
        IGoogleTokenValidator google,
        ILoginRateLimiter rateLimiter,
        CurrentUser currentUser,
        IOptions<JwtOptions> jwtOptions,
        IEmailService email,
        IOptions<EmailOptions> emailOptions,
        ILogger<UserService> logger)
    {
        _db = db;
        _tokens = tokens;
        _google = google;
        _rateLimiter = rateLimiter;
        _currentUser = currentUser;
        _jwtOptions = jwtOptions.Value;
        _email = email;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public async Task<ServiceResult<AdminUserResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var firstName = Blank(request.FirstName);
        var lastName = Blank(request.LastName);
        var email = NormaliseEmail(request.Email);

        var validationError = ValidateRegistration(firstName, lastName, email, request);
        if (validationError is not null)
        {
            return ServiceResult<AdminUserResponse>.Validation(validationError);
        }

        // A soft-deleted account still owns its email.
        var emailTaken = await _db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == email, cancellationToken);

        if (emailTaken)
        {
            return ServiceResult<AdminUserResponse>.Conflict(
                "email_taken", "That email address is already registered.");
        }

        var user = new User
        {
            Email = email!,
            FirstName = firstName!,
            LastName = lastName!,
            PasswordHash = PasswordHasher.Hash(request.Password!),
            Role = UserRole.Admin,
            Status = UserStatus.EmailVerificationRequired
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        await IssueVerificationEmailAsync(user, cancellationToken);

        return ServiceResult<AdminUserResponse>.Success(ToResponse(user));
    }

    public async Task<ServiceResult<AdminUserResponse>> VerifyEmailAsync(
        VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        if (Blank(request.Token) is not { } rawToken)
        {
            return ServiceResult<AdminUserResponse>.Validation("A token is required.");
        }

        var hash = EmailVerificationTokenGenerator.Hash(rawToken);

        var token = await _db.EmailVerificationTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (token is null)
        {
            return ServiceResult<AdminUserResponse>.NotFound(
                "invalid_verification_token", "This verification link is not valid.");
        }

        if (token.UsedAt is not null || token.User.Status != UserStatus.EmailVerificationRequired)
        {
            return ServiceResult<AdminUserResponse>.Conflict(
                "verification_token_already_used",
                "This link has already been used. If you haven't verified yet, request a new one.");
        }

        if (token.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return ServiceResult<AdminUserResponse>.Conflict(
                "verification_token_expired",
                "This link has expired. Request a new verification email.");
        }

        var user = token.User;

        user.Status = UserStatus.Pending;
        user.EmailVerifiedAt = DateTimeOffset.UtcNow;
        token.UsedAt = DateTimeOffset.UtcNow;

        await _db.EmailVerificationTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null && t.Id != token.Id)
            .ExecuteUpdateAsync(t => t.SetProperty(x => x.UsedAt, DateTimeOffset.UtcNow), cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<AdminUserResponse>.Success(ToResponse(user));
    }

    public async Task<ServiceResult<ResendVerificationResponse>> ResendVerificationAsync(
        ResendVerificationRequest request,
        CancellationToken cancellationToken)
    {
        // Same response on every branch: no account enumeration.
        var response = new ResendVerificationResponse();

        var email = NormaliseEmail(request.Email);
        if (email is null)
        {
            return ServiceResult<ResendVerificationResponse>.Success(response);
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null || user.Status != UserStatus.EmailVerificationRequired)
        {
            return ServiceResult<ResendVerificationResponse>.Success(response);
        }

        var since = DateTimeOffset.UtcNow - ResendWindow;
        var recentCount = await _db.EmailVerificationTokens
            .CountAsync(t => t.UserId == user.Id && t.CreatedAt >= since, cancellationToken);

        if (recentCount >= MaxResendsPerEmailPerWindow)
        {
            return ServiceResult<ResendVerificationResponse>.Success(response);
        }

        await _db.EmailVerificationTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null)
            .ExecuteUpdateAsync(t => t.SetProperty(x => x.UsedAt, DateTimeOffset.UtcNow), cancellationToken);

        await IssueVerificationEmailAsync(user, cancellationToken);

        return ServiceResult<ResendVerificationResponse>.Success(response);
    }

    // A failed send doesn't fail the caller; the user can resend.
    private async Task IssueVerificationEmailAsync(User user, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var rawToken = EmailVerificationTokenGenerator.Create();

        _db.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            UserId = user.Id,
            TokenHash = EmailVerificationTokenGenerator.Hash(rawToken),
            ExpiresAt = now.Add(VerificationTokenLifetime),
            CreatedAt = now
        });

        await _db.SaveChangesAsync(cancellationToken);

        var link = $"{_emailOptions.BaseUrl.TrimEnd('/')}/verify-email?token={rawToken}";

        var sent = await _email.SendAsync(new EmailMessage
        {
            To = user.Email,
            Subject = "Confirm your FrostWoodTech admin account",
            HtmlBody =
                $"<p>Hi {user.FirstName},</p>" +
                $"<p>Confirm your email address to continue setting up your FrostWoodTech admin account:</p>" +
                $"<p><a href=\"{link}\">{link}</a></p>" +
                "<p>This link expires in 24 hours. If you didn't request this account, ignore this email.</p>",
            TextBody =
                $"Hi {user.FirstName},\n\n" +
                "Confirm your email address to continue setting up your FrostWoodTech admin account:\n" +
                $"{link}\n\n" +
                "This link expires in 24 hours. If you didn't request this account, ignore this email."
        }, cancellationToken);

        if (!sent.IsSuccess)
        {
            _logger.LogWarning(
                "Verification email to {Email} could not be sent: {Code}", user.Email, sent.Error!.Code);
        }
    }

    public async Task<ServiceResult<ForgotPasswordResponse>> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        // Same response on every branch: no account enumeration.
        var response = new ForgotPasswordResponse();
        var success = ServiceResult<ForgotPasswordResponse>.Success(response);

        var email = NormaliseEmail(request.Email);
        if (email is null)
        {
            return success;
        }

        if (await _rateLimiter.IsBlockedAsync(
                email, ipAddress, AuthAttemptAction.PasswordReset, cancellationToken))
        {
            _logger.LogInformation("Password reset for {Email} refused: rate limited.", email);

            return success;
        }

        await _rateLimiter.RecordAttemptAsync(
            email, ipAddress, AuthAttemptAction.PasswordReset, cancellationToken);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            _logger.LogInformation("Password reset requested for {Email}, which has no account.", email);

            return success;
        }

        // Only pending/approved get a link; rejected and disabled must not get a new credential.
        if (user.Status is not (UserStatus.Pending or UserStatus.Approved))
        {
            _logger.LogInformation(
                "Password reset for {Email} skipped: status is {Status}.", email, user.Status);

            return success;
        }

        // Invalidate first so only the newest link works.
        await _db.PasswordTokens
            .Where(t => t.UserId == user.Id
                && t.Purpose == PasswordTokenPurpose.Reset
                && t.UsedAt == null)
            .ExecuteUpdateAsync(t => t.SetProperty(x => x.UsedAt, DateTimeOffset.UtcNow), cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var rawToken = EmailVerificationTokenGenerator.Create();

        _db.PasswordTokens.Add(new PasswordToken
        {
            UserId = user.Id,
            TokenHash = EmailVerificationTokenGenerator.Hash(rawToken),
            Purpose = PasswordTokenPurpose.Reset,
            ExpiresAt = now.Add(ResetTokenLifetime),
            CreatedAt = now
        });

        await _db.SaveChangesAsync(cancellationToken);

        var link = $"{_emailOptions.BaseUrl.TrimEnd('/')}/set-password?token={rawToken}";

        var sent = await _email.SendAsync(new EmailMessage
        {
            To = user.Email,
            Subject = "Reset your FrostWoodTech password",
            HtmlBody =
                $"<p>Hi {user.FirstName},</p>" +
                "<p>Somebody asked to reset the password for your FrostWoodTech admin account. Choose a new one here:</p>" +
                $"<p><a href=\"{link}\">{link}</a></p>" +
                "<p>This link expires in 1 hour and can only be used once. Setting a new password signs you out everywhere else.</p>" +
                "<p>If this wasn't you, ignore this email — your password has not changed.</p>",
            TextBody =
                $"Hi {user.FirstName},\n\n" +
                "Somebody asked to reset the password for your FrostWoodTech admin account. Choose a new one here:\n" +
                $"{link}\n\n" +
                "This link expires in 1 hour and can only be used once. Setting a new password signs you out everywhere else.\n\n" +
                "If this wasn't you, ignore this email — your password has not changed."
        }, cancellationToken);

        if (!sent.IsSuccess)
        {
            _logger.LogWarning(
                "Password reset email to {Email} could not be sent: {Code}", user.Email, sent.Error!.Code);
        }

        return success;
    }

    public async Task<ServiceResult<AdminUserResponse>> SetPasswordAsync(
        SetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (Blank(request.Token) is not { } rawToken)
        {
            return ServiceResult<AdminUserResponse>.Validation("A token is required.");
        }

        var validationError = ValidatePassword(request.Password, request.ConfirmPassword, "Password");
        if (validationError is not null)
        {
            return ServiceResult<AdminUserResponse>.Validation(validationError);
        }

        var hash = EmailVerificationTokenGenerator.Hash(rawToken);

        var token = await _db.PasswordTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        // Distinct codes are safe: reaching them already requires a token.
        if (token is null)
        {
            _logger.LogInformation("Password link rejected: no token matches the presented value.");

            return ServiceResult<AdminUserResponse>.NotFound(
                "invalid_setup_token", "This password link is not valid.");
        }

        if (token.UsedAt is not null)
        {
            _logger.LogInformation(
                "{Purpose} link for {UserId} rejected: already used at {UsedAt}.",
                token.Purpose, token.UserId, token.UsedAt);

            return ServiceResult<AdminUserResponse>.Conflict(
                "setup_token_already_used", "This link has already been used.");
        }

        if (token.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _logger.LogInformation(
                "{Purpose} link for {UserId} rejected: expired at {ExpiresAt}.",
                token.Purpose, token.UserId, token.ExpiresAt);

            return ServiceResult<AdminUserResponse>.Conflict(
                "setup_token_expired", "This link has expired.");
        }

        var user = token.User;

        user.PasswordHash = PasswordHasher.Hash(request.Password!);
        token.UsedAt = DateTimeOffset.UtcNow;

        await _db.PasswordTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null && t.Id != token.Id)
            .ExecuteUpdateAsync(t => t.SetProperty(x => x.UsedAt, DateTimeOffset.UtcNow), cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        // A new password ends every existing session.
        await RevokeAllForUserAsync(user.Id, cancellationToken);

        _logger.LogInformation(
            "{Purpose} link redeemed for {UserId}. All refresh tokens revoked.", token.Purpose, user.Id);

        return ServiceResult<AdminUserResponse>.Success(ToResponse(user));
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var email = NormaliseEmail(request.Email);
        var password = request.Password;

        if (email is null || string.IsNullOrEmpty(password))
        {
            return ServiceResult<AuthResponse>.Validation("Email and password are required.");
        }

        // Before the password check: a blocked caller shouldn't cost an Argon2 hash.
        if (await _rateLimiter.IsBlockedAsync(email, ipAddress, AuthAttemptAction.Login, cancellationToken))
        {
            return ServiceResult<AuthResponse>.Unauthorized(
                "too_many_attempts",
                "Too many failed sign-in attempts. Wait a few minutes and try again.");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            // Same work and answer as a wrong password: no account enumeration.
            PasswordHasher.BurnVerifyTime(password);
            await _rateLimiter.RecordAttemptAsync(email, ipAddress, AuthAttemptAction.Login, cancellationToken);

            return InvalidCredentials();
        }

        // No hash (Google-only or unredeemed super admin): fail like a wrong password.
        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            PasswordHasher.BurnVerifyTime(password);
            await _rateLimiter.RecordAttemptAsync(email, ipAddress, AuthAttemptAction.Login, cancellationToken);

            return InvalidCredentials();
        }

        if (!PasswordHasher.Verify(user.PasswordHash, password))
        {
            await _rateLimiter.RecordAttemptAsync(email, ipAddress, AuthAttemptAction.Login, cancellationToken);

            return InvalidCredentials();
        }

        // After the password check, so account state can't be probed anonymously.
        if (user.Status != UserStatus.Approved)
        {
            return NotApproved(user);
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;

        await _rateLimiter.ClearAsync(email, AuthAttemptAction.Login, cancellationToken);

        var (response, _) = await IssueTokensAsync(user, cancellationToken);

        return ServiceResult<AuthResponse>.Success(response);
    }

    public async Task<ServiceResult<AuthResponse>> GoogleSignInAsync(
        GoogleSignInRequest request,
        CancellationToken cancellationToken)
    {
        if (Blank(request.IdToken) is not { } idToken)
        {
            return ServiceResult<AuthResponse>.Validation("An idToken is required.");
        }

        var validated = await _google.ValidateAsync(idToken, cancellationToken);
        if (!validated.IsSuccess)
        {
            return ServiceResult<AuthResponse>.Failure(validated.Error!);
        }

        var identity = validated.Value!;

        // Never match an unverified Google email, or anyone could claim a CMS account.
        if (!identity.EmailVerified)
        {
            return ServiceResult<AuthResponse>.Unauthorized(
                "google_email_unverified", "This Google account's email address is not verified.");
        }

        // Subject first (stable). Include deleted users so they're refused, not re-created.
        var user = await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.GoogleSubjectId == identity.Subject || u.Email == identity.Email,
                cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Email = identity.Email,
                FirstName = identity.FirstName ?? identity.Email,
                LastName = identity.LastName ?? string.Empty,
                GoogleSubjectId = identity.Subject,
                AvatarUrl = identity.AvatarUrl,
                PasswordHash = null,
                Role = UserRole.Admin,
                Status = UserStatus.Pending
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);

            return NotApproved(user);
        }

        if (user.IsDeleted)
        {
            return ServiceResult<AuthResponse>.Forbidden(
                "account_disabled", "This account has been disabled. Contact the super admin.");
        }

        // Links Google to an existing password account; the password keeps working.
        user.GoogleSubjectId ??= identity.Subject;
        user.AvatarUrl = identity.AvatarUrl ?? user.AvatarUrl;

        if (user.Status != UserStatus.Approved)
        {
            await _db.SaveChangesAsync(cancellationToken);

            return NotApproved(user);
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;

        var (response, _) = await IssueTokensAsync(user, cancellationToken);

        return ServiceResult<AuthResponse>.Success(response);
    }

    public async Task<ServiceResult<AuthResponse>> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (Blank(request.RefreshToken) is not { } presented)
        {
            return ServiceResult<AuthResponse>.Validation("A refresh token is required.");
        }

        var hash = RefreshTokenGenerator.Hash(presented);

        // Include deleted owners so the account checks below still run.
        var stored = await _db.RefreshTokens
            .IgnoreQueryFilters()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (stored is null)
        {
            return InvalidRefreshToken();
        }

        if (stored.RevokedAt is not null)
        {
            // A reused revoked token means theft: revoke the whole family.
            await RevokeAllForUserAsync(stored.UserId, cancellationToken);

            return ServiceResult<AuthResponse>.Unauthorized(
                "refresh_token_reused",
                "This refresh token was already used. All sessions have been signed out.");
        }

        if (stored.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return ServiceResult<AuthResponse>.Unauthorized(
                "refresh_token_expired", "This refresh token has expired. Sign in again.");
        }

        // Blocks renewal for revoked users, whatever their JWT still says.
        if (stored.User.Status != UserStatus.Approved || stored.User.IsDeleted)
        {
            await RevokeAllForUserAsync(stored.UserId, cancellationToken);

            return NotApproved(stored.User);
        }

        var (issued, replacement) = await IssueTokensAsync(stored.User, cancellationToken);

        stored.RevokedAt = DateTimeOffset.UtcNow;
        stored.ReplacedByTokenId = replacement.Id;

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<AuthResponse>.Success(issued);
    }

    public async Task<ServiceResult<bool>> LogoutAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        // No error for unknown tokens: logout can't be used to probe tokens.
        if (Blank(request.RefreshToken) is { } presented)
        {
            var hash = RefreshTokenGenerator.Hash(presented);

            await _db.RefreshTokens
                .Where(t => t.TokenHash == hash && t.RevokedAt == null)
                .ExecuteUpdateAsync(
                    t => t.SetProperty(x => x.RevokedAt, DateTimeOffset.UtcNow), cancellationToken);
        }

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<AdminUserResponse>> GetMeAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return ServiceResult<AdminUserResponse>.Unauthorized("unauthenticated", "No signed-in user.");
        }

        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(AdminProjection)
            .FirstOrDefaultAsync(cancellationToken);

        return user is null
            ? ServiceResult<AdminUserResponse>.Unauthorized("unauthenticated", "This account no longer exists.")
            : ServiceResult<AdminUserResponse>.Success(user);
    }

    public async Task<ServiceResult<bool>> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return ServiceResult<bool>.Unauthorized("unauthenticated", "No signed-in user.");
        }

        var validationError = ValidatePassword(request.NewPassword, request.ConfirmNewPassword, "New password");
        if (validationError is not null)
        {
            return ServiceResult<bool>.Validation(validationError);
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return ServiceResult<bool>.Unauthorized("unauthenticated", "This account no longer exists.");
        }

        if (string.IsNullOrEmpty(user.PasswordHash)
            || string.IsNullOrEmpty(request.CurrentPassword)
            || !PasswordHasher.Verify(user.PasswordHash, request.CurrentPassword))
        {
            return ServiceResult<bool>.Unauthorized("invalid_credentials", "The current password is incorrect.");
        }

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword!);
        await _db.SaveChangesAsync(cancellationToken);

        // Signs out everywhere, including the current session.
        await RevokeAllForUserAsync(userId, cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<PagedResult<AdminUserResponse>>> GetAllAsync(
        string? search,
        UserStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (RequireSuperAdmin<PagedResult<AdminUserResponse>>() is { } denied)
        {
            return denied;
        }

        var query = _db.Users.AsNoTracking();

        if (status is { } wanted)
        {
            query = query.Where(u => u.Status == wanted);
        }

        if (Blank(search) is { } term)
        {
            var pattern = $"%{term}%";
            query = query.Where(u =>
                EF.Functions.ILike(u.Email, pattern)
                || EF.Functions.ILike(u.FirstName, pattern)
                || EF.Functions.ILike(u.LastName, pattern));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(AdminProjection)
            .ToListAsync(cancellationToken);

        return ServiceResult<PagedResult<AdminUserResponse>>.Success(new PagedResult<AdminUserResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        });
    }

    public async Task<ServiceResult<AdminUserResponse>> ApproveAsync(Guid id, CancellationToken cancellationToken)
    {
        var loaded = await LoadManageableUserAsync(id, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return ServiceResult<AdminUserResponse>.Failure(loaded.Error!);
        }

        var user = loaded.Value!;

        if (user.Status != UserStatus.Pending)
        {
            return ServiceResult<AdminUserResponse>.Conflict(
                "user_not_pending", "Only accounts pending approval can be approved.");
        }

        user.Status = UserStatus.Approved;
        user.ApprovedBy = _currentUser.UserId;
        user.ApprovedAt = DateTimeOffset.UtcNow;
        user.RejectionReason = null;

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<AdminUserResponse>.Success(ToResponse(user));
    }

    public async Task<ServiceResult<AdminUserResponse>> RejectAsync(
        Guid id,
        RejectUserRequest request,
        CancellationToken cancellationToken)
    {
        if (Blank(request.Reason) is not { } reason)
        {
            return ServiceResult<AdminUserResponse>.Validation("A rejection reason is required.");
        }

        var loaded = await LoadManageableUserAsync(id, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return ServiceResult<AdminUserResponse>.Failure(loaded.Error!);
        }

        var user = loaded.Value!;

        user.Status = UserStatus.Rejected;
        user.RejectionReason = reason;
        user.ApprovedBy = null;
        user.ApprovedAt = null;

        await _db.SaveChangesAsync(cancellationToken);
        await RevokeAllForUserAsync(user.Id, cancellationToken);

        return ServiceResult<AdminUserResponse>.Success(ToResponse(user));
    }

    public async Task<ServiceResult<AdminUserResponse>> DisableAsync(Guid id, CancellationToken cancellationToken)
    {
        var loaded = await LoadManageableUserAsync(id, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return ServiceResult<AdminUserResponse>.Failure(loaded.Error!);
        }

        var user = loaded.Value!;

        user.Status = UserStatus.Disabled;

        await _db.SaveChangesAsync(cancellationToken);
        await RevokeAllForUserAsync(user.Id, cancellationToken);

        return ServiceResult<AdminUserResponse>.Success(ToResponse(user));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var loaded = await LoadManageableUserAsync(id, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return ServiceResult<bool>.Failure(loaded.Error!);
        }

        loaded.Value!.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);
        await RevokeAllForUserAsync(loaded.Value.Id, cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    // Super admin only; nobody may modify themselves or the super admin row.
    private async Task<ServiceResult<User>> LoadManageableUserAsync(Guid id, CancellationToken cancellationToken)
    {
        if (RequireSuperAdmin<User>() is { } denied)
        {
            return denied;
        }

        if (_currentUser.UserId == id)
        {
            return ServiceResult<User>.Conflict(
                "cannot_modify_self", "You cannot change your own account this way.");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return ServiceResult<User>.NotFound("user_not_found", "No user with that id.");
        }

        if (user.Role == UserRole.SuperAdmin)
        {
            return ServiceResult<User>.Conflict(
                "cannot_modify_super_admin", "The super admin account cannot be changed here.");
        }

        return ServiceResult<User>.Success(user);
    }

    private ServiceResult<T>? RequireSuperAdmin<T>() =>
        _currentUser.IsSuperAdmin ? null : ServiceResult<T>.Forbidden("forbidden", SuperAdminOnly);

    // Stores only the refresh token hash; expired rows are swept here (no timer needed).
    private async Task<(AuthResponse Response, RefreshToken Row)> IssueTokensAsync(
        User user,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var (accessToken, expiresAt) = _tokens.CreateAccessToken(user);

        var refreshToken = RefreshTokenGenerator.Create();
        var refreshExpiresAt = now.AddDays(_jwtOptions.RefreshTokenDays);

        var row = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = RefreshTokenGenerator.Hash(refreshToken),
            ExpiresAt = refreshExpiresAt,
            CreatedAt = now
        };

        _db.RefreshTokens.Add(row);

        await _db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.ExpiresAt < now)
            .ExecuteDeleteAsync(cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return (new AuthResponse
        {
            AccessToken = accessToken,
            ExpiresAt = expiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt,
            User = ToResponse(user)
        }, row);
    }

    // Called on password change and whenever access is removed.
    private Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(t => t.SetProperty(x => x.RevokedAt, DateTimeOffset.UtcNow), cancellationToken);

    private static ServiceResult<AuthResponse> InvalidCredentials() =>
        ServiceResult<AuthResponse>.Unauthorized("invalid_credentials", "Incorrect email or password.");

    private static ServiceResult<AuthResponse> InvalidRefreshToken() =>
        ServiceResult<AuthResponse>.Unauthorized("invalid_refresh_token", "This refresh token is not valid.");

    private static ServiceResult<AuthResponse> NotApproved(User user) => user.Status switch
    {
        UserStatus.EmailVerificationRequired => ServiceResult<AuthResponse>.Forbidden(
            "email_verification_required", "Please confirm your email address before signing in."),
        UserStatus.Pending => ServiceResult<AuthResponse>.Forbidden(
            "account_pending", "This account is waiting for super admin approval."),
        UserStatus.Rejected => ServiceResult<AuthResponse>.Forbidden(
            "account_rejected", user.RejectionReason ?? "This account was rejected."),
        _ => ServiceResult<AuthResponse>.Forbidden(
            "account_disabled", "This account has been disabled. Contact the super admin.")
    };

    private static string? ValidateRegistration(
        string? firstName,
        string? lastName,
        string? email,
        RegisterRequest request)
    {
        if (firstName is null)
        {
            return "First name is required.";
        }

        if (lastName is null)
        {
            return "Last name is required.";
        }

        if (email is null || !LooksLikeEmail(email))
        {
            return "A valid email address is required.";
        }

        return ValidatePassword(request.Password, request.ConfirmPassword, "Password");
    }

    private static string? ValidatePassword(string? password, string? confirmation, string label)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinimumPasswordLength)
        {
            return $"{label} must be at least {MinimumPasswordLength} characters.";
        }

        return password == confirmation ? null : $"{label} and its confirmation do not match.";
    }

    // Deliberately loose: the real check is whether mail arrives.
    private static bool LooksLikeEmail(string email)
    {
        var at = email.IndexOf('@');

        return at > 0
            && at == email.LastIndexOf('@')
            && at < email.Length - 1
            && !email.Contains(' ');
    }

    private static string? NormaliseEmail(string? email) => Blank(email)?.ToLowerInvariant();

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
