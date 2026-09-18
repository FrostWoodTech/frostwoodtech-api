using Microsoft.IdentityModel.Tokens;

using FrostWoodTech.API.Entities;

namespace FrostWoodTech.API.Interfaces;

public interface IJwtTokenService
{
    /// <summary>Claims: sub, email, role, jti.</summary>
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user);

    TokenValidationParameters CreateValidationParameters();
}
