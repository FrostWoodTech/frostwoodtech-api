using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using FrostWoodTech.API.Auth;
using FrostWoodTech.API.Common;
using FrostWoodTech.API.Data;
using FrostWoodTech.API.Email;
using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Interfaces;
using FrostWoodTech.API.Services;

namespace FrostWoodTech.Tests;

public sealed class FakeEmailService : IEmailService
{
    public int SentCount { get; private set; }

    public EmailMessage? LastMessage { get; private set; }

    public void Reset()
    {
        SentCount = 0;
        LastMessage = null;
    }

    public Task<ServiceResult<EmailSendResult>> SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        SentCount++;
        LastMessage = message;

        return Task.FromResult(ServiceResult<EmailSendResult>.Success(new EmailSendResult { Provider = "fake" }));
    }
}

public sealed class FakeJwtTokenService : IJwtTokenService
{
    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user) =>
        ("fake-token", DateTimeOffset.UtcNow.AddMinutes(15));

    public TokenValidationParameters CreateValidationParameters() => new();
}

public sealed class FakeGoogleTokenValidator : IGoogleTokenValidator
{
    public Task<ServiceResult<GoogleIdentity>> ValidateAsync(string idToken, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Not exercised by these tests.");
}

public sealed class FakeLoginRateLimiter : ILoginRateLimiter
{
    public Task<bool> IsBlockedAsync(string email, string? ipAddress, AuthAttemptAction action, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public Task RecordAttemptAsync(string email, string? ipAddress, AuthAttemptAction action, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task ClearAsync(string email, AuthAttemptAction action, CancellationToken cancellationToken) => Task.CompletedTask;
}

public static class TestServices
{
    public const string AdminBaseUrl = "https://admin.frostwoodtech.test";

    public static UserService CreateUserService(
        FrostWoodTechDbContext db,
        IEmailService emails,
        CurrentUser? currentUser = null,
        ILoginRateLimiter? rateLimiter = null) =>
        new(
            db,
            new FakeJwtTokenService(),
            new FakeGoogleTokenValidator(),
            rateLimiter ?? new FakeLoginRateLimiter(),
            currentUser ?? new CurrentUser { UserId = Guid.NewGuid(), Role = UserRole.Admin },
            Options.Create(new JwtOptions()),
            emails,
            Options.Create(new EmailOptions { BaseUrl = AdminBaseUrl }),
            NullLogger<UserService>.Instance);
}
