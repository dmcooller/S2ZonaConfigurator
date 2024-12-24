namespace S2ZonaConfigurator.Helpers;
public static class StringHelper
{
    public static string? NormalizeConfigPath(string? configPath)
    {
        return configPath?.Replace('\\', '/').TrimStart('/');
    }
}
