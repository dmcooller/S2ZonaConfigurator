using DiffPlex.DiffBuilder.Model;
using S2ZonaConfigurator.Models;

namespace S2ZonaConfigurator.Services.DiffService.Formatters;

public class UnifiedDiffFormatter : BaseFormatter
{
    public override string FileExtension => ".diff";

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
        var currentHunkLines = new List<(DiffPiece? OldLine, DiffPiece? NewLine)>();

        for (var i = 0; i < lines.Count; i++)
        {
            var (oldLine, newLine) = lines[i];

            if (oldLine == null && newLine == null)
            {
                if (currentHunkLines.Count > 0)
                {
                    hunk.HunkNumber++;
                    await WriteHunk(writer, currentHunkLines, hunk);
                    currentHunkLines.Clear();
                }
                continue;
            }

            currentHunkLines.Add((oldLine, newLine));
        }

        // Write last hunk if any
        if (currentHunkLines.Count > 0)
        {
            hunk.HunkNumber++;
            await WriteHunk(writer, currentHunkLines, hunk);
        }

        await writer.WriteLineAsync();
    }

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

    private static async Task WriteHunk(
        StreamWriter writer,
        List<(DiffPiece? OldLine, DiffPiece? NewLine)> hunkLines,
        HunkInfo hunk)
    {
        hunk.OldStart = hunk.OldLineNumber;
        hunk.NewStart = hunk.NewLineNumber;
        hunk.HeaderPosition = writer.BaseStream.Position;

        // Write placeholder header that will be updated later
        await writer.WriteLineAsync(hunk.HeaderLine);

        foreach (var (oldLine, newLine) in hunkLines)
        {
            if (oldLine?.Type == ChangeType.Deleted || oldLine?.Type == ChangeType.Modified)
            {
                await writer.WriteLineAsync($"-{oldLine.Text}");
                hunk.OldLineNumber++;
                hunk.OldCount++;
            }
            if (newLine?.Type == ChangeType.Inserted || newLine?.Type == ChangeType.Modified)
            {
                await writer.WriteLineAsync($"+{newLine.Text}");
                hunk.NewLineNumber++;
                hunk.NewCount++;
            }
            else if (oldLine?.Type == ChangeType.Unchanged)
            {
                await writer.WriteLineAsync($" {oldLine.Text}");
                hunk.OldLineNumber++;
                hunk.NewLineNumber++;
                hunk.OldCount++;
                hunk.NewCount++;
            }
        }

        // Update hunk header
        var currentPosition = writer.BaseStream.Position;
        writer.BaseStream.Position = hunk.HeaderPosition;
        await writer.WriteLineAsync(hunk.HeaderLine);
        writer.BaseStream.Position = currentPosition;
    }
}