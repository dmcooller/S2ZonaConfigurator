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
        await writer.WriteLineAsync("| Old | New |");
        await writer.WriteLineAsync("|-----|-----|");

        var lines = GetLinesWithContext(diffModel, config.ContextLines);

        foreach (var (oldLine, newLine) in lines)
        {
            if (oldLine == null && newLine == null)
            {
                await writer.WriteLineAsync("|...|...|");
                continue;
            }

            string oldText = oldLine?.Text ?? "";
            string newText = newLine?.Text ?? "";

            // Escape pipe characters in markdown table
            oldText = oldText.Replace("|", "\\|");
            newText = newText.Replace("|", "\\|");

            if (oldLine?.Type == ChangeType.Modified || oldLine?.Type == ChangeType.Deleted)
                oldText = $"~~{oldText}~~";
            if (newLine?.Type == ChangeType.Modified || newLine?.Type == ChangeType.Inserted)
                newText = $"**{newText}**";

            await writer.WriteLineAsync($"| {oldText} | {newText} |");
        }

        await writer.WriteLineAsync();
    }
}