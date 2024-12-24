using DiffPlex.DiffBuilder.Model;
using S2ZonaConfigurator.Models;

namespace S2ZonaConfigurator.Services.DiffService.Formatters;
public class GitHubMarkdownFormatter : BaseFormatter
{
    public override string FileExtension => ".md";

    public override async Task FormatDiff(StreamWriter writer, SideBySideDiffModel diffModel, string relativePath, DiffConfig config)
    {
        await writer.WriteLineAsync($"## 📄 {relativePath}");
        await writer.WriteLineAsync("```diff");

        var lines = GetLinesWithContext(diffModel, config.ContextLines);

        foreach (var (oldLine, newLine) in lines)
        {
            if (oldLine == null && newLine == null)
            {
                await writer.WriteLineAsync("...");
                continue;
            }

            if (oldLine?.Type == ChangeType.Deleted || oldLine?.Type == ChangeType.Modified)
                await writer.WriteLineAsync($"- {oldLine.Text}");
            if (newLine?.Type == ChangeType.Inserted || newLine?.Type == ChangeType.Modified)
                await writer.WriteLineAsync($"+ {newLine.Text}");
            else if (oldLine?.Type == ChangeType.Unchanged)
                await writer.WriteLineAsync($" {oldLine.Text}");
        }

        await writer.WriteLineAsync("```");
        await writer.WriteLineAsync();
    }
}