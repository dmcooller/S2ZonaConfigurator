using Microsoft.Extensions.Logging;
using S2ZonaConfigurator.Helpers;
using S2ZonaConfigurator.Interfaces.Services;
using S2ZonaConfigurator.Models;
using System.Text;
using System.Text.Json;


namespace S2ZonaConfigurator.Services.ModService;


public class ModProcessor(ILogger<ModProcessor> logger, IConfigParser parser) : IModProcessor
{
    private readonly ILogger<ModProcessor> _logger = logger;
    private readonly IConfigParser _parser = parser;


    private int _totalModsProcessed = 0;
    private int _modsSucess = 0;
    private int _modsFailed = 0;

    public void ProcessMod(string modFile, ModData modData)
    {
        try
        {
            _totalModsProcessed++;
            Printer.PrintModHeader(Path.GetFileNameWithoutExtension(modFile), modData.Version, _totalModsProcessed);

            for (int i = 0; i < modData.Actions.Count; i++)
            {
                var actionData = modData.Actions[i];
                Printer.PrintActionProgress(i + 1, modData.Actions.Count, actionData);
                var action = new ConfigAction(
                    actionData.Type,
                    actionData.File,
                    actionData.Path,
                    actionData.Value,
                    actionData.Values,
                    actionData.Structures,
                    actionData.IsRegex
                );

                _parser.ApplyAction(action);
                _parser.SaveFile();
            }

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
            .Where(file => !Path.GetFileName(file).StartsWith('$'));

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
                _logger.LogWarning("Failed to parse mod file: {ModFile}. Skiping", modFile);
                continue;
            }
        }

        return modDataMap;
    }

    public static HashSet<string> GetRequiredConfigFiles(Dictionary<string, ModData> modDataMap)
    {
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
}
