using DiffPlex.DiffBuilder.Model;
using S2ZonaConfigurator.Models;

namespace S2ZonaConfigurator.Services.DiffService.Formatters;

public class UnifiedDiffFormatter : BaseFormatter
{
    public override string FileExtension => ".diff";

    private class HunkInfo
    {
        public int OldLineNumber { get; set; }
        public int NewLineNumber { get; set; }
        public int OldCount { get; set; }
        public int NewCount { get; set; }
        public int HunkNumber { get; set; }
        public long HeaderPosition { get; set; }
        public string HeaderLine => $"@@ -{OldStart},{OldCount} +{NewStart},{NewCount} @@ Hunk #{HunkNumber}";

        public int OldStart { get; set; }
        public int NewStart { get; set; }
    }

    public override async Task FormatDiff(StreamWriter writer, SideBySideDiffModel diffModel, string relativePath, DiffConfig config)
    {
        await writer.WriteLineAsync($"--- a/{relativePath}");
        await writer.WriteLineAsync($"+++ b/{relativePath}");

        var lines = GetLinesWithContext(diffModel, config.ContextLines);
        var hunk = new HunkInfo
        {
            HunkNumber = 0,
            OldLineNumber = 1,
            NewLineNumber = 1
        };

        // Group lines into hunks
        var currentHunkLines = new List<LineInfo>();

        for (var i = 0; i < lines.Count; i++)
        {
            var lineInfo = lines[i];

            if (lineInfo.OldLine == null && lineInfo.NewLine == null)
            {
                if (currentHunkLines.Count > 0)
                {
                    hunk.HunkNumber++;
                    await WriteHunk(writer, currentHunkLines, hunk);
                    currentHunkLines.Clear();
                }
                continue;
            }

            currentHunkLines.Add(lineInfo);
        }

        // Write last hunk if any
        if (currentHunkLines.Count > 0)
        {
            hunk.HunkNumber++;
            await WriteHunk(writer, currentHunkLines, hunk);
        }

        await writer.WriteLineAsync();
    }

    private static async Task WriteHunk(
        StreamWriter writer,
        List<LineInfo> hunkLines,
        HunkInfo hunk)
    {
        // Calculate hunk start positions
        hunk.OldStart = hunkLines.First().OldLineNumber ?? hunk.OldLineNumber;
        hunk.NewStart = hunkLines.First().NewLineNumber ?? hunk.NewLineNumber;
        hunk.HeaderPosition = writer.BaseStream.Position;

        // Write placeholder header that will be updated later
        await writer.WriteLineAsync(hunk.HeaderLine);

        // Track the maximum width needed for line numbers to align output
        int maxLineNumWidth = hunkLines.Max(l => Math.Max(
            l.OldLineNumber?.ToString().Length ?? 0,
            l.NewLineNumber?.ToString().Length ?? 0));

        foreach (var lineInfo in hunkLines)
        {
            if (lineInfo.OldLine?.Type == ChangeType.Deleted || lineInfo.OldLine?.Type == ChangeType.Modified)
            {
                string lineNum = lineInfo.OldLineNumber?.ToString().PadLeft(maxLineNumWidth) ?? new string(' ', maxLineNumWidth);
                await writer.WriteLineAsync($"-[{lineNum}] {lineInfo.OldLine.Text}");
                hunk.OldLineNumber++;
                hunk.OldCount++;
            }
            if (lineInfo.NewLine?.Type == ChangeType.Inserted || lineInfo.NewLine?.Type == ChangeType.Modified)
            {
                string lineNum = lineInfo.NewLineNumber?.ToString().PadLeft(maxLineNumWidth) ?? new string(' ', maxLineNumWidth);
                await writer.WriteLineAsync($"+[{lineNum}] {lineInfo.NewLine.Text}");
                hunk.NewLineNumber++;
                hunk.NewCount++;
            }
            else if (lineInfo.OldLine?.Type == ChangeType.Unchanged)
            {
                string lineNum = lineInfo.OldLineNumber?.ToString().PadLeft(maxLineNumWidth) ?? new string(' ', maxLineNumWidth);
                await writer.WriteLineAsync($" [{lineNum}] {lineInfo.OldLine.Text}");
                hunk.OldLineNumber++;
                hunk.NewLineNumber++;
                hunk.OldCount++;
                hunk.NewCount++;
            }
        }

        // Update hunk header with final counts
        var currentPosition = writer.BaseStream.Position;
        writer.BaseStream.Position = hunk.HeaderPosition;
        await writer.WriteLineAsync(hunk.HeaderLine);
        writer.BaseStream.Position = currentPosition;
    }
}