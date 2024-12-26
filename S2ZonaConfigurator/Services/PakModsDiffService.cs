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
    IPakManager pakManagerGame,
    IPakManager pakManagerMods,
    IDiffService diffService) : IAppService
{
    private readonly ILogger<PakModsDiffService> _logger = logger;
    private readonly AppConfig _config = config.Value;
    private readonly IPakManager _pakManagerGame = pakManagerGame;
    private readonly IPakManager _pakManagerMods = pakManagerMods;
    private readonly IDiffService _diffService = diffService;

    public async Task RunAsync()
    {
        try
        {
            // Get the mods directory path
            string paksPath = Path.Combine(_config.Paths.PakModsDirectory);
            if (!Directory.Exists(paksPath))
            {
                _logger.LogWarning("Pak mods directory not found at: {Path}", paksPath);
                return;
            }

            // Get all PAK files in the mods directory
            var modPakFiles = Directory.GetFiles(paksPath, "*.pak", SearchOption.TopDirectoryOnly);
            if (modPakFiles.Length == 0)
            {
                _logger.LogWarning("No PAK files found in mods directory");
                return;
            }

            // Create work directories
            var vanillaPath = Path.Combine(_config.Paths.WorkDirectory, _config.Paths.VanillaDirectory);
            var modsPath = Path.Combine(_config.Paths.WorkDirectory, _config.Paths.ModifiedDirectory);
            Directory.CreateDirectory(vanillaPath);
            Directory.CreateDirectory(modsPath);

            // Initialize the PAK managers
            _pakManagerGame.Initialize();
            _pakManagerMods.Initialize(paksPath);

            Printer.PrintInfoSection("Extracting required files from the game");
            // Process each mod PAK file
            foreach (var pakFile in modPakFiles)
            {
                _logger.LogInformation("Processing mod PAK: {PakFile}", Path.GetFileName(pakFile));

                // Extract modded files
                await _pakManagerMods.ExtractFromModPak(pakFile, modsPath);

                // For each extracted file, extract its vanilla counterpart
                var extractedFiles = Directory.GetFiles(modsPath, "*.*", SearchOption.AllDirectories);
                foreach (var modFile in extractedFiles)
                {
                    var relativePath = Path.GetRelativePath(modsPath, modFile);
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
            string reportPath = await _diffService.GenerateDiffReport(vanillaPath, modsPath, "");

            Printer.PrintInfoSection($"Diff report generated: {reportPath}");

            // Clean up if configured
            if (_config.Options.CleanWorkDirectory)
            {
                Directory.Delete(vanillaPath, true);
                Directory.Delete(modsPath, true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during mod extraction and comparison");
            throw;
        }
    }
}