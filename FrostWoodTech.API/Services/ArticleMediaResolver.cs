using System.Text.RegularExpressions;

using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Services;

public partial class ArticleMediaResolver : IArticleMediaResolver
{
    private readonly IMediaService _mediaService;

    public ArticleMediaResolver(IMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    public string ResolveMediaReferences(string markdown) =>
        string.IsNullOrEmpty(markdown)
            ? markdown
            : MediaReferencePattern().Replace(markdown, m => _mediaService.GetPublicUrl(m.Groups[1].Value));

    // media://<key> maps straight to the storage object key; no lookup needed.
    [GeneratedRegex(@"media://([\w\-./]+)")]
    private static partial Regex MediaReferencePattern();
}
