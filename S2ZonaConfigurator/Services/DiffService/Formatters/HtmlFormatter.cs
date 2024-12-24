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
            .line-number { color: #999; margin-right: 10px; }
        ");
        await writer.WriteLineAsync("</style>");
        await writer.WriteLineAsync("</head>");
        await writer.WriteLineAsync("<body>");

        await writer.WriteLineAsync($"<h2>📄 {WebUtility.HtmlEncode(relativePath)}</h2>");
        await writer.WriteLineAsync("<div class='diff'>");

        var lines = GetLinesWithContext(diffModel, config.ContextLines);
        var lineNumber = 1;

        foreach (var (oldLine, newLine) in lines)
        {
            if (oldLine == null && newLine == null)
            {
                await writer.WriteLineAsync("<div class='diff-separator'>...</div>");
                continue;
            }

            switch (oldLine?.Type)
            {
                case ChangeType.Deleted:
                    await writer.WriteLineAsync($"<div class='diff-deleted'><span class='line-number'>{lineNumber++}</span>- {WebUtility.HtmlEncode(oldLine.Text)}</div>");
                    break;
                case ChangeType.Modified:
                    await writer.WriteLineAsync($"<div class='diff-modified'><span class='line-number'>{lineNumber++}</span>- {WebUtility.HtmlEncode(oldLine.Text)}</div>");
                    break;
            }

            switch (newLine?.Type)
            {
                case ChangeType.Inserted:
                    await writer.WriteLineAsync($"<div class='diff-inserted'><span class='line-number'>{lineNumber++}</span>+ {WebUtility.HtmlEncode(newLine.Text)}</div>");
                    break;
                case ChangeType.Modified:
                    await writer.WriteLineAsync($"<div class='diff-modified'><span class='line-number'>{lineNumber++}</span>+ {WebUtility.HtmlEncode(newLine.Text)}</div>");
                    break;
                case ChangeType.Unchanged:
                    await writer.WriteLineAsync($"<div><span class='line-number'>{lineNumber++}</span>  {WebUtility.HtmlEncode(newLine.Text)}</div>");
                    break;
            }
        }

        await writer.WriteLineAsync("</div>");
        await writer.WriteLineAsync("</body>");
        await writer.WriteLineAsync("</html>");
    }
}