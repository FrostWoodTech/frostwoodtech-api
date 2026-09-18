using Microsoft.IdentityModel.JsonWebTokens;

using FrostWoodTech.API.Auth;
using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Enums;

using MsOptions = Microsoft.Extensions.Options.Options;

namespace FrostWoodTech.Tests;

public class AuthPrimitivesTests
{
    private static readonly JwtOptions JwtSettings = new() { Signer = new string('k', 64) };

    [Fact]
    public void A_password_hash_verifies_the_right_password_only()
    {
        var hash = PasswordHasher.Hash("correct horse battery staple");

        Assert.StartsWith("$argon2id$v=19$", hash);
        Assert.True(PasswordHasher.Verify(hash, "correct horse battery staple"));
        Assert.False(PasswordHasher.Verify(hash, "Correct horse battery staple"));
    }

    [Fact]
    public void Hashing_the_same_password_twice_uses_a_fresh_salt()
    {
        Assert.NotEqual(PasswordHasher.Hash("same"), PasswordHasher.Hash("same"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("plain-text")]
    [InlineData("$argon2id$v=19$m=0,t=2,p=1$AAAA$AAAA")]
    [InlineData("$argon2id$v=19$m=19456,t=2,p=1$not base64!$AAAA")]
    public void A_missing_or_malformed_hash_never_verifies(string? stored)
    {
        Assert.False(PasswordHasher.Verify(stored, "anything"));
    }

    [Fact]
    public void Generated_tokens_are_url_safe_random_and_hash_deterministically()
    {
        var first = RefreshTokenGenerator.Create();
        var second = RefreshTokenGenerator.Create();

        Assert.NotEqual(first, second);
        Assert.Matches("^[A-Za-z0-9_-]{43}$", first);
        Assert.Equal(RefreshTokenGenerator.Hash(first), RefreshTokenGenerator.Hash(first));
        Assert.Matches("^[0-9a-f]{64}$", RefreshTokenGenerator.Hash(first));
    }

    [Fact]
    public async Task An_access_token_carries_the_agreed_claims_and_a_fifteen_minute_lifetime()
    {
        var service = new JwtTokenService(MsOptions.Create(JwtSettings));
        var user = NewUser();

        var (token, expiresAt) = service.CreateAccessToken(user);

        Assert.InRange(expiresAt - DateTimeOffset.UtcNow, TimeSpan.FromMinutes(14), TimeSpan.FromMinutes(15));

        var validated = await new JsonWebTokenHandler().ValidateTokenAsync(token, service.CreateValidationParameters());
        Assert.True(validated.IsValid);
        Assert.Equal(user.Id.ToString(), validated.Claims["sub"]);
        Assert.Equal(user.Email, validated.Claims["email"]);
        Assert.Equal(nameof(UserRole.SuperAdmin), validated.Claims["role"]);
        Assert.True(Guid.TryParse(validated.Claims["jti"].ToString(), out _));
    }

    [Fact]
    public async Task A_token_signed_with_another_key_is_rejected()
    {
        var issuer = new JwtTokenService(MsOptions.Create(new JwtOptions { Signer = new string('x', 64) }));
        var validator = new JwtTokenService(MsOptions.Create(JwtSettings));

        var (token, _) = issuer.CreateAccessToken(NewUser());

        var validated = await new JsonWebTokenHandler().ValidateTokenAsync(token, validator.CreateValidationParameters());
        Assert.False(validated.IsValid);
    }

    [Fact]
    public void A_missing_signer_fails_fast_at_startup()
    {
        Assert.Throws<InvalidOperationException>(() => new JwtTokenService(MsOptions.Create(new JwtOptions())));
    }

    private static User NewUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "root@example.com",
        FirstName = "Root",
        LastName = "Admin",
        Role = UserRole.SuperAdmin,
        Status = UserStatus.Approved
    };
}
