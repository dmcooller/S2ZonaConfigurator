using System;
using System.Text.Json;

namespace S2ZonaConfigurator.Helpers;
public static class JsonHelper
{
    public static Dictionary<string, string> JsonElementToStringDictionary(JsonElement element)
    {
        var dict = new Dictionary<string, string>();
        foreach (var property in element.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.String)
            {
                dict[property.Name] = property.Value.GetString()!;
            }
            else
            {
                dict[property.Name] = property.Value.ToString();
            }
        }
        return dict;
    }
}
