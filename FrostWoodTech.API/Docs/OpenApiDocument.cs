using System.Reflection;

namespace FrostWoodTech.API.Docs;

/// <summary>The hand-written openapi.yaml embedded in the assembly.</summary>
public static class OpenApiDocument
{
    private const string ResourceName = "FrostWoodTech.API.Docs.openapi.yaml";

    private static readonly Lazy<string> Document = new(Read, LazyThreadSafetyMode.ExecutionAndPublication);

    public const string ContentType = "application/yaml";

    public static string Yaml => Document.Value;

    private static string Read()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{ResourceName}' is missing. Check the EmbeddedResource item in FrostWoodTech.API.csproj.");

        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
