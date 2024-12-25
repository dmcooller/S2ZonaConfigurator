using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using S2ZonaConfigurator.Helpers;
using S2ZonaConfigurator.Interfaces.Services;
using S2ZonaConfigurator.Models;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace S2ZonaConfigurator.Services.ModService;

public class ModProcessor(ILogger<ModProcessor> logger, IOptions<AppConfig> config, IConfigParser parser) : IModProcessor
{
    private readonly ILogger<ModProcessor> _logger = logger;
    private readonly AppConfig _config = config.Value;
    private readonly IConfigParser _parser = parser;

    private int _totalModsProcessed = 0;
    private int _modsSucess = 0;
    private int _modsFailed = 0;

    private readonly string _pakNamePrefix = Path.GetFileNameWithoutExtension(config.Value.Paths.OutputPakName);

    public void ProcessMod(string modFile, ModData modData)
    {
        try
        {
            _totalModsProcessed++;
            Printer.PrintModHeader(Path.GetFileNameWithoutExtension(modFile), modData.Version, _totalModsProcessed);

            string? currentFile = null;
            string? previousFile = null;

            for (int i = 0; i < modData.Actions.Count; i++)
            {
                var actionData = modData.Actions[i];

                // Update current file only if explicitly specified in the action
                if (!string.IsNullOrEmpty(actionData.File))
                {
                    previousFile = currentFile ?? actionData.File;
                    currentFile = actionData.File;
                }
                else if (currentFile == null)
                {
                    throw new InvalidOperationException($"No file path specified for action in mod {modFile}");
                }

                // Save the file if the next action is for a different file
                if (currentFile != previousFile)
                    _parser.SaveFile();

                Printer.PrintActionProgress(i + 1, modData.Actions.Count, actionData);
                var action = new ConfigAction(
                    actionData.Type,
                    currentFile,
                    actionData.Path,
                    actionData.Value,
                    actionData.Values,
                    actionData.Structures,
                    actionData.IsRegex
                );

                _parser.ApplyAction(action);

                // Save the file if this is the last action
                if (i == modData.Actions.Count - 1)
                    _parser.SaveFile();
            }

            // Copy additional files if they exist
            CopyAdditionalFiles(modFile);

            Printer.PrintModFooter();
            _modsSucess++;
        }
        catch (Exception ex)
        {
            Printer.PrintModFooter(false);
            _modsFailed++;
            _logger.LogError(ex, "Error processing mod {ModFile}", modFile);
        }
    }

    public Dictionary<string, ModData> ParseModFiles(string ModsDirectory)
    {
        var modDataMap = new Dictionary<string, ModData>();
        // Get all *.json files in the mods directory but skip files with names that start with a $ sign
        var modFiles = Directory.GetFiles(ModsDirectory, "*.json", SearchOption.AllDirectories)
            .Where(file => !Path.GetFileName(file).StartsWith('$')).OrderBy(file => Path.GetFileName(file));

        foreach (var modFile in modFiles)
        {
            try
            {
                var modJson = File.ReadAllText(modFile);
                var modData = JsonSerializer.Deserialize(modJson, ConfigJsonContext.Default.ModData);

                if (modData != null)
                {
                    modDataMap[modFile] = modData;
                }
            }
            catch (Exception)
            {
                // Skip invalid mod files
                Printer.PrintErrorMessage($"Failed to parse mod file: {modFile}. Check the syntax and try again");
                throw;
            }
        }

        return modDataMap;
    }

    public static HashSet<string> GetRequiredConfigFiles(Dictionary<string, ModData> modDataMap)
    {
        Printer.PrintInfoSection("Extracting required config files from the game");

        var requiredFiles = new HashSet<string>();

        foreach (var modData in modDataMap.Values)
        {
            foreach (var action in modData.Actions)
            {
                var configPath = StringHelper.NormalizeConfigPath(action.File);
                if (!string.IsNullOrWhiteSpace(configPath))
                {
                    requiredFiles.Add(configPath);
                }
            }
        }

        return requiredFiles;
    }

    public void PrintFinalSummary()
    {
        Printer.PrintFinalSummary(_totalModsProcessed, _modsSucess, _modsFailed);
    }

    public void GenerateChangelog(string pakFilePath, Dictionary<string, ModData> modDataMap)
    {
        try
        {
            var changelogPath = Path.ChangeExtension(pakFilePath, ".txt");
            var changelogBuilder = new StringBuilder();

            changelogBuilder.AppendLine("# Changelog");
            changelogBuilder.AppendLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            changelogBuilder.AppendLine();

            foreach (var (modFile, modData) in modDataMap)
            {
                if (!string.IsNullOrWhiteSpace(modData.Description))
                {
                    changelogBuilder.AppendLine($"## {Path.GetFileNameWithoutExtension(modFile)}");
                    changelogBuilder.AppendLine($"- {modData.Description}");
                    changelogBuilder.AppendLine();
                }
            }

            File.WriteAllText(changelogPath, changelogBuilder.ToString());
            _logger.LogInformation("Generated changelog at {Path}", changelogPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate changelog");
            // Don't throw - we don't want to fail the whole process if changelog generation fails
        }
    }

    private void CopyAdditionalFiles(string modFile)
    {
        try
        {
            // Get the mod name (same as json file without extension)
            string modName = Path.GetFileNameWithoutExtension(modFile);
            string modDirPath = Path.GetDirectoryName(modFile)!;

            // Get the destination path (our ~mods folder)
            string modsDestPath = Path.Combine(_config.Game.GamePath, _config.Paths.PaksPath, "~mods");

            // Try folder first
            string modFolderPath = Path.Combine(modDirPath, modName);
            if (Directory.Exists(modFolderPath))
            {
                CopyFromFolder(modFolderPath, modsDestPath, _pakNamePrefix);
                _logger.LogInformation("Successfully copied additional files from folder for mod: {ModName}", modName);
            }

            // Try ZIP file
            string modZipPath = Path.Combine(modDirPath, $"{modName}.zip");
            if (File.Exists(modZipPath))
            {
                ExtractFromZip(modZipPath, modsDestPath, _pakNamePrefix);
                _logger.LogInformation("Successfully extracted additional files from ZIP for mod: {ModName}", modName);
            }

            // If neither exists, log debug message
            if (!Directory.Exists(modFolderPath) && !File.Exists(modZipPath))
            {
                _logger.LogDebug("No additional files (folder or ZIP) found for mod: {ModName}", modName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing additional files for mod: {ModFile}", modFile);
            throw;
        }
    }

    private void CopyFromFolder(string sourceFolderPath, string destinationBasePath, string pakNamePrefix)
    {
        foreach (var file in Directory.GetFiles(sourceFolderPath, "*.*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(sourceFolderPath, file);
            string relativeDir = Path.GetDirectoryName(relativePath) ?? string.Empty;
            string fileName = Path.GetFileName(relativePath);
            string prefixedFileName = $"{pakNamePrefix}_{fileName}";
            string destinationPath = Path.Combine(destinationBasePath, relativeDir, prefixedFileName);

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            File.Copy(file, destinationPath, true);

            _logger.LogDebug("Copied file: {Source} to {Destination}", file, destinationPath);
        }

        Printer.PrintColoredField("Copied folder", Path.GetFileName(sourceFolderPath), ConsoleColor.DarkCyan);
    }

    private void ExtractFromZip(string zipPath, string destinationBasePath, string pakNamePrefix)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            // Skip directories
            if (string.IsNullOrEmpty(entry.Name))
                continue;

            string relativeDir = Path.GetDirectoryName(entry.FullName) ?? string.Empty;
            string fileName = Path.GetFileName(entry.FullName);
            string prefixedFileName = $"{pakNamePrefix}_{fileName}";
            string destinationPath = Path.Combine(destinationBasePath, relativeDir, prefixedFileName);
            string? destinationDir = Path.GetDirectoryName(destinationPath);

            if (destinationDir != null)
            {
                Directory.CreateDirectory(destinationDir);
                entry.ExtractToFile(destinationPath, true);
                _logger.LogDebug("Extracted file: {Source} to {Destination}", entry.FullName, destinationPath);
            }
        }
        Printer.PrintColoredField("Extracted ZIP", Path.GetFileName(zipPath), ConsoleColor.DarkCyan);
    }
}
