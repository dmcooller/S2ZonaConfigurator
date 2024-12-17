using System.Text.Json.Serialization;

namespace S2ZonaConfigurator.Models;
public record ModData
{
    [JsonPropertyName("version")]
    public string Version { get; init; } = "1.0";

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("actions")]
    public List<ModActionData> Actions { get; init; } = [];
}