namespace S2ZonaConfigurator.Interfaces.Services;

public interface IPakManager
{
    void Initialize();
    void Initialize(string path);
    void CopyExtractedFilesToMods();
    Task ExtractFile(string filePath);
    Task ExtractFromModPak(string pakFilePath, string outputPath);
    Task CreateModPak();
    string GetOutputPakPath();
}