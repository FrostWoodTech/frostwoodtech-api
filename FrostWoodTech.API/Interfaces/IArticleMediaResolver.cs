namespace FrostWoodTech.API.Interfaces;

/// <summary>Resolves media:// tokens in article Markdown to storage URLs; stored Markdown never holds real URLs.</summary>
public interface IArticleMediaResolver
{
    string ResolveMediaReferences(string markdown);
}
