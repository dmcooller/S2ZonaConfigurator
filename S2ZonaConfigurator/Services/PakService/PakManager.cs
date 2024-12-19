using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetPak;
using S2ZonaConfigurator.Helpers;
using S2ZonaConfigurator.Interfaces.Services;
using S2ZonaConfigurator.Models;

namespace S2ZonaConfigurator.Services.PakService;
public class PakManager : IPakManager
{
    private readonly ILogger<PakManager> _logger;
    private readonly AppConfig _config;
    private DefaultFileProvider? _provider;

    private readonly string _vanillaDir;
    private readonly string _modifiedDir;

    public PakManager(ILogger<PakManager> logger, IOptions<AppConfig> config)
    {
        _logger = logger;
        _config = config.Value;
        _vanillaDir = _config.Paths.VanillaDirectory;
        _modifiedDir = _config.Paths.ModifiedDirectory;


        InitializeDirectories();
    }

    private void InitializeDirectories()
    {
        CleanWorkDirectory();
        Directory.CreateDirectory(_config.Paths.WorkDirectory);

        var vanillaPath = Path.Combine(_config.Paths.WorkDirectory, _vanillaDir);
        var modsPath = Path.Combine(_config.Paths.WorkDirectory, _modifiedDir);

        Directory.CreateDirectory(vanillaPath);
        Directory.CreateDirectory(modsPath);
    }

    public void Initialize()
    {
        try
        {
            InitializeDirectories();

            var packsPath = Path.Combine(_config.Game.GamePath, _config.Paths.PaksPath);
            if (!Directory.Exists(packsPath))
                throw new DirectoryNotFoundException($"Packs directory not found at {packsPath}");

            _provider = new DefaultFileProvider(
                directory: packsPath,
                searchOption: SearchOption.TopDirectoryOnly,
                isCaseInsensitive: false,
                versions: new VersionContainer(EGame.GAME_UE5_1));
            _provider.Initialize();

            // Set up AES key
            var key = new FAesKey(_config.Game.AesKey);
            _provider.SubmitKey(new FGuid(), key);

            Printer.PrintInfoSection("PAK provider initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize PAK provider");
            throw;
        }
    }

    private void TestAes()
    {
        if (_provider!.MountedVfs.Count == 0)
            throw new Exception("AES key is invalid");
    }

    public async Task ExtractConfigFile(string configPath)
    {
        if (_provider == null)
            throw new InvalidOperationException("PAK provider not initialized");

        TestAes();

        try
        {
            configPath = StringHelper.NormalizeConfigPath(configPath);

            // Get game file
            var gameFile = _provider.Files.FirstOrDefault(f => f.Key.EndsWith(configPath));
            if (gameFile.Value == null)
                throw new FileNotFoundException($"Config file not found: {configPath}");

            // Extract the file
            var extractPath = Path.Combine(_config.Paths.WorkDirectory, _vanillaDir, configPath);
            Directory.CreateDirectory(Path.GetDirectoryName(extractPath)!);

            var data = await _provider.SaveAssetAsync(gameFile.Key);
            if (data != null)
            {
                await File.WriteAllBytesAsync(extractPath, data);
                _logger.LogDebug("Successfully extracted config file to {Path}", extractPath);
            }
            else
            {
                _logger.LogWarning("Failed to extract data from {Path}", configPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract config file: {Path}", configPath);
            throw;
        }
    }

    public void CopyExtractedFilesToMods()
    {
        var vanillaPath = Path.Combine(_config.Paths.WorkDirectory, _vanillaDir);
        var modsPath = Path.Combine(_config.Paths.WorkDirectory, _modifiedDir);
        // Copy all everything from the vanilla directory to the mods directory
        foreach (var file in Directory.GetFiles(vanillaPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(vanillaPath, file);
            var destPath = Path.Combine(modsPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.Copy(file, destPath, true);
        }
        _logger.LogDebug("Copied all extracted files to the mods directory");
    }

    public async Task CreateModPak()
    {
        var fileName = new FString(_config.Paths.OutputPakName);
        var mountPoint = new FString("../../../");
        var modsPath = Path.Combine(_config.Paths.WorkDirectory, _modifiedDir);
        if (!Directory.Exists(modsPath))
            throw new DirectoryNotFoundException($"Mods directory not found at {modsPath}");

        string modsDestPath = Path.Combine(_config.Game.GamePath, _config.Paths.PaksPath, "~mods");
        Directory.CreateDirectory(modsDestPath);
        string pakFilePath = Path.Combine(modsDestPath, fileName);

        try
        {
            // Files to add to the PAK 
            var files = Directory.GetFiles(modsPath, "*", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                Printer.PrintInfoSection("No files to add to the PAK. PAK creation skipped");
                return;
            }

            using PakFile pakFile = PakFile.Create(fileName, mountPoint, CompressionMethod.Zlib);
            foreach (var file in files)
            {
                // Exclude the mods directory from the path
                var fileEntryPath = new FString(StringHelper.NormalizeConfigPath(file.Replace(modsPath, "")));
                // Read the file content
                var content = await File.ReadAllBytesAsync(file);
                pakFile.AddEntry(fileEntryPath, content);
            }

            pakFile.Save(pakFilePath);
            Printer.PrintPakStatus(pakFilePath, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create mod PAK");
            Printer.PrintPakStatus(pakFilePath, false);
            throw;
        }
        finally
        {
            if (_config.Options.CleanWorkDirectory)
                CleanWorkDirectory();
        }
    }

    public string GetOutputPakPath()
    {
        string modsDestPath = Path.Combine(_config.Game.GamePath, _config.Paths.PaksPath, "~mods");
        return Path.Combine(modsDestPath, _config.Paths.OutputPakName);
    }

    private void CleanWorkDirectory()
    {
        var vanillaPath = Path.Combine(_config.Paths.WorkDirectory, _vanillaDir);
        var modsPath = Path.Combine(_config.Paths.WorkDirectory, _modifiedDir);
        if (Directory.Exists(vanillaPath))
            Directory.Delete(vanillaPath, true);
        if (Directory.Exists(modsPath))
            Directory.Delete(modsPath, true);
    }
}