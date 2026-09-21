using System.Reflection;
using System.Text;

namespace FrostWoodTech.API.Docs;

/// <summary>The hand-written openapi.yaml embedded in the assembly.</summary>
public static class OpenApiDocument
{
    private const string ResourceName = "FrostWoodTech.API.Docs.openapi.yaml";

    /// <summary>Relative, so it resolves against whichever host serves the docs.</summary>
    public const string CurrentHostServerUrl = "/api";

    private static readonly Lazy<string> Document = new(Read, LazyThreadSafetyMode.ExecutionAndPublication);

    public const string ContentType = "application/yaml";

    public static string Yaml => Document.Value;

    /// <summary>The spec with its top-level <c>servers:</c> block replaced by the current host plus <paramref name="extraUrls"/>.</summary>
    public static string WithServers(IReadOnlyList<string> extraUrls) => ReplaceServers(Yaml, extraUrls);

    public static string ReplaceServers(string yaml, IReadOnlyList<string> extraUrls)
    {
        ArgumentNullException.ThrowIfNull(yaml);
        ArgumentNullException.ThrowIfNull(extraUrls);

        var lines = yaml.Split('\n');
        var start = Array.FindIndex(lines, line => line.TrimEnd('\r') == "servers:");

        if (start < 0)
        {
            throw new InvalidOperationException("openapi.yaml has no top-level 'servers:' block.");
        }

        var end = start + 1;
        while (end < lines.Length && (lines[end].TrimEnd('\r').Length == 0 || char.IsWhiteSpace(lines[end][0])))
        {
            end++;
        }

        var newline = lines[start].EndsWith('\r') ? "\r\n" : "\n";
        var block = new StringBuilder()
            .Append("servers:").Append(newline)
            .Append("  - url: ").Append(CurrentHostServerUrl).Append(newline)
            .Append("    description: This host").Append(newline);

        foreach (var url in extraUrls.Select(u => u.Trim()).Where(u => u.Length > 0 && u != CurrentHostServerUrl).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            block.Append("  - url: \"").Append(url.Replace("\"", "\\\"", StringComparison.Ordinal)).Append('"').Append(newline);
        }

        // Keep the blank line separating servers from the next section.
        block.Append(newline);

        var before = string.Join('\n', lines[..start]);
        var after = string.Join('\n', lines[end..]);

        return before + (start > 0 ? "\n" : string.Empty) + block + after;
    }

    private static string Read()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{ResourceName}' is missing. Check the EmbeddedResource item in FrostWoodTech.API.csproj.");

        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
