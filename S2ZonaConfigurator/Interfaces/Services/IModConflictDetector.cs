using S2ZonaConfigurator.Models;

namespace S2ZonaConfigurator.Interfaces.Services;
public interface IModConflictDetector
{
    List<ModConflict> DetectConflicts(Dictionary<string, ModData> modDataMap);
}