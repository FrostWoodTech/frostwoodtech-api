using FrostWoodTech.API.Common;

namespace FrostWoodTech.Tests;

public class SlugGeneratorTests
{
    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("  Leading and trailing  ", "leading-and-trailing")]
    [InlineData("Café Déjà Vu", "cafe-deja-vu")]
    [InlineData("C# & .NET -- 10!", "c-net-10")]
    [InlineData("already-a-slug", "already-a-slug")]
    [InlineData("UPPER_snake_Case", "upper-snake-case")]
    public void Titles_become_lowercase_hyphenated_slugs(string input, string expected)
    {
        Assert.Equal(expected, SlugGenerator.Generate(input));
    }

    [Theory]
    [InlineData("!!!")]
    [InlineData("日本語")]
    [InlineData("   ")]
    public void Input_with_no_ascii_letters_or_digits_produces_an_empty_slug(string input)
    {
        Assert.Equal(string.Empty, SlugGenerator.Generate(input));
    }
}
