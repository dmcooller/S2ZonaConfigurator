using DiffPlex.DiffBuilder.Model;
using S2ZonaConfigurator.Models;

namespace S2ZonaConfigurator.Services.DiffService.Formatters;
public class SideBySideMarkdownFormatter : BaseFormatter
{
    public override string FileExtension => ".md";

    public override async Task FormatDiff(StreamWriter writer, SideBySideDiffModel diffModel, string relativePath, DiffConfig config)
    {
        await writer.WriteLineAsync($"## 📄 {relativePath}");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("| Line (Old) | Old | Line (New) | New |");
        await writer.WriteLineAsync("|------------|-----|------------|-----|");

        var lines = GetLinesWithContext(diffModel, config.ContextLines);

        foreach (var lineInfo in lines)
        {
            if (lineInfo.OldLine == null && lineInfo.NewLine == null)
            {
                await writer.WriteLineAsync("|...|...|...|...|");
                continue;
            }

            string oldText = lineInfo.OldLine?.Text ?? "";
            string newText = lineInfo.NewLine?.Text ?? "";
            string oldLineNum = lineInfo.OldLineNumber?.ToString() ?? "";
            string newLineNum = lineInfo.NewLineNumber?.ToString() ?? "";

            // Escape pipe characters in markdown table
            oldText = oldText.Replace("|", "\\|");
            newText = newText.Replace("|", "\\|");

            if (lineInfo.OldLine?.Type == ChangeType.Modified || lineInfo.OldLine?.Type == ChangeType.Deleted)
                oldText = $"~~{oldText}~~";
            if (lineInfo.NewLine?.Type == ChangeType.Modified || lineInfo.NewLine?.Type == ChangeType.Inserted)
                newText = $"**{newText}**";

            await writer.WriteLineAsync($"| {oldLineNum} | {oldText} | {newLineNum} | {newText} |");
        }

        await writer.WriteLineAsync();
    }
}