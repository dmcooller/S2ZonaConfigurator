using S2ZonaConfigurator.Enums;
using S2ZonaConfigurator.Interfaces.Services;
using S2ZonaConfigurator.Models;

namespace S2ZonaConfigurator.Services.ModConflictDetectorService;
public class ModConflictDetector : IModConflictDetector
{
    public List<ModConflict> DetectConflicts(Dictionary<string, ModData> modDataMap)
    {
        var conflicts = new List<ModConflict>();
        var modPairs = GetModPairs(modDataMap);

        foreach (var (mod1File, mod2File) in modPairs)
        {
            var mod1 = modDataMap[mod1File];
            var mod2 = modDataMap[mod2File];

            var mod1Actions = GetModificationMap(mod1);
            var mod2Actions = GetModificationMap(mod2);

            // Check for conflicts between the two mods
            foreach (var (file, modifications1) in mod1Actions)
            {
                if (!mod2Actions.TryGetValue(file, out var modifications2))
                    continue;

                // Check path-based action conflicts
                foreach (var (path, action1) in modifications1.PathActions)
                {
                    if (modifications2.PathActions.TryGetValue(path, out var action2))
                    {
                        if (IsConflicting(action1, action2))
                        {
                            conflicts.Add(new ModConflict(
                                mod1File,
                                mod2File,
                                file,
                                path,
                                action1.Type,
                                action2.Type,
                                action1.Value,
                                action2.Value
                            ));
                        }
                    }
                }

                // Check Replace action conflicts
                if (modifications1.ReplaceActions.Count != 0 && modifications2.ReplaceActions.Count != 0)
                {
                    foreach (var replace1 in modifications1.ReplaceActions)
                    {
                        foreach (var replace2 in modifications2.ReplaceActions)
                        {
                            if (IsReplaceConflicting(replace1, replace2))
                            {
                                conflicts.Add(new ModConflict(
                                    mod1File,
                                    mod2File,
                                    file,
                                    null,
                                    replace1.Type,
                                    replace2.Type,
                                    replace1.Value,
                                    replace2.Value,
                                    IsReplaceConflict: true
                                ));
                            }
                        }
                    }
                }

                // Check conflicts between Replace actions and path-based actions
                if (modifications1.ReplaceActions.Count != 0 && modifications2.PathActions.Count != 0 ||
                    modifications1.PathActions.Count != 0 && modifications2.ReplaceActions.Count != 0)
                {
                    conflicts.Add(new ModConflict(
                        mod1File,
                        mod2File,
                        file,
                        null,
                        modifications1.ReplaceActions.FirstOrDefault()?.Type ?? modifications1.PathActions.First().Value.Type,
                        modifications2.ReplaceActions.FirstOrDefault()?.Type ?? modifications2.PathActions.First().Value.Type,
                        null,
                        null,
                        IsReplaceConflict: true
                    ));
                }
            }
        }

        return conflicts;
    }

    private static IEnumerable<(string, string)> GetModPairs(Dictionary<string, ModData> modDataMap)
    {
        var modFiles = modDataMap.Keys.ToList();
        for (int i = 0; i < modFiles.Count; i++)
        {
            for (int j = i + 1; j < modFiles.Count; j++)
            {
                yield return (modFiles[i], modFiles[j]);
            }
        }
    }

    private class FileModifications
    {
        public Dictionary<string, ModActionData> PathActions { get; } = [];
        public List<ModActionData> ReplaceActions { get; } = [];
    }

    private static Dictionary<string, FileModifications> GetModificationMap(ModData mod)
    {
        var result = new Dictionary<string, FileModifications>();
        string currentFile = string.Empty;

        foreach (var action in mod.Actions)
        {
            // Update current file if specified in the action
            if (!string.IsNullOrEmpty(action.File))
            {
                currentFile = action.File;
            }
            // Skip if we haven't encountered a file property yet
            else if (string.IsNullOrEmpty(currentFile))
            {
                continue;
            }

            if (!result.TryGetValue(currentFile, out FileModifications? value))
            {
                value = new FileModifications();
                result[currentFile] = value;
            }

            // Create a new ModActionData with the current file path if it was inherited
            var actionWithFile = string.IsNullOrEmpty(action.File)
                ? action with { File = currentFile }
                : action;

            if (action.Type == ActionType.Replace)
            {
                value.ReplaceActions.Add(actionWithFile);
            }
            else
            {
                value.PathActions[action.Path] = actionWithFile;
            }
        }

        return result;
    }

    private static bool IsConflicting(ModActionData action1, ModActionData action2)
    {
        // Different types of modifications to the same path are always considered conflicts
        if (action1.Type != action2.Type)
            return true;

        return action1.Type switch
        {
            // For Modify actions, check if the values are different
            ActionType.Modify => !AreValuesEqual(action1.Value, action2.Value),

            // Add actions conflict if they try to add different values
            ActionType.Add => !AreValuesEqual(action1.Value, action2.Value),

            // Remove actions don't conflict with each other
            ActionType.RemoveLine => false,
            ActionType.RemoveStruct => false,

            // AddStruct actions conflict if they try to add different structures
            ActionType.AddStruct => !AreStructuresEqual(action1.Structures, action2.Structures),

            // Replace action is handled separately
            _ => false
        };
    }

    private static bool IsReplaceConflicting(ModActionData replace1, ModActionData replace2)
    {
        // Try to convert values to dictionaries
        if (replace1.Value is not Dictionary<string, object> dict1 ||
            replace2.Value is not Dictionary<string, object> dict2)
        {
            // Try to convert JsonElement to dictionary if needed
            dict1 = replace1.Value is System.Text.Json.JsonElement je1 ? JsonElementToDictionary(je1) : null;
            dict2 = replace2.Value is System.Text.Json.JsonElement je2 ? JsonElementToDictionary(je2) : null;

            if (dict1 == null || dict2 == null)
                return true;
        }

        // Get old/new values
        var old1 = dict1.GetValueOrDefault("old")?.ToString();
        var new1 = dict1.GetValueOrDefault("new")?.ToString();
        var old2 = dict2.GetValueOrDefault("old")?.ToString();
        var new2 = dict2.GetValueOrDefault("new")?.ToString();

        if (old1 == null || new1 == null || old2 == null || new2 == null)
            return true;

        // For non-regex replaces, check if they're trying to replace the same text
        if (!replace1.IsRegex && !replace2.IsRegex)
        {
            return old1 == old2;
        }

        // If either one uses regex
        try
        {
            // Create regexes, escaping the pattern if it's not a regex replace
            var pattern1 = replace1.IsRegex ? old1 : System.Text.RegularExpressions.Regex.Escape(old1);
            var pattern2 = replace2.IsRegex ? old2 : System.Text.RegularExpressions.Regex.Escape(old2);

            var regex1 = new System.Text.RegularExpressions.Regex(pattern1);
            var regex2 = new System.Text.RegularExpressions.Regex(pattern2);

            // Test if the patterns might affect each other's replacements
            if (regex1.IsMatch(old2) || regex2.IsMatch(old1))
                return true;

            // Test if either pattern matches the other's replacement value
            if (regex1.IsMatch(new2) || regex2.IsMatch(new1))
                return true;

            return false;
        }
        catch
        {
            // If regex is invalid, consider it a conflict
            return true;
        }
    }

    private static Dictionary<string, object> JsonElementToDictionary(System.Text.Json.JsonElement element)
    {
        var dict = new Dictionary<string, object>();
        foreach (var property in element.EnumerateObject())
        {
            dict[property.Name] = property.Value;
        }
        return dict;
    }

    private static bool AreValuesEqual(object? value1, object? value2)
    {
        if (value1 == null && value2 == null)
            return true;

        if (value1 == null || value2 == null)
            return false;

        return value1.ToString() == value2.ToString();
    }

    private static bool AreStructuresEqual(object[]? structures1, object[]? structures2)
    {
        if (structures1 == null && structures2 == null)
            return true;

        if (structures1 == null || structures2 == null)
            return false;

        if (structures1.Length != structures2.Length)
            return false;

        for (int i = 0; i < structures1.Length; i++)
        {
            if (!AreValuesEqual(structures1[i], structures2[i]))
                return false;
        }

        return true;
    }
}