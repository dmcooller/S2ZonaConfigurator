using S2ZonaConfigurator.Models;

namespace S2ZonaConfigurator.Interfaces.Services;
public interface IConfigParser
{
    void ApplyAction(ConfigAction action);
    void SaveFile();
}