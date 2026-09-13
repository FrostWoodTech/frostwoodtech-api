using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Auth;

/// <summary>Singleton so Google's signing keys are cached between sign-ins.</summary>
public sealed class GoogleTokenValidator : IGoogleTokenValidator
{
    private const string Discovery = "https://accounts.google.com/.well-known/openid-configuration";

    private static readonly string[] ValidIssuers = ["https://accounts.google.com", "accounts.google.com"];

    private readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
    private readonly GoogleOptions _options;

    public GoogleTokenValidator(IOptions<GoogleOptions> options)
    {
        _options = options.Value;

        _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            Discovery,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever());
    }

    public async Task<ServiceResult<GoogleIdentity>> ValidateAsync(
        string idToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            // Misconfigured: refuse rather than accept anything.
            return ServiceResult<GoogleIdentity>.Forbidden(
                "google_not_configured", "Google sign-in is not configured on this server.");
        }

        OpenIdConnectConfiguration configuration;
        try
        {
            configuration = await _configurationManager.GetConfigurationAsync(cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return ServiceResult<GoogleIdentity>.Unauthorized(
                "google_unavailable", "Could not reach Google to verify the token. Try again.");
        }

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = ValidIssuers,
            ValidateAudience = true,
            ValidAudience = _options.ClientId,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = configuration.SigningKeys
        };

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(idToken, parameters);
        if (!result.IsValid)
        {
            return ServiceResult<GoogleIdentity>.Unauthorized(
                "invalid_google_token", "The Google token is invalid or has expired.");
        }

        var subject = Claim(result.Claims, "sub");
        var email = Claim(result.Claims, "email");

        if (subject is null || email is null)
        {
            return ServiceResult<GoogleIdentity>.Unauthorized(
                "invalid_google_token", "The Google token is missing a subject or email.");
        }

        return ServiceResult<GoogleIdentity>.Success(new GoogleIdentity(
            subject,
            email.ToLowerInvariant(),
            // Sometimes sent as a string rather than a bool.
            EmailVerified: bool.TryParse(Claim(result.Claims, "email_verified"), out var verified) && verified,
            FirstName: Claim(result.Claims, "given_name"),
            LastName: Claim(result.Claims, "family_name"),
            AvatarUrl: Claim(result.Claims, "picture")));
    }

    private static string? Claim(IDictionary<string, object> claims, string name) =>
        claims.TryGetValue(name, out var value) ? value?.ToString() : null;
}
