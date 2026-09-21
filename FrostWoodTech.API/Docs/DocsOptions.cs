namespace FrostWoodTech.API.Docs;

public sealed class DocsOptions
{
    /// <summary>Off by default; the spec maps the whole admin surface.</summary>
    public bool Enabled { get; set; }

    /// <summary>Pinned version so the docs page can't change unexpectedly.</summary>
    public string ScalarCdnUrl { get; set; } =
        "https://cdn.jsdelivr.net/npm/@scalar/api-reference@1.25.28/dist/browser/standalone.min.js";

    /// <summary>Extra servers listed after the serving host, e.g. https://api.example.com/api.</summary>
    public string[] ServerUrls { get; set; } = [];

    public bool HasValidServerUrls() =>
        ServerUrls.All(url =>
            Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp));
}
