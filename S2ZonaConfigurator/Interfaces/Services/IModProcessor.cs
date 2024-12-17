using S2ZonaConfigurator.Models;

namespace S2ZonaConfigurator.Interfaces.Services;
public interface IModProcessor
{
    void ProcessMod(string modFile, ModData modData);
    Dictionary<string, ModData> ParseModFiles(string ModsDirectory);
    void PrintFinalSummary();
    void GenerateChangelog(string pakFilePath, Dictionary<string, ModData> modDataMap);
}