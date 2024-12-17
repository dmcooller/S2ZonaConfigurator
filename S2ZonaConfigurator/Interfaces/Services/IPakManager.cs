namespace S2ZonaConfigurator.Interfaces.Services;

public interface IPakManager
{
    void Initialize();
    void CopyExtractedFilesToMods();
    Task ExtractConfigFile(string configPath);
    Task CreateModPak();
    string GetOutputPakPath();
}