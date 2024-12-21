using System.Text.Json.Serialization;

namespace S2ZonaConfigurator.Models;

// Use the Source Genarator to be able to use trimming
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Default,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ModData))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(object[]))]
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(PathsConfig))]
[JsonSerializable(typeof(GameConfig))]
internal partial class ConfigJsonContext : JsonSerializerContext
{
}