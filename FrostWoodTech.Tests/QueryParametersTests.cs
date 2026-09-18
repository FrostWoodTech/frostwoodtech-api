using Microsoft.AspNetCore.Http;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.Tests;

public class QueryParametersTests
{
    [Theory]
    [InlineData(null, null, 1, QueryParameters.DefaultPageSize)]
    [InlineData("0", "0", 1, 1)]
    [InlineData("-5", "-1", 1, 1)]
    [InlineData("3", "1000", 3, QueryParameters.MaxPageSize)]
    [InlineData("abc", "xyz", 1, QueryParameters.DefaultPageSize)]
    public void Paging_is_clamped_to_sane_bounds(string? page, string? pageSize, int expectedPage, int expectedPageSize)
    {
        var request = Request(("page", page), ("pageSize", pageSize));

        Assert.Equal((expectedPage, expectedPageSize), QueryParameters.ReadPaging(request));
    }

    [Theory]
    [InlineData("agency", Site.Agency)]
    [InlineData("Personal", Site.Personal)]
    [InlineData("  AGENCY ", Site.Agency)]
    public void A_known_site_name_is_read(string raw, Site expected)
    {
        Assert.True(QueryParameters.TryReadSite(Request(("site", raw)), out var site));
        Assert.Equal(expected, site);
    }

    [Fact]
    public void A_missing_site_is_valid_but_null_so_the_caller_can_answer_site_required()
    {
        Assert.True(QueryParameters.TryReadSite(Request(), out var site));
        Assert.Null(site);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("99")]
    [InlineData("-1")]
    [InlineData("agency,personal")]
    [InlineData("both")]
    public void Anything_but_a_site_name_is_rejected(string raw)
    {
        Assert.False(QueryParameters.TryReadSite(Request(("site", raw)), out var site));
        Assert.Null(site);
    }

    [Fact]
    public void Snake_case_enum_values_are_read()
    {
        Assert.True(QueryParameters.TryReadEnum<TechCategory>(Request(("category", "tool_or_platform")), "category", out var category));
        Assert.Equal(TechCategory.ToolOrPlatform, category);
    }

    [Fact]
    public void Bool_guid_and_string_readers_return_null_for_absent_or_malformed_values()
    {
        var id = Guid.NewGuid();
        var request = Request(("flag", "true"), ("bad", "yes"), ("id", id.ToString()), ("blank", "   "), ("name", " hi "));

        Assert.True(QueryParameters.ReadBool(request, "flag"));
        Assert.Null(QueryParameters.ReadBool(request, "bad"));
        Assert.Null(QueryParameters.ReadBool(request, "missing"));
        Assert.Equal(id, QueryParameters.ReadGuid(request, "id"));
        Assert.Null(QueryParameters.ReadGuid(request, "bad"));
        Assert.Null(QueryParameters.ReadString(request, "blank"));
        Assert.Equal("hi", QueryParameters.ReadString(request, "name"));
    }

    private static HttpRequest Request(params (string Name, string? Value)[] query)
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = QueryString.Create(
            query.Where(q => q.Value is not null).Select(q => new KeyValuePair<string, string?>(q.Name, q.Value)));

        return context.Request;
    }
}
