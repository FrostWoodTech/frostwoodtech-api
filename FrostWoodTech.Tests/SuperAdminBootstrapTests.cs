using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using FrostWoodTech.API.Auth;
using FrostWoodTech.API.Common;
using FrostWoodTech.API.Data;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Email;
using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Interfaces;
using FrostWoodTech.API.Services;

namespace FrostWoodTech.Tests;

public class SuperAdminBootstrapTests(PostgresFixture fixture) : DatabaseTest(fixture)
{
    [Fact]
    public async Task Seeding_an_empty_database_creates_one_passwordless_super_admin_and_one_link()
    {
        var email = await ResetSuperAdminAsync();
        var emails = await SeedAsync(email);

        Assert.Equal(1, emails.SentCount);
        Assert.Contains("/set-password?token=", emails.LastMessage!.HtmlBody);

        await using var db = _fixture.CreateContext();
        var user = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Email == email);

        Assert.Null(user.PasswordHash);
        Assert.Equal(UserRole.SuperAdmin, user.Role);
        Assert.Equal(UserStatus.Approved, user.Status);
        Assert.NotNull(user.EmailVerifiedAt);

        var tokens = await db.PasswordTokens.CountAsync(t => t.UserId == user.Id && t.UsedAt == null);
        Assert.Equal(1, tokens);
    }

    [Fact]
    public async Task Seeding_twice_creates_one_account_and_sends_one_link()
    {
        var email = await ResetSuperAdminAsync();

        await SeedAsync(email);
        var second = await SeedAsync(email);

        Assert.Equal(0, second.SentCount);

        await using var db = _fixture.CreateContext();
        Assert.Equal(1, await db.Users.IgnoreQueryFilters().CountAsync(u => u.Role == UserRole.SuperAdmin));
        Assert.Equal(1, await db.PasswordTokens.CountAsync(t => t.User.Email == email));
    }

    [Fact]
    public async Task Seeding_never_touches_an_account_that_already_owns_the_configured_address()
    {
        var email = await ResetSuperAdminAsync();
        const string knownHash = "$argon2id$not-a-real-hash";

        await using (var db = _fixture.CreateContext())
        {
            db.Users.Add(new User
            {
                Email = email,
                FirstName = "Already",
                LastName = "Here",
                PasswordHash = knownHash,
                Role = UserRole.Admin,
                Status = UserStatus.Approved
            });
            await db.SaveChangesAsync();
        }

        var emails = await SeedAsync(email);

        Assert.Equal(0, emails.SentCount);

        await using var fresh = _fixture.CreateContext();
        var user = await fresh.Users.IgnoreQueryFilters().SingleAsync(u => u.Email == email);

        Assert.Equal(knownHash, user.PasswordHash);
        Assert.Equal(UserRole.Admin, user.Role);
    }

    [Fact]
    public async Task Seeding_is_a_no_op_when_the_email_is_blank()
    {
        await ResetSuperAdminAsync();

        var emails = await SeedAsync("   ");

        Assert.Equal(0, emails.SentCount);

        await using var db = _fixture.CreateContext();
        Assert.False(await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Role == UserRole.SuperAdmin));
    }

    [Fact]
    public async Task A_seeded_super_admin_cannot_log_in_before_the_link_is_redeemed()
    {
        var email = await ResetSuperAdminAsync();
        await SeedAsync(email);

        await using var db = _fixture.CreateContext();
        var login = await NewService(db, out _).LoginAsync(
            new LoginRequest { Email = email, Password = NewPassword }, "127.0.0.1", CancellationToken.None);

        Assert.False(login.IsSuccess);
        // A null hash looks like a wrong password, not account_pending.
        Assert.Equal("invalid_credentials", login.Error!.Code);
    }

    [Fact]
    public async Task Redeeming_the_link_sets_the_password_and_login_then_succeeds()
    {
        var email = await ResetSuperAdminAsync();
        await SeedAsync(email);
        var rawToken = await RawSetupTokenForAsync(email);

        await using (var db = _fixture.CreateContext())
        {
            var result = await NewService(db, out _).SetPasswordAsync(NewSetPassword(rawToken), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(UserRole.SuperAdmin, result.Value!.Role);
        }

        await using var login = _fixture.CreateContext();
        var signIn = await NewService(login, out _).LoginAsync(
            new LoginRequest { Email = email, Password = NewPassword }, "127.0.0.1", CancellationToken.None);

        Assert.True(signIn.IsSuccess);
    }

    [Fact]
    public async Task A_setup_token_cannot_be_used_twice()
    {
        var email = await ResetSuperAdminAsync();
        await SeedAsync(email);
        var rawToken = await RawSetupTokenForAsync(email);

        await using (var db = _fixture.CreateContext())
        {
            Assert.True((await NewService(db, out _)
                .SetPasswordAsync(NewSetPassword(rawToken), CancellationToken.None)).IsSuccess);
        }

        await using var second = _fixture.CreateContext();
        var replay = await NewService(second, out _)
            .SetPasswordAsync(NewSetPassword(rawToken), CancellationToken.None);

        Assert.False(replay.IsSuccess);
        Assert.Equal("setup_token_already_used", replay.Error!.Code);
    }

    [Fact]
    public async Task An_unknown_setup_token_is_rejected()
    {
        await using var db = _fixture.CreateContext();

        var result = await NewService(db, out _)
            .SetPasswordAsync(NewSetPassword("not-a-real-token"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("invalid_setup_token", result.Error!.Code);
    }

    [Fact]
    public async Task An_expired_setup_token_is_rejected()
    {
        var email = await ResetSuperAdminAsync();
        await SeedAsync(email);
        var rawToken = await RawSetupTokenForAsync(email);

        await using (var db = _fixture.CreateContext())
        {
            await db.PasswordTokens
                .Where(t => t.User.Email == email)
                .ExecuteUpdateAsync(t => t.SetProperty(x => x.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1)));
        }

        await using var fresh = _fixture.CreateContext();
        var result = await NewService(fresh, out _)
            .SetPasswordAsync(NewSetPassword(rawToken), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("setup_token_expired", result.Error!.Code);
    }

    [Fact]
    public async Task A_setup_token_still_enforces_the_password_rules()
    {
        var email = await ResetSuperAdminAsync();
        await SeedAsync(email);
        var rawToken = await RawSetupTokenForAsync(email);

        await using var db = _fixture.CreateContext();
        var service = NewService(db, out _);

        var tooShort = await service.SetPasswordAsync(
            new SetPasswordRequest { Token = rawToken, Password = "short", ConfirmPassword = "short" },
            CancellationToken.None);
        Assert.False(tooShort.IsSuccess);

        var mismatched = await service.SetPasswordAsync(
            new SetPasswordRequest { Token = rawToken, Password = NewPassword, ConfirmPassword = "something else" },
            CancellationToken.None);
        Assert.False(mismatched.IsSuccess);
    }

    [Fact]
    public async Task Registration_still_requires_a_password()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db, out _);

        var request = new RegisterRequest
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = $"user-{Guid.NewGuid():N}@example.com",
            Password = null,
            ConfirmPassword = null
        };

        var result = await service.RegisterAsync(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation_failed", result.Error!.Code);
    }

    private const string NewPassword = "correct horse battery staple";

    private static SetPasswordRequest NewSetPassword(string token) => new()
    {
        Token = token,
        Password = NewPassword,
        ConfirmPassword = NewPassword
    };

    // Only one super admin may exist, so each test clears the slot.
    private async Task<string> ResetSuperAdminAsync()
    {
        await using var db = _fixture.CreateContext();

        await db.Users
            .IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.SuperAdmin)
            .ExecuteDeleteAsync();

        return $"root-{Guid.NewGuid():N}@example.com";
    }

    private async Task<FakeEmailService> SeedAsync(string email)
    {
        await using var db = _fixture.CreateContext();
        var emails = new FakeEmailService();

        await SuperAdminSeeder.EnsureSeededAsync(
            db,
            new SuperAdminOptions { Email = email, FirstName = "Root", LastName = "Admin" },
            emails,
            new EmailOptions { BaseUrl = TestServices.AdminBaseUrl },
            NullLogger.Instance);

        return emails;
    }

    private async Task<string> RawSetupTokenForAsync(string email)
    {
        await using var db = _fixture.CreateContext();

        var token = await db.PasswordTokens
            .Include(t => t.User)
            .Where(t => t.User.Email == email && t.UsedAt == null)
            .OrderByDescending(t => t.CreatedAt)
            .FirstAsync();

        var rawToken = EmailVerificationTokenGenerator.Create();
        token.TokenHash = EmailVerificationTokenGenerator.Hash(rawToken);
        await db.SaveChangesAsync();

        return rawToken;
    }

    private static UserService NewService(FrostWoodTechDbContext db, out FakeEmailService emails)
    {
        emails = new FakeEmailService();

        return TestServices.CreateUserService(db, emails);
    }
}
