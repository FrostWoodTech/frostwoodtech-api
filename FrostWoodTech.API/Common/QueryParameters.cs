using Microsoft.AspNetCore.Http;

using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Common;

public static class QueryParameters
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static (int Page, int PageSize) ReadPaging(HttpRequest request)
    {
        var page = ReadInt(request, "page") ?? 1;
        var pageSize = ReadInt(request, "pageSize") ?? DefaultPageSize;

        return (Math.Max(page, 1), Math.Clamp(pageSize, 1, MaxPageSize));
    }

    public static bool? ReadBool(HttpRequest request, string name) =>
        bool.TryParse(request.Query[name], out var value) ? value : null;

    public static Guid? ReadGuid(HttpRequest request, string name) =>
        Guid.TryParse(request.Query[name], out var value) ? value : null;

    public static int? ReadInt(HttpRequest request, string name) =>
        int.TryParse(request.Query[name], out var value) ? value : null;

    public static string? ReadString(HttpRequest request, string name)
    {
        var value = request.Query[name].ToString();

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>Parses a snake_case enum name. Returns false only when present but invalid.</summary>
    public static bool TryReadEnum<TEnum>(HttpRequest request, string name, out TEnum? value)
        where TEnum : struct, Enum
    {
        value = null;

        var raw = ReadString(request, name);
        if (raw is null)
        {
            return true;
        }

        // Names only: Enum.TryParse also accepts "1", "99" and "agency,personal".
        if (raw.All(c => char.IsAsciiLetter(c) || c == '_')
            && Enum.TryParse<TEnum>(raw.Replace("_", string.Empty), ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    /// <summary>False for an unknown site; true with null when absent (answer with SiteRequired).</summary>
    public static bool TryReadSite(HttpRequest request, out Site? site) =>
        TryReadEnum(request, "site", out site);
}
