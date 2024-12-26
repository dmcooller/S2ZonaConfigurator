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

    /// <summary>
    /// Initializes the PAK provider with the default PAK directory path (from the config)
    /// </summary>
    public void Initialize()
        => Initialize(Path.Combine(_config.Game.GamePath, _config.Paths.PaksPath));
    
    public void Initialize(string path)
    {
        try
        {
            InitializeDirectories();
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"Packs directory not found at {path}");

            _provider = new DefaultFileProvider(
                directory: path,
                searchOption: SearchOption.TopDirectoryOnly,
                isCaseInsensitive: false,
                versions: new VersionContainer(EGame.GAME_UE5_1));
            _provider.Initialize();

            // Set up AES key
            var key = new FAesKey(_config.Game.AesKey);
            _provider.SubmitKey(new FGuid(), key);

            Printer.PrintInfoSection($"PAK provider for `{path}` initialized successfully");
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
            throw new Exception("Failed to mount PAK files. AES may be incorrect");
    }

    public async Task ExtractFile(string filePath)
    {
        if (_provider == null)
            throw new InvalidOperationException("PAK provider not initialized");

        TestAes();

        try
        {
            filePath = StringHelper.NormalizeConfigPath(filePath);

            // Get game file
            var gameFile = _provider.Files.FirstOrDefault(f => f.Key.EndsWith(filePath));
            if (gameFile.Value == null)
                throw new FileNotFoundException($"File not found: {filePath}");

            // Extract the file
            var extractPath = Path.Combine(_config.Paths.WorkDirectory, _vanillaDir, filePath);
            Directory.CreateDirectory(Path.GetDirectoryName(extractPath)!);

            var data = await _provider.SaveAssetAsync(gameFile.Key);
            if (data != null)
            {
                await File.WriteAllBytesAsync(extractPath, data);
                _logger.LogDebug("Successfully extracted config file to {Path}", extractPath);
            }
            else
            {
                _logger.LogWarning("Failed to extract data from {Path}", filePath);
            }
        }
        catch (Exception ex)
        {
            if (ex is FileNotFoundException)
                _logger.LogInformation("File not found for: {Path}", filePath);
            else
                _logger.LogError(ex, "Failed to extract config file: {Path}", filePath);
            throw;
        }
    }

    public async Task ExtractFromModPak(string pakFilePath, string outputPath)
    {
        if (_provider == null)
            throw new InvalidOperationException("PAK provider not initialized");

        try
        {
            // Get all files in the PAK
            foreach (var file in _provider.Files)
            {
                try
                {
                    var extractPath = Path.Combine(outputPath, file.Key);
                    Directory.CreateDirectory(Path.GetDirectoryName(extractPath)!);

                    var data = await _provider.SaveAssetAsync(file.Key);
                    if (data != null)
                    {
                        await File.WriteAllBytesAsync(extractPath, data);
                        _logger.LogDebug("Successfully extracted file: {Path}", file.Key);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to extract data from {Path}", file.Key);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract file: {Path}", file.Key);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract files from mod PAK: {Path}", pakFilePath);
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
                var fileEntryPath = new FString(StringHelper.NormalizeConfigPath(Path.GetRelativePath(modsPath, file)));
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