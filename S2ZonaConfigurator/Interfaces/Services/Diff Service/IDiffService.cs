namespace S2ZonaConfigurator.Interfaces.Services;
public interface IDiffService
{
    /// <summary>
    /// Generates a diff report between vanilla and modified files
    /// </summary>
    /// <param name="originalPath">Path to the original files</param>
    /// <param name="modifiedPath">Path to the modified files to compare with</param>
    /// <param name="outputPath">Path where to save the diff report</param>
    /// <returns>Path to the generated diff report</returns>
    Task<string> GenerateDiffReport(string originalPath, string modifiedPath, string outputPath);
}
