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
    private readonly HelperService _helper;
    private DefaultFileProvider? _provider;

    private readonly string _workDir;
    private readonly string _workVanillaPath;
    private readonly string _workModsPath;
    private readonly string _gamePaksPath;
    private readonly string _outputPakName;
    private readonly string _gameModsPath;

    public PakManager(ILogger<PakManager> logger, IOptions<AppConfig> config, HelperService helper)
    {
        _logger = logger;
        _config = config.Value;
        _helper = helper;

        _workDir = helper.GetWorkDirectoryPath();
        _workVanillaPath = helper.GetWorkVanillaPath();
        _workModsPath = helper.GetWorkModsPath();
        _gamePaksPath = helper.GetGamePaksPath();
        _outputPakName = helper.GetOutputPakName();
        _gameModsPath = helper.GetGameModsPath();

        helper.CleanWorkDirectory();

        InitializeDirectories();
    }

    private void InitializeDirectories()
    {
        Directory.CreateDirectory(_workDir);
        Directory.CreateDirectory(_workVanillaPath);
        Directory.CreateDirectory(_workModsPath);
    }

    /// <summary>
    /// Initializes the PAK provider with the default PAK directory path (from the config)
    /// </summary>
    public void Initialize()
        => Initialize(_gamePaksPath);
    
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

        string? filePathN = StringHelper.NormalizeConfigPath(filePath);
        ArgumentNullException.ThrowIfNull(filePathN);

        try
        {
            // Get game file
            var gameFile = _provider.Files.FirstOrDefault(f => f.Key.EndsWith(filePathN));
            if (gameFile.Value == null)
                throw new FileNotFoundException($"File not found: {filePathN}");

            // Extract the file
            var extractPath = Path.Combine(_workVanillaPath, filePathN);
            Directory.CreateDirectory(Path.GetDirectoryName(extractPath)!);

            var data = await _provider.SaveAssetAsync(gameFile.Key);
            if (data != null)
            {
                await File.WriteAllBytesAsync(extractPath, data);
                _logger.LogDebug("Successfully extracted config file to {Path}", extractPath);
            }
            else
            {
                _logger.LogWarning("Failed to extract data from {Path}", filePathN);
            }
        }
        catch (Exception ex)
        {
            if (ex is FileNotFoundException)
                _logger.LogInformation("File not found for: {Path}", filePathN);
            else
                _logger.LogError(ex, "Failed to extract config file: {Path}", filePathN);
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
        // Copy all everything from the vanilla directory to the mods directory
        foreach (var file in Directory.GetFiles(_workVanillaPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(_workVanillaPath, file);
            var destPath = Path.Combine(_workModsPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.Copy(file, destPath, true);
        }
        _logger.LogDebug("Copied all extracted files to the mods directory");
    }

    public async Task CreateModPak()
    {
        var fileName = new FString(_config.Paths.OutputPakName);
        var mountPoint = new FString("../../../");

        if (!Directory.Exists(_workModsPath))
            throw new DirectoryNotFoundException($"Mods directory not found at {_workModsPath}");

        string modsDestPath = Path.Combine(_config.Game.GamePath, _config.Paths.PaksPath, "~mods");
        Directory.CreateDirectory(modsDestPath);
        string pakFilePath = Path.Combine(modsDestPath, fileName);

        try
        {
            // Files to add to the PAK 
            var files = Directory.GetFiles(_workModsPath, "*", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                Printer.PrintInfoSection("No files to add to the PAK. PAK creation skipped");
                return;
            }

            using PakFile pakFile = PakFile.Create(fileName, mountPoint, CompressionMethod.Zlib);
            foreach (var file in files)
            {
                // Exclude the mods directory from the path
                var normalizedPath = StringHelper.NormalizeConfigPath(Path.GetRelativePath(_workModsPath, file)) 
                    ?? throw new InvalidOperationException($"Failed to normalize path: {file}");
                var fileEntryPath = new FString(normalizedPath);
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
                _helper.CleanWorkDirectory();
        }
    }

    /// <summary>
    /// Cleans up old mod files from the mods directory
    /// It removes all files that starts with the OutputPakName
    /// </summary>
    public void DeleteOldMods()
    {
        var files = Directory.GetFiles(_gameModsPath, $"{Path.GetFileNameWithoutExtension(_outputPakName)}*", SearchOption.TopDirectoryOnly);
        foreach (var file in files)
        {
            File.Delete(file);
            _logger.LogDebug("Deleted old mod file: {File}", file);
        }
    }
}