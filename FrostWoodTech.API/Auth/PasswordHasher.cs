using System.Security.Cryptography;
using System.Text;

using Konscious.Security.Cryptography;

namespace FrostWoodTech.API.Auth;

/// <summary>Argon2id; salt and cost parameters live in the stored string, so costs can rise later.</summary>
public static class PasswordHasher
{
    private const string Prefix = "$argon2id$";
    private const int SaltBytes = 16;
    private const int HashBytes = 32;
    private const int MemoryKib = 19 * 1024;
    private const int Iterations = 2;
    private const int Parallelism = 1;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Derive(password, salt, MemoryKib, Iterations, Parallelism, HashBytes);

        return $"{Prefix}v=19$m={MemoryKib},t={Iterations},p={Parallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    /// <summary>Constant-time; false for a missing or malformed hash.</summary>
    public static bool Verify(string? storedHash, string password)
    {
        if (string.IsNullOrWhiteSpace(storedHash) || !storedHash.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        // $argon2id$v=19$m=...,t=...,p=...$<salt>$<hash>
        var parts = storedHash.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5)
        {
            return false;
        }

        var parameters = parts[2].Split(',');
        if (parameters.Length != 3
            || !TryReadParameter(parameters[0], "m", out var memoryKib)
            || !TryReadParameter(parameters[1], "t", out var iterations)
            || !TryReadParameter(parameters[2], "p", out var parallelism))
        {
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[3]);
            expected = Convert.FromBase64String(parts[4]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Derive(password, salt, memoryKib, iterations, parallelism, expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    /// <summary>Same work as a real verify, so unknown emails can't be detected by timing.</summary>
    public static void BurnVerifyTime(string password) =>
        Derive(password, RandomNumberGenerator.GetBytes(SaltBytes), MemoryKib, Iterations, Parallelism, HashBytes);

    private static byte[] Derive(string password, byte[] salt, int memoryKib, int iterations, int parallelism, int length)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memoryKib,
            Iterations = iterations,
            DegreeOfParallelism = parallelism
        };

        return argon2.GetBytes(length);
    }

    private static bool TryReadParameter(string part, string name, out int value)
    {
        value = 0;

        return part.StartsWith($"{name}=", StringComparison.Ordinal)
            && int.TryParse(part[(name.Length + 1)..], out value)
            && value > 0;
    }
}
