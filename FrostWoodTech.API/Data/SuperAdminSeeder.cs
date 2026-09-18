using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using FrostWoodTech.API.Auth;
using FrostWoodTech.API.Email;
using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Data;

/// <summary>Seeds the single super admin with no password and emails a setup link. Never touches an existing account.</summary>
public static class SuperAdminSeeder
{
    private static readonly TimeSpan SetupTokenLifetime = TimeSpan.FromHours(24);

    private const long AdvisoryLockKey = 4_820_117_003L;

    public static async Task EnsureSeededAsync(
        FrostWoodTechDbContext db,
        SuperAdminOptions options,
        IEmailService email,
        EmailOptions emailOptions,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!options.IsConfigured)
        {
            logger.LogWarning("SuperAdmin__Email is not set — skipping super admin seeding.");
            return;
        }

        var address = options.Email.Trim().ToLowerInvariant();
        string? rawToken = null;

        // The execution strategy must own the transaction when retries are enabled.
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            // Advisory lock serialises concurrent cold starts until the transaction ends.
            await db.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock({0})", [AdvisoryLockKey], cancellationToken);

            var existingSuperAdmin = await db.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Role == UserRole.SuperAdmin, cancellationToken);

            if (existingSuperAdmin is not null)
            {
                if (!string.Equals(existingSuperAdmin.Email, address, StringComparison.OrdinalIgnoreCase))
                {
                    // Exactly one super admin: never promote a second from config.
                    logger.LogWarning(
                        "A super admin already exists as {ExistingEmail} but SuperAdmin__Email is {ConfiguredEmail}. Leaving the existing account alone.",
                        existingSuperAdmin.Email,
                        address);
                }

                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            // An address owned by another account needs a human.
            var addressTaken = await db.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Email == address, cancellationToken);

            if (addressTaken)
            {
                logger.LogWarning(
                    "SuperAdmin__Email {Email} already belongs to a non-super-admin account. Not seeding, and not modifying that account.",
                    address);

                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            var now = DateTimeOffset.UtcNow;
            rawToken = EmailVerificationTokenGenerator.Create();

            var user = new User
            {
                Email = address,
                FirstName = options.FirstName,
                LastName = options.LastName,
                PasswordHash = null,
                Role = UserRole.SuperAdmin,
                Status = UserStatus.Approved,
                // Trusted config, nothing to verify.
                EmailVerifiedAt = now
            };

            db.Users.Add(user);
            db.PasswordTokens.Add(new PasswordToken
            {
                User = user,
                TokenHash = EmailVerificationTokenGenerator.Hash(rawToken),
                Purpose = PasswordTokenPurpose.Setup,
                ExpiresAt = now.Add(SetupTokenLifetime),
                CreatedAt = now
            });

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                // Lost a race; the account exists, so don't send a link for a rolled-back token.
                logger.LogWarning(
                    ex, "Super admin seeding lost a race — the account already exists. No mail sent.");

                rawToken = null;
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            logger.LogWarning(
                "Seeded the super admin account {Email} with no password. Sending a setup link.", address);
        });

        if (rawToken is not null)
        {
            await SendSetupLinkAsync(
                email, emailOptions, address, options.FirstName, rawToken, logger, cancellationToken);
        }
    }

    // Called after the commit so a slow mail provider can't hold the transaction open.
    private static async Task SendSetupLinkAsync(
        IEmailService email,
        EmailOptions emailOptions,
        string address,
        string firstName,
        string rawToken,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var link = $"{emailOptions.BaseUrl.TrimEnd('/')}/set-password?token={rawToken}";

        var sent = await email.SendAsync(new EmailMessage
        {
            To = address,
            Subject = "Set your FrostWoodTech super admin password",
            HtmlBody =
                $"<p>Hi {firstName},</p>" +
                "<p>Your FrostWoodTech super admin account has been created. Choose a password to finish setting it up:</p>" +
                $"<p><a href=\"{link}\">{link}</a></p>" +
                "<p>This link expires in 24 hours and can only be used once. Until then the account cannot be signed in to.</p>",
            TextBody =
                $"Hi {firstName},\n\n" +
                "Your FrostWoodTech super admin account has been created. Choose a password to finish setting it up:\n" +
                $"{link}\n\n" +
                "This link expires in 24 hours and can only be used once. Until then the account cannot be signed in to."
        }, cancellationToken);

        if (!sent.IsSuccess)
        {
            // Error level: there is no self-service resend for this link.
            logger.LogError(
                "The super admin setup link for {Email} could not be sent: {Code}. Issue a fresh one before the 24 hours are up.",
                address,
                sent.Error!.Code);
        }
    }
}
