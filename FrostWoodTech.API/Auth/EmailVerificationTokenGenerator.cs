using System.Security.Cryptography;
using System.Text;

namespace FrostWoodTech.API.Auth;

/// <summary>SHA-256 is enough: tokens have 256 bits of entropy and are looked up by hash.</summary>
public static class EmailVerificationTokenGenerator
{
    private const int TokenBytes = 32;

    public static string Create() =>
        Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenBytes));

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
