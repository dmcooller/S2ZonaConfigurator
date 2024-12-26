using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using S2ZonaConfigurator.Helpers;
using S2ZonaConfigurator.Interfaces;
using S2ZonaConfigurator.Interfaces.Services;
using S2ZonaConfigurator.Models;
using S2ZonaConfigurator.Services.ModService;

namespace S2ZonaConfigurator.Services;

public class MainService(
    ILogger<MainService> logger,
    IOptions<AppConfig> appConfig,
    IPakManager pakManager,
    IModProcessor modProcessor,
    IModConflictDetector conflictDetector,
    IDiffService diffService) : IAppService
{
    private readonly ILogger<MainService> _logger = logger;
    private readonly AppConfig _appConfig = appConfig.Value;
    private readonly IPakManager _pakManager = pakManager;
    private readonly IModProcessor _modProcessor = modProcessor;
    private readonly IModConflictDetector _conflictDetector = conflictDetector;
    private readonly IDiffService _diffService = diffService;

    public async Task RunAsync()
    {
        try
        {
            // Initialize services
            _pakManager.Initialize();

            // Parse mod files
            var modDataMap = _modProcessor.ParseModFiles(_appConfig.Paths.ModsDirectory);
            if (modDataMap.Count == 0)
            {
                Printer.PrintInfoSection($"No mods to apply. Make sure you have mods in `{_appConfig.Paths.ModsDirectory}` directory");
                return;
            }

            // Check for conflicts
            if (_appConfig.Options.DetectModConflicts)
            {
                var conflicts = _conflictDetector.DetectConflicts(modDataMap);
                if (conflicts.Count != 0)
                {
                    Printer.PrintConflicts(conflicts);
                    throw new InvalidOperationException("Mod conflicts detected. Please resolve conflicts before proceeding.");
                }
            }

            // Extract a list of required config file paths
            var requiredConfigs = ModProcessor.GetRequiredFiles(modDataMap);

            // Extract required config files from PAKs
            foreach (var config in requiredConfigs)
            {
                await _pakManager.ExtractFile(config);
            }

            // Copy extracted files to Mods directory. We will modify these files
            _pakManager.CopyExtractedFilesToMods();

            // Process mods
            foreach (var (modFile, modData) in modDataMap)
            {
                _modProcessor.ProcessMod(modFile, modData);
            }

            _modProcessor.PrintFinalSummary();

            // Generate diff report
            if (_appConfig.Options.GenerateDiffReport)
            {
                string vanillaPath = Path.Combine(_appConfig.Paths.WorkDirectory, _appConfig.Paths.VanillaDirectory);
                string modsPath = Path.Combine(_appConfig.Paths.WorkDirectory, _appConfig.Paths.ModifiedDirectory);
                string reportPath = await _diffService.GenerateDiffReport(vanillaPath, modsPath, "");
                Printer.PrintInfoSection($"Diff report generated at: {reportPath}");
            }

            // Delete old mods from the game directory
            if (_appConfig.Options.DeleteOldMods)
                _pakManager.DeleteOldMods();

            // Create PAK file with mods
            await _pakManager.CreateModPak();

            // Generate Optional changelog
            if (_appConfig.Options.OutputChangelogFile)
                _modProcessor.GenerateChangelog(_pakManager.GetOutputPakPath(), modDataMap);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during mod extraction and comparison");
            throw;
        }
    }
}