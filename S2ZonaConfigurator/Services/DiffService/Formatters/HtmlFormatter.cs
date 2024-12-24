using DiffPlex.DiffBuilder.Model;
using S2ZonaConfigurator.Models;
using System.Net;

namespace S2ZonaConfigurator.Services.DiffService.Formatters;

public class HtmlFormatter : BaseFormatter
{
    public override string FileExtension => ".html";

    public override async Task FormatDiff(StreamWriter writer, SideBySideDiffModel diffModel, string relativePath, DiffConfig config)
    {
        await writer.WriteLineAsync("<!DOCTYPE html>");
        await writer.WriteLineAsync("<html>");
        await writer.WriteLineAsync("<head>");
        await writer.WriteLineAsync("<style>");
        await writer.WriteLineAsync(@"
            .diff { font-family: monospace; white-space: pre; }
            .diff-header { font-weight: bold; margin: 10px 0; }
            .diff-deleted { background-color: #fdd; color: #900; }
            .diff-inserted { background-color: #dfd; color: #090; }
            .diff-modified { background-color: #ffd; }
            .diff-separator { color: #999; background: #f0f0f0; text-align: center; }
            .line-number { color: #666; margin-right: 10px; user-select: none; }
            .line-number-old { color: #900; }
            .line-number-new { color: #090; }
            .line-numbers { 
                display: inline-block;
                text-align: right;
                padding-right: 10px;
                margin-right: 10px;
                border-right: 1px solid #ddd;
                min-width: 80px;
            }
        ");
        await writer.WriteLineAsync("</style>");
        await writer.WriteLineAsync("</head>");
        await writer.WriteLineAsync("<body>");

        await writer.WriteLineAsync($"<h2>📄 {WebUtility.HtmlEncode(relativePath)}</h2>");
        await writer.WriteLineAsync("<div class='diff'>");

        var lines = GetLinesWithContext(diffModel, config.ContextLines);

        foreach (var lineInfo in lines)
        {
            if (lineInfo.OldLine == null && lineInfo.NewLine == null)
            {
                await writer.WriteLineAsync("<div class='diff-separator'>...</div>");
                continue;
            }

            string lineNumbers = FormatLineNumbers(lineInfo.OldLineNumber, lineInfo.NewLineNumber);

            switch (lineInfo.OldLine?.Type)
            {
                case ChangeType.Deleted:
                    await writer.WriteLineAsync($"<div class='diff-deleted'>{lineNumbers}- {WebUtility.HtmlEncode(lineInfo.OldLine.Text)}</div>");
                    break;
                case ChangeType.Modified:
                    await writer.WriteLineAsync($"<div class='diff-modified'>{lineNumbers}- {WebUtility.HtmlEncode(lineInfo.OldLine.Text)}</div>");
                    break;
            }

            switch (lineInfo.NewLine?.Type)
            {
                case ChangeType.Inserted:
                    await writer.WriteLineAsync($"<div class='diff-inserted'>{lineNumbers}+ {WebUtility.HtmlEncode(lineInfo.NewLine.Text)}</div>");
                    break;
                case ChangeType.Modified:
                    await writer.WriteLineAsync($"<div class='diff-modified'>{lineNumbers}+ {WebUtility.HtmlEncode(lineInfo.NewLine.Text)}</div>");
                    break;
                case ChangeType.Unchanged:
                    await writer.WriteLineAsync($"<div>{lineNumbers}  {WebUtility.HtmlEncode(lineInfo.NewLine.Text)}</div>");
                    break;
            }
        }

        await writer.WriteLineAsync("</div>");
        await writer.WriteLineAsync("</body>");
        await writer.WriteLineAsync("</html>");
    }

    private static string FormatLineNumbers(int? oldLineNumber, int? newLineNumber)
    {
        var oldNum = oldLineNumber?.ToString() ?? "";
        var newNum = newLineNumber?.ToString() ?? "";
        return $"<span class='line-numbers'><span class='line-number-old'>{oldNum}</span>→<span class='line-number-new'>{newNum}</span></span>";
    }
}