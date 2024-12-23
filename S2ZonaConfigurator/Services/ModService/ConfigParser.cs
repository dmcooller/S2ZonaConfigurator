using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using S2ZonaConfigurator.Enums;
using S2ZonaConfigurator.Helpers;
using S2ZonaConfigurator.Interfaces.Services;
using S2ZonaConfigurator.Models;
using System.Text.Json;

namespace S2ZonaConfigurator.Services.ModService;

public partial class ConfigParser(ILogger<ConfigParser> logger, IOptions<AppConfig> config) : IConfigParser
{
    private readonly ILogger<ConfigParser> _logger = logger;
    private readonly AppConfig _config = config.Value;
    private string? _currentFile;
    private List<string> _currentContent = [];
    private const int IndentSize = 3;
    private const string STRUCT_BEGIN_LABEL = "struct.begin";
    private const string STRUCT_END_LABEL = "struct.end";
    private static readonly string[] structBeginSeparator = [": ", STRUCT_BEGIN_LABEL];


    [System.Text.RegularExpressions.GeneratedRegex(@"^\[(\d+)\]")]
    private static partial System.Text.RegularExpressions.Regex ArrayIndexRegex();

    private static List<string> ParsePath(string path)
    {
        // Split by :: but preserve array notation
        var components = new List<string>();
        var current = "";
        var inArrayBracket = false;

        foreach (var c in path)
        {
            if (c == '[')
                inArrayBracket = true;
            else if (c == ']')
                inArrayBracket = false;

            if (!inArrayBracket && c == ':' && current.EndsWith(':'))
            {
                components.Add(current.TrimEnd(':'));
                current = "";
                continue;
            }

            current += c;
        }

        if (!string.IsNullOrEmpty(current))
            components.Add(current);

        return components;
    }

    private static (int Start, int End) FindStructurePosition(List<string> lines, List<string> pathComponents)
    {
        int currentLine = 0;
        Stack<(int Line, string Name)> structureStack = new();
        Stack<string> currentPath = new();
        int targetStart = -1;
        int targetEnd = -1;
        int targetNestingLevel = -1;

        while (currentLine < lines.Count)
        {
            string line = lines[currentLine].Trim();

            // Handle structure/array beginning
            if (line.Contains(STRUCT_BEGIN_LABEL))
            {
                string structureName;
                if (line.Contains('['))
                {
                    // Handle array index
                    structureName = line.Split(':')[0].Trim();
                }
                else
                {
                    // Handle named structure
                    structureName = line.Split(structBeginSeparator, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                }

                structureStack.Push((currentLine, structureName));
                currentPath.Push(structureName);

                // Check if current path matches target path
                if (IsPathMatch(currentPath.Reverse().ToList(), pathComponents))
                {
                    targetStart = currentLine;
                    targetNestingLevel = structureStack.Count;
                }
            }
            // Handle structure/array end
            else if (line == STRUCT_END_LABEL)
            {
                if (structureStack.Count > 0)
                {
                    // If we found our target structure and we're back at its nesting level
                    if (targetStart != -1 && structureStack.Count == targetNestingLevel)
                    {
                        targetEnd = currentLine;
                        // If we're looking for a parameter, continue to find its containing structure
                        if (IsStructureOrArrayPath(pathComponents))
                        {
                            break;
                        }
                    }

                    structureStack.Pop();
                    if (currentPath.Count > 0)
                        currentPath.Pop();
                }
            }
            // Handle parameter assignment
            else if (line.Contains('='))
            {
                string paramName = line.Split('=')[0].Trim();
                List<string> currentFullPath = currentPath.Reverse().ToList();
                currentFullPath.Add(paramName);

                if (IsPathMatch(currentFullPath, pathComponents))
                {
                    // For parameters, return the containing structure's positions
                    if (structureStack.Count > 0)
                    {
                        var (Line, _) = structureStack.Peek();
                        targetStart = Line;
                        targetNestingLevel = structureStack.Count;
                    }
                }
            }

            currentLine++;
        }

        return (targetStart, targetEnd);
    }

    private static bool IsPathMatch(List<string> currentPath, List<string> targetPath)
    {
        if (currentPath.Count != targetPath.Count)
            return false;

        for (int i = 0; i < currentPath.Count; i++)
        {
            string current = currentPath[i];
            string target = targetPath[i];

            // Handle array indices
            if (current.StartsWith('[') && target.StartsWith('['))
            {
                if (current != target)
                    return false;
            }
            // Handle normal path components
            else if (current != target)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsStructureOrArrayPath(List<string> path)
    {
        if (path.Count == 0)
            return false;

        string lastComponent = path[^1];
        return lastComponent.StartsWith('[') || // Array index
               !lastComponent.Contains('='); // Not a parameter assignment
    }

    private string FormatValue(object? value)
    {
        return value switch
        {
            bool b => b.ToString().ToLowerInvariant(),
            int or float or double => value.ToString()!,
            string s => s,
            Dictionary<string, object> dict => FormatStructure(dict),
            JsonElement element => FormatJsonElement(element),
            _ => throw new ArgumentException($"Unsupported value type: {value?.GetType()}")
        };
    }

    private string FormatJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.String => element.GetString()!,
            JsonValueKind.Object => FormatStructure(JsonElementToDictionary(element)),
            JsonValueKind.Array => throw new ArgumentException("Array values are not supported"),
            _ => throw new ArgumentException($"Unsupported JSON value kind: {element.ValueKind}")
        };
    }

    private static Dictionary<string, object> JsonElementToDictionary(JsonElement element)
    {
        var dict = new Dictionary<string, object>();
        foreach (var property in element.EnumerateObject())
        {
            dict[property.Name] = property.Value;
        }
        return dict;
    }

    private string FormatStructure(Dictionary<string, object> structDict)
    {
        var lines = new List<string>();
        var indent = new string(' ', IndentSize);

        foreach (var (key, value) in structDict)
        {
            if (key == STRUCT_BEGIN_LABEL)
                lines.Add(STRUCT_BEGIN_LABEL);
            else if (key == STRUCT_END_LABEL)
                lines.Add(STRUCT_END_LABEL);
            else
                lines.Add($"{indent}{key} = {FormatValue(value)}");
        }

        return string.Join(Environment.NewLine, lines);
    }


    public void ApplyAction(ConfigAction action)
    {
        try
        {
            if (_currentFile != action.File)
                LoadFile(Path.Combine(_config.Paths.WorkDirectory, _config.Paths.ModifiedDirectory, action.File));

            // Handle Replace action separately due to different dictionary type requirement
            if (action.Type == ActionType.Replace)
            {
                var actionValue = action.Value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object
                    ? JsonHelper.JsonElementToStringDictionary(jsonElement)
                    : action.Value as Dictionary<string, string>;

                if (actionValue is not Dictionary<string, string> replacePair)
                    throw new ArgumentException("Value must be a Dictionary with 'old' and 'new' keys for Replace action");

                if (!replacePair.TryGetValue("old", out var oldText) ||
                    !replacePair.TryGetValue("new", out var newText))
                    throw new ArgumentException("Replace action requires 'old' and 'new' values");

                ReplaceSubstrings(oldText, newText, action.IsRegex);
                return;
            }

            // Handle all other action types
            var pathComponents = ParsePath(action.Path);

            switch (action.Type)
            {
                case ActionType.Modify when action.Values != null:
                    ModifyMultipleValues(pathComponents, action.Values);
                    break;
                case ActionType.Modify:
                    ModifyValue(pathComponents, action.Value);
                    break;
                case ActionType.Add:
                    AddValue(pathComponents, action.Value);
                    break;
                case ActionType.RemoveLine:
                    RemoveLine(pathComponents);
                    break;
                case ActionType.RemoveStruct:
                    RemoveStruct(pathComponents);
                    break;
                case ActionType.AddStruct when action.Structures != null:
                    if (pathComponents[^1].StartsWith('['))
                    {
                        // If the last path component is an array index, add named structures
                        AddMultipleNamedStructures(pathComponents, action.Structures);
                    }
                    else
                    {
                        // otherwise use array element handling
                        AddMultipleStructures(pathComponents, action.Structures);
                    }
                    break;
                case ActionType.AddStruct:
                    if (action.Value is Dictionary<string, object> structDict)
                        AddStructure(pathComponents, structDict);
                    else if (action.Value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
                        AddStructure(pathComponents, JsonElementToDictionary(jsonElement));
                    else
                        throw new ArgumentException("Value must be a Dictionary or JsonElement object for AddStructure action");
                    break;
                default:
                    throw new ArgumentException($"Unsupported action type: {action.Type}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying action {Type} to {Path}", action.Type, action.Path);
            throw;
        }
    }


    private void LoadFile(string filePath)
    {
        try
        {
            _currentContent = File.ReadAllLines(filePath).ToList();
            _currentFile = filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading file {FilePath}", filePath);
            throw;
        }
    }

    public void SaveFile()
    {
        if (_currentFile == null)
            throw new InvalidOperationException("No file currently loaded");

        try
        {
            File.WriteAllLines(_currentFile, _currentContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving file {FilePath}", _currentFile);
            throw;
        }
    }

    private (int LineNumber, string Indentation) FindValueLine(int startIdx, int endIdx, string key)
    {
        for (var i = startIdx; i <= endIdx; i++)
        {
            string line = _currentContent[i];
            string trimmedLine = line.TrimStart();
            if (trimmedLine.StartsWith($"{key} ="))
            {
                return (i, new string(' ', line.Length - trimmedLine.Length));
            }
        }
        throw new KeyNotFoundException($"Failed to find the target line {key}");
    }

    private void ModifyValue(List<string> pathComponents, object? newValue)
    {
        try
        {
            var parentPath = pathComponents[..^1];
            var (startIdx, endIdx) = FindStructurePosition(_currentContent, parentPath);
            if (!IsStructureBoundsValid(parentPath, startIdx, endIdx))
                throw new ArgumentOutOfRangeException($"Failed to find the structure's bounds. {string.Join("::", parentPath)} startIdx = {startIdx} endIdx = {endIdx}");

            var lastComponent = pathComponents[^1];
            var (targetLine, properIndentation) = FindValueLine(startIdx, endIdx, lastComponent);
            _currentContent[targetLine] = $"{properIndentation}{lastComponent} = {FormatValue(newValue)}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error modifying value for path: {Path}", string.Join(" -> ", pathComponents));
            throw;
        }
    }

    private void ModifyMultipleValues(List<string> pathComponents, Dictionary<string, object> values)
    {
        try
        {
            var (startIdx, endIdx) = FindStructurePosition(_currentContent, pathComponents);
            if (!IsStructureBoundsValid(pathComponents, startIdx, endIdx))
                throw new ArgumentOutOfRangeException($"Failed to find the structure's bounds. {string.Join("::", pathComponents)} startIdx = {startIdx} endIdx = {endIdx}");

            foreach (var (key, newValue) in values)
            {
                var (targetLine, properIndentation) = FindValueLine(startIdx, endIdx, key);
                _currentContent[targetLine] = $"{properIndentation}{key} = {FormatValue(newValue)}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error modifying multiple values");
            throw;
        }
    }


    private void AddValue(List<string> pathComponents, object? newValue)
    {
        try
        {
            var parentPath = pathComponents[..^1];
            var (startIdx, endIdx) = FindStructurePosition(_currentContent, parentPath);
            if (!IsStructureBoundsValid(parentPath, startIdx, endIdx))
                throw new ArgumentOutOfRangeException($"Failed to find the structure's bounds. {string.Join("::", parentPath)} startIdx = {startIdx} endIdx = {endIdx}");


            var newLines = new List<string>();
            newLines.Add($"   {pathComponents[^1]} = {FormatValue(newValue)}");

            for (var i = endIdx; i >= startIdx; i--)
            {
                if (_currentContent[i].Contains("struct.end"))
                {
                    var insertPosition = i;
                    foreach (var newLine in newLines)
                        _currentContent.Insert(insertPosition, newLine);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding value");
            throw;
        }
    }

    private void RemoveLine(List<string> pathComponents)
    {
        try
        {
            var parentPath = pathComponents[..^1];
            var (startIdx, endIdx) = FindStructurePosition(_currentContent, parentPath);
            if (!IsStructureBoundsValid(parentPath, startIdx, endIdx))
                throw new ArgumentOutOfRangeException($"Failed to find the structure's bounds. {string.Join("::", parentPath)} startIdx = {startIdx} endIdx = {endIdx}");

            for (var i = startIdx; i <= endIdx; i++)
            {
                var line = _currentContent[i].Trim();
                if (line.StartsWith(pathComponents[^1]))
                {
                    if (i > 0 && _currentContent[i - 1].Trim().StartsWith("//"))
                    {
                        _currentContent.RemoveAt(i - 1);
                        i--;
                    }
                    _currentContent.RemoveAt(i);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing value");
            throw;
        }
    }

    private void RemoveStruct(List<string> pathComponents)
    {
        try
        {
            var (startIdx, endIdx) = FindStructurePosition(_currentContent, pathComponents);
            if (!IsStructureBoundsValid(pathComponents, startIdx, endIdx))
                throw new ArgumentOutOfRangeException($"Failed to find the structure's bounds. {string.Join("::", pathComponents)} startIdx = {startIdx} endIdx = {endIdx}");

            // If we found the structure, we need to remove it and its ending struct.end
            if (startIdx >= 0 && startIdx < _currentContent.Count)
            {
                // Remove the structure and its content
                _currentContent.RemoveRange(startIdx, endIdx - startIdx + 1);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing structure at path: {Path}",
                string.Join(" -> ", pathComponents));
            throw;
        }
    }

    private void AddStructure(List<string> pathComponents, Dictionary<string, object> structure)
    {
        try
        {
            var parentPath = pathComponents[..^1];
            var (startIdx, endIdx) = FindStructurePosition(_currentContent, parentPath);
            if (!IsStructureBoundsValid(parentPath, startIdx, endIdx))
                throw new ArgumentOutOfRangeException($"Failed to find the structure's bounds. {string.Join("::", parentPath)} startIdx = {startIdx} endIdx = {endIdx}");

            var lastComponent = pathComponents[^1];

            // Find the parent's struct.end
            var insertPosition = endIdx;

            // Calculate proper indentation based on the parent structure's line
            var parentLine = _currentContent[startIdx];
            var baseIndentation = parentLine[..parentLine.IndexOf(parentPath[^1])].Length;
            var properIndentation = new string(' ', baseIndentation + IndentSize);

            // Create the new structure lines with proper ordering
            var newLines = new List<string>();
            newLines.Add($"{properIndentation}{lastComponent} : {STRUCT_BEGIN_LABEL}");
            foreach (var (key, value) in structure)
            {
                newLines.Add($"{properIndentation}   {key} = {FormatValue(value)}");
            }
            newLines.Add($"{properIndentation}{STRUCT_END_LABEL}");

            // Insert all lines right before the parent's struct.end
            for (var i = newLines.Count - 1; i >= 0; i--)
            {
                _currentContent.Insert(insertPosition, newLines[i]);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding structure");
            throw;
        }
    }

    private void AddMultipleStructures(List<string> parentPath, object[] structures)
    {
        try
        {
            var (startIdx, endIdx) = FindStructurePosition(_currentContent, parentPath);
            if (!IsStructureBoundsValid(parentPath, startIdx, endIdx))
                throw new ArgumentOutOfRangeException($"Failed to find the structure's bounds. {string.Join("::", parentPath)} startIdx = {startIdx} endIdx = {endIdx}");

            // Find the last array index in the current structure
            int lastIndex = -1;
            for (var i = startIdx; i <= endIdx; i++)
            {
                var line = _currentContent[i].TrimStart();
                var match = ArrayIndexRegex().Match(line);
                if (match.Success)
                {
                    var index = int.Parse(match.Groups[1].Value);
                    lastIndex = Math.Max(lastIndex, index);
                }
            }

            // Add each new structure with incrementing indices
            for (int i = 0; i < structures.Length; i++)
            {
                var nextIndex = lastIndex + 1 + i;
                var newPath = parentPath.Concat([$"[{nextIndex}]"]).ToList();

                if (structures[i] is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
                {
                    AddStructure(newPath, JsonElementToDictionary(jsonElement));
                }
                else if (structures[i] is Dictionary<string, object> dict)
                {
                    AddStructure(newPath, dict);
                }
                else
                {
                    throw new ArgumentException($"Invalid structure format at index {i}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding multiple structures");
            throw;
        }
    }

    private void AddMultipleNamedStructures(List<string> parentPath, object[] structures)
    {
        try
        {
            var (startIdx, endIdx) = FindStructurePosition(_currentContent, parentPath);
            if (!IsStructureBoundsValid(parentPath, startIdx, endIdx))
                throw new ArgumentOutOfRangeException($"Failed to find the structure's bounds. {string.Join("::", parentPath)} startIdx = {startIdx} endIdx = {endIdx}");

            foreach (var structure in structures)
            {
                if (structure is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
                {
                    // Each structure should have a single property which is the structure name
                    var structureObj = JsonElementToDictionary(jsonElement);
                    foreach (var (structureName, structureValue) in structureObj)
                    {
                        var newPath = parentPath.Concat([structureName]).ToList();
                        if (structureValue is JsonElement valueElement && valueElement.ValueKind == JsonValueKind.Object)
                        {
                            AddStructure(newPath, JsonElementToDictionary(valueElement));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding multiple named structures");
            throw;
        }
    }

    private void ReplaceSubstrings(string oldText, string newText, bool isRegex = false)
    {
        try
        {
            var replacements = 0;

            if (isRegex)
            {
                try
                {
                    var regex = new System.Text.RegularExpressions.Regex(oldText,
                        System.Text.RegularExpressions.RegexOptions.Compiled |
                        System.Text.RegularExpressions.RegexOptions.Multiline);

                    for (var i = 0; i < _currentContent.Count; i++)
                    {
                        var newContent = regex.Replace(_currentContent[i], newText);
                        if (newContent != _currentContent[i])
                        {
                            replacements += regex.Matches(_currentContent[i]).Count;
                            _currentContent[i] = newContent;
                        }
                    }
                }
                catch (System.Text.RegularExpressions.RegexParseException ex)
                {
                    _logger.LogError(ex, "Invalid regex pattern: '{Pattern}'", oldText);
                    throw new ArgumentException($"Invalid regex pattern: {ex.Message}", ex);
                }
            }
            else
            {
                // Original string replacement logic
                for (var i = 0; i < _currentContent.Count; i++)
                {
                    if (_currentContent[i].Contains(oldText))
                    {
                        var newContent = _currentContent[i].Replace(oldText, newText);
                        if (newContent != _currentContent[i])
                        {
                            replacements++;
                            _currentContent[i] = newContent;
                        }
                    }
                }
            }

            if (replacements == 0)
            {
                _logger.LogWarning("No {Type} matches found for '{Pattern}'",
                    isRegex ? "regex" : "text", oldText);
            }
            else
            {
                _logger.LogDebug("Replaced {Count} {Type} match(es) of '{Pattern}' with '{NewText}'",
                    replacements, isRegex ? "regex" : "text", oldText, newText);
            }
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            _logger.LogError(ex, "Error replacing {Type}", isRegex ? "regex" : "text");
            throw;
        }
    }

    private static bool IsStructureBoundsValid(List<string> parentPath, int startIdx, int endIdx)
    {
        if (startIdx < 0 || endIdx <= 0)
        {
            Printer.PrintErrorMessage($"Failed to find the structure's bounds. {parentPath} startIdx = {startIdx} endIdx = {endIdx}");
            return false;
        }
        return true;
    }
}