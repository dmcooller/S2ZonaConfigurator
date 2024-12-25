using S2ZonaConfigurator.Enums;

namespace S2ZonaConfigurator.Models;
public record ModConflict(
    string ModFile1,
    string ModFile2,
    string ConfigFile,
    string? Path,  // Can be null for Replace actions
    ActionType Action1,
    ActionType Action2,
    object? Value1,
    object? Value2,
    bool IsReplaceConflict = false
);
