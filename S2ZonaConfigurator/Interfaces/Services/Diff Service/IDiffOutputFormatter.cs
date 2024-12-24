using DiffPlex.DiffBuilder.Model;
using S2ZonaConfigurator.Models;

namespace S2ZonaConfigurator.Interfaces.Services;
public interface IDiffOutputFormatter
{
    Task FormatDiff(StreamWriter writer, SideBySideDiffModel diffModel, string relativePath, DiffConfig config);
    string FileExtension { get; }
}