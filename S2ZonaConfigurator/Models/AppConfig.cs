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
    public bool OutputChangelogFile { get; set; } = false;
    public bool CleanWorkDirectory { get; set; } = true;
    public bool GenerateDiffReport { get; set; } = false;
}
