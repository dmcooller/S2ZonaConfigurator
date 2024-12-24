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

        foreach (var lineInfo in lines)
        {
            if (lineInfo.OldLine == null && lineInfo.NewLine == null)
            {
                await writer.WriteLineAsync("...");
                continue;
            }

            string lineNumber = "";
            if (lineInfo.OldLineNumber.HasValue || lineInfo.NewLineNumber.HasValue)
            {
                lineNumber = $"[{lineInfo.OldLineNumber ?? 0}→{lineInfo.NewLineNumber ?? 0}] ";
            }

            if (lineInfo.OldLine?.Type == ChangeType.Deleted || lineInfo.OldLine?.Type == ChangeType.Modified)
                await writer.WriteLineAsync($"- {lineNumber}{lineInfo.OldLine.Text}");
            if (lineInfo.NewLine?.Type == ChangeType.Inserted || lineInfo.NewLine?.Type == ChangeType.Modified)
                await writer.WriteLineAsync($"+ {lineNumber}{lineInfo.NewLine.Text}");
            else if (lineInfo.OldLine?.Type == ChangeType.Unchanged)
                await writer.WriteLineAsync($"  {lineNumber}{lineInfo.OldLine.Text}");
        }

        await writer.WriteLineAsync("```");
        await writer.WriteLineAsync();
    }
}