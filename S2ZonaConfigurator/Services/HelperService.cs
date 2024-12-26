using Microsoft.Extensions.Options;
using S2ZonaConfigurator.Models;

namespace S2ZonaConfigurator.Services;
public class HelperService(IOptions<AppConfig> config)
{
    private readonly AppConfig _config = config.Value;

    public string GetGamePaksPath()
        => Path.Combine(_config.Game.GamePath, _config.Paths.PaksPath);

    public string GetGameModsPath()
        => Path.Combine(GetGamePaksPath(), "~mods");

    public string GetWorkDirectoryPath()
        => _config.Paths.WorkDirectory;

    public string GetWorkVanillaPath()
        => Path.Combine(GetWorkDirectoryPath(), _config.Paths.VanillaDirectory);

    public string GetWorkModsPath()
        => Path.Combine(GetWorkDirectoryPath(), _config.Paths.ModifiedDirectory);
    public string GetOutputPakName()
        => _config.Paths.OutputPakName;

    public string GetOutputPakPath()
        => Path.Combine(GetGameModsPath(), GetOutputPakName());

    public string GetJsonModsPath()
        => _config.Paths.ModsDirectory;

    public string GetPakModsPath()
        => _config.Paths.PakModsDirectory;


    public void CleanWorkDirectory()
    {
        string workVanillaPath = GetWorkVanillaPath();
        string workModsPath = GetWorkModsPath();

        if (Directory.Exists(workVanillaPath))
            Directory.Delete(workVanillaPath, true);
        if (Directory.Exists(workModsPath))
            Directory.Delete(workModsPath, true);
    }
}
