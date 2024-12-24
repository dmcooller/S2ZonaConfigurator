using S2ZonaConfigurator.Enums;
using System.Text.Json.Serialization;

namespace S2ZonaConfigurator.Models;
public record ModActionData
{
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ActionType Type { get; init; }

    [JsonPropertyName("file")]
    public string? File { get; init; }

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("values")]
    public Dictionary<string, object>? Values { get; init; }

    [JsonPropertyName("value")]
    public object? Value { get; init; }
    [JsonPropertyName("structures")]
    public object[]? Structures { get; init; }

    [JsonPropertyName("defaultValue")]
    public object? DefaultValue { get; init; }

    [JsonPropertyName("isRegex")]
    public bool IsRegex { get; init; }
}