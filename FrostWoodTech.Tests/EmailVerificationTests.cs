using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using FrostWoodTech.API.Auth;
using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Email;
using FrostWoodTech.API.Data;
using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Interfaces;
using FrostWoodTech.API.Services;

namespace FrostWoodTech.Tests;

public class EmailVerificationTests(PostgresFixture fixture) : DatabaseTest(fixture)
{
    [Fact]
    public async Task Registering_creates_an_unverified_account_and_issues_no_tokens()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db, out _);

        var result = await service.RegisterAsync(NewRegistration(out var email), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserStatus.EmailVerificationRequired, result.Value!.Status);
        Assert.Null(result.Value.EmailVerifiedAt);

        var tokenExists = await db.EmailVerificationTokens
            .AnyAsync(t => t.User.Email == email);
        Assert.True(tokenExists);
    }

    [Fact]
    public async Task A_valid_token_verifies_the_account_and_moves_it_to_pending()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db, out _);

        await service.RegisterAsync(NewRegistration(out var email), CancellationToken.None);
        var rawToken = await RawTokenForAsync(email);

        var result = await service.VerifyEmailAsync(new VerifyEmailRequest { Token = rawToken }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserStatus.Pending, result.Value!.Status);
        Assert.NotNull(result.Value.EmailVerifiedAt);
    }

    [Fact]
    public async Task An_unknown_token_is_rejected()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db, out _);

        var result = await service.VerifyEmailAsync(
            new VerifyEmailRequest { Token = "not-a-real-token" }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("invalid_verification_token", result.Error!.Code);
    }

    [Fact]
    public async Task A_token_cannot_be_used_twice()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db, out _);

        await service.RegisterAsync(NewRegistration(out var email), CancellationToken.None);
        var rawToken = await RawTokenForAsync(email);

        var first = await service.VerifyEmailAsync(new VerifyEmailRequest { Token = rawToken }, CancellationToken.None);
        Assert.True(first.IsSuccess);

        var second = await service.VerifyEmailAsync(new VerifyEmailRequest { Token = rawToken }, CancellationToken.None);

        Assert.False(second.IsSuccess);
        Assert.Equal("verification_token_already_used", second.Error!.Code);
    }

    [Fact]
    public async Task An_expired_token_is_rejected()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db, out _);

        await service.RegisterAsync(NewRegistration(out var email), CancellationToken.None);

        var token = await db.EmailVerificationTokens.Include(t => t.User)
            .FirstAsync(t => t.User.Email == email);
        token.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        var rawToken = EmailVerificationTokenGenerator.Create();
        token.TokenHash = EmailVerificationTokenGenerator.Hash(rawToken);
        await db.SaveChangesAsync();

        var result = await service.VerifyEmailAsync(new VerifyEmailRequest { Token = rawToken }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("verification_token_expired", result.Error!.Code);
    }

    [Fact]
    public async Task Verifying_invalidates_any_other_unused_token_for_the_same_user()
    {
        // Fresh context per step, like real requests: a shared tracker would hide ExecuteUpdate changes.
        string email;
        await using (var db = _fixture.CreateContext())
        {
            var registerResult = await NewService(db, out _)
                .RegisterAsync(NewRegistration(out email), CancellationToken.None);
            Assert.True(registerResult.IsSuccess);
        }

        var firstRawToken = await RawTokenForAsync(email);

        await using (var db = _fixture.CreateContext())
        {
            await NewService(db, out _)
                .ResendVerificationAsync(new ResendVerificationRequest { Email = email }, CancellationToken.None);
        }

        await using (var db = _fixture.CreateContext())
        {
            var verify = await NewService(db, out _)
                .VerifyEmailAsync(new VerifyEmailRequest { Token = firstRawToken }, CancellationToken.None);

            Assert.False(verify.IsSuccess);
            Assert.Equal("verification_token_already_used", verify.Error!.Code);
        }

        var secondRawToken = await RawTokenForAsync(email);

        await using (var db = _fixture.CreateContext())
        {
            var secondVerify = await NewService(db, out _)
                .VerifyEmailAsync(new VerifyEmailRequest { Token = secondRawToken }, CancellationToken.None);
            Assert.True(secondVerify.IsSuccess);
        }

        await using var freshDb = _fixture.CreateContext();
        var remainingUnused = await freshDb.EmailVerificationTokens
            .CountAsync(t => t.User.Email == email && t.UsedAt == null);
        Assert.Equal(0, remainingUnused);
    }

    [Fact]
    public async Task Resend_always_returns_the_generic_response_for_an_unknown_email()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db, out var emails);

        var result = await service.ResendVerificationAsync(
            new ResendVerificationRequest { Email = $"nobody-{Guid.NewGuid():N}@example.com" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, emails.SentCount);
    }

    [Fact]
    public async Task Resend_always_returns_the_generic_response_for_an_already_verified_account()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db, out var emails);

        await service.RegisterAsync(NewRegistration(out var email), CancellationToken.None);
        var rawToken = await RawTokenForAsync(email);
        await service.VerifyEmailAsync(new VerifyEmailRequest { Token = rawToken }, CancellationToken.None);

        emails.Reset();
        var result = await service.ResendVerificationAsync(
            new ResendVerificationRequest { Email = email }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, emails.SentCount);
    }

    [Fact]
    public async Task Resend_is_rate_limited_to_three_per_hour()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db, out var emails);

        await service.RegisterAsync(NewRegistration(out var email), CancellationToken.None);
        emails.Reset();

        // Registration already issued one token, so two resends hit the limit of three.
        await service.ResendVerificationAsync(new ResendVerificationRequest { Email = email }, CancellationToken.None);
        await service.ResendVerificationAsync(new ResendVerificationRequest { Email = email }, CancellationToken.None);
        Assert.Equal(2, emails.SentCount);

        var fourthAttempt = await service.ResendVerificationAsync(
            new ResendVerificationRequest { Email = email }, CancellationToken.None);

        Assert.True(fourthAttempt.IsSuccess);
        Assert.Equal(2, emails.SentCount);
    }

    [Fact]
    public async Task Approval_is_refused_while_the_account_is_still_unverified()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db, out _, superAdmin: true);

        var registered = await service.RegisterAsync(NewRegistration(out _), CancellationToken.None);

        var result = await service.ApproveAsync(registered.Value!.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("user_not_pending", result.Error!.Code);
    }

    [Fact]
    public async Task Login_is_refused_with_the_verification_required_code_before_the_link_is_clicked()
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db, out _);

        await service.RegisterAsync(NewRegistration(out var email), CancellationToken.None);

        var login = await service.LoginAsync(
            new LoginRequest { Email = email, Password = ValidPassword }, "127.0.0.1", CancellationToken.None);

        Assert.False(login.IsSuccess);
        Assert.Equal("email_verification_required", login.Error!.Code);
    }

    private const string ValidPassword = "correct horse battery staple";

    private static RegisterRequest NewRegistration(out string email)
    {
        email = $"user-{Guid.NewGuid():N}@example.com";

        return new RegisterRequest
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = email,
            Password = ValidPassword,
            ConfirmPassword = ValidPassword
        };
    }

    private async Task<string> RawTokenForAsync(string email)
    {
        // Raw tokens can't be recovered from hashes, so mint one and overwrite the stored hash.
        await using var db = _fixture.CreateContext();

        var token = await db.EmailVerificationTokens.Include(t => t.User)
            .Where(t => t.User.Email == email && t.UsedAt == null)
            .OrderByDescending(t => t.CreatedAt)
            .FirstAsync();

        var rawToken = EmailVerificationTokenGenerator.Create();
        token.TokenHash = EmailVerificationTokenGenerator.Hash(rawToken);
        await db.SaveChangesAsync();

        return rawToken;
    }

    private static UserService NewService(
        FrostWoodTechDbContext db, out FakeEmailService emails, bool superAdmin = false)
    {
        emails = new FakeEmailService();

        return TestServices.CreateUserService(
            db,
            emails,
            new CurrentUser { UserId = Guid.NewGuid(), Role = superAdmin ? UserRole.SuperAdmin : UserRole.Admin });
    }
}
