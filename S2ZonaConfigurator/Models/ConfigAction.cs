using S2ZonaConfigurator.Enums;

namespace S2ZonaConfigurator.Models;
public record ConfigAction(
    ActionType Type,
    string File,
    string Path,
    object? Value = null,
    Dictionary<string, object>? Values = null,
    object[]? Structures = null,
    bool IsRegex = false
);