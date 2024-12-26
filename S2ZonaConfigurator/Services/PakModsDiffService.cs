using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using S2ZonaConfigurator.Helpers;
using S2ZonaConfigurator.Interfaces;
using S2ZonaConfigurator.Interfaces.Services;
using S2ZonaConfigurator.Models;

namespace S2ZonaConfigurator.Services;

public class PakModsDiffService(
    ILogger<PakModsDiffService> logger,
    IOptions<AppConfig> config,
    HelperService helper,
    IPakManager pakManagerGame,
    IPakManager pakManagerMods,
    IDiffService diffService) : IAppService
{
    private readonly ILogger<PakModsDiffService> _logger = logger;
    private readonly AppConfig _config = config.Value;
    private readonly HelperService _helper = helper;
    private readonly IPakManager _pakManagerGame = pakManagerGame;
    private readonly IPakManager _pakManagerMods = pakManagerMods;
    private readonly IDiffService _diffService = diffService;

    private readonly string _pakModsPath = helper.GetPakModsPath();
    private readonly string _workVanillaPath = helper.GetWorkVanillaPath();
    private readonly string _workModsPath = helper.GetWorkModsPath();

    public async Task RunAsync()
    {
        try
        {
            if (!Directory.Exists(_pakModsPath))
            {
                Printer.PrintErrorSection($"Pak mods directory not found at: {_pakModsPath}");
                return;
            }

            // Get all PAK files in the mods directory
            var modPakFiles = Directory.GetFiles(_pakModsPath, "*.pak", SearchOption.TopDirectoryOnly);
            if (modPakFiles.Length == 0)
            {
                Printer.PrintErrorSection("No PAK files found in mods directory");
                return;
            }

            // Initialize PAK managers
            _pakManagerGame.Initialize();
            _pakManagerMods.Initialize(_pakModsPath);

            Printer.PrintInfoSection("Extracting required files from the game");
            // Process each mod PAK file
            foreach (var pakFile in modPakFiles)
            {
                _logger.LogInformation("Processing mod PAK: {PakFile}", Path.GetFileName(pakFile));

                // Extract modded files
                await _pakManagerMods.ExtractFromModPak(pakFile, _workModsPath);

                // For each extracted file, extract its vanilla counterpart
                var extractedFiles = Directory.GetFiles(_workModsPath, "*.*", SearchOption.AllDirectories);
                foreach (var modFile in extractedFiles)
                {
                    var relativePath = Path.GetRelativePath(_workModsPath, modFile);
                    try
                    {
                        await _pakManagerGame.ExtractFile(relativePath);
                    }
                    catch (FileNotFoundException)
                    {
                        // Skip if the file is not found in the vanilla PAK
                        continue;
                    }
                }
            }

            // Generate diff report
            string reportPath = await _diffService.GenerateDiffReport(_workVanillaPath, _workModsPath, "");

            Printer.PrintInfoSection($"Diff report generated: {reportPath}");

            // Clean up if configured
            if (_config.Options.CleanWorkDirectory)
                _helper.CleanWorkDirectory();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during mod extraction and comparison");
            throw;
        }
    }
}