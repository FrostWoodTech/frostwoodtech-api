using FrostWoodTech.API.Docs;

namespace FrostWoodTech.Tests;

public class OpenApiDocumentTests
{
    private static List<string> ServerUrls(string yaml)
    {
        var lines = yaml.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        var start = lines.IndexOf("servers:");

        return lines.Skip(start + 1)
            .TakeWhile(l => l.Length == 0 || char.IsWhiteSpace(l[0]))
            .Where(l => l.StartsWith("  - url: ", StringComparison.Ordinal))
            .Select(l => l["  - url: ".Length..].Trim('"'))
            .ToList();
    }

    [Fact]
    public void Without_extras_only_the_serving_host_is_listed()
    {
        var yaml = OpenApiDocument.WithServers([]);

        Assert.Equal(["/api"], ServerUrls(yaml));
        Assert.DoesNotContain("localhost", yaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<your-function-app>", yaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Extras_follow_the_serving_host_in_order_without_duplicates()
    {
        var yaml = OpenApiDocument.WithServers(
            ["https://api.example.com/api", "http://localhost:7060/api", "https://API.example.com/api", "/api", " "]);

        Assert.Equal(["/api", "https://api.example.com/api", "http://localhost:7060/api"], ServerUrls(yaml));
    }

    [Fact]
    public void Only_the_servers_block_changes()
    {
        var original = OpenApiDocument.Yaml;
        var yaml = OpenApiDocument.WithServers(["https://api.example.com/api"]);

        var originalStart = original.IndexOf("\ntags:", StringComparison.Ordinal);
        var rewrittenStart = yaml.IndexOf("\ntags:", StringComparison.Ordinal);

        Assert.Equal(original[originalStart..], yaml[rewrittenStart..]);
        Assert.Equal(
            original[..original.IndexOf("servers:", StringComparison.Ordinal)],
            yaml[..yaml.IndexOf("servers:", StringComparison.Ordinal)]);
        Assert.Contains("\n\ntags:", yaml.ReplaceLineEndings("\n"), StringComparison.Ordinal);
    }

    [Fact]
    public void Missing_servers_block_throws()
    {
        Assert.Throws<InvalidOperationException>(() => OpenApiDocument.ReplaceServers("openapi: 3.1.0\n", []));
    }

    [Theory]
    [InlineData("https://api.example.com/api", true)]
    [InlineData("http://localhost:7060/api", true)]
    [InlineData("/api", false)]
    [InlineData("ftp://example.com/api", false)]
    [InlineData("not a url", false)]
    public void Server_urls_must_be_absolute_http(string url, bool valid)
    {
        Assert.Equal(valid, new DocsOptions { ServerUrls = [url] }.HasValidServerUrls());
    }
}
