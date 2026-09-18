using System.Text.Json;
using System.Text.Json.Serialization;

namespace FrostWoodTech.API.Common;

/// <summary>camelCase properties; enums as snake_case strings, matching the Postgres enums.</summary>
public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));

        return options;
    }
}
