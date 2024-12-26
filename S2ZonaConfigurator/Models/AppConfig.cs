using S2ZonaConfigurator.Enums;

namespace S2ZonaConfigurator.Models;

public class AppConfig
{
    public PathsConfig Paths { get; set; } = new();
    public GameConfig Game { get; set; } = new();
    public OptionsConfig Options { get; set; } = new();
    public DiffConfig DiffConfig { get; set; } = new();
}

public class PathsConfig
{
    public string WorkDirectory { get; set; } = "work";
    public string VanillaDirectory { get; set; } = "vanilla";
    public string ModifiedDirectory { get; set; } = "modified";
    public string ModsDirectory { get; set; } = "mods";
    public string PakModsDirectory { get; set; } = "pak_mods";
    public string PaksPath { get; set; } = "Stalker2\\Content\\Paks";
    public string OutputPakName { get; set; } = "ZonaBundle.pak";
}

public class GameConfig
{
    public string GamePath { get; set; } = string.Empty;
    public string AesKey { get; set; } = string.Empty;
}

public class OptionsConfig
{
    public AppMode AppMode { get; set; } = AppMode.Main;
    public bool OutputChangelogFile { get; set; } = false;
    public bool CleanWorkDirectory { get; set; } = true;
    public bool DeleteOldMods { get; set; } = false;
    public bool GenerateDiffReport { get; set; } = false;
    public bool DetectModConflicts { get; set; } = true;
}
