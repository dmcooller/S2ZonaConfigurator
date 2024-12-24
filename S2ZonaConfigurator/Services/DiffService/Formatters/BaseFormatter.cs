using DiffPlex.DiffBuilder.Model;
using S2ZonaConfigurator.Interfaces.Services;
using S2ZonaConfigurator.Models;

namespace S2ZonaConfigurator.Services.DiffService.Formatters;

public record LineInfo(
    DiffPiece? OldLine,
    DiffPiece? NewLine,
    int? OldLineNumber,
    int? NewLineNumber
);

public abstract class BaseFormatter : IDiffOutputFormatter
{
    public abstract string FileExtension { get; }

    public abstract Task FormatDiff(StreamWriter writer, SideBySideDiffModel diffModel, string relativePath, DiffConfig config);

    protected static bool IsChangedLine(DiffPiece? line) =>
        line?.Type == ChangeType.Deleted ||
        line?.Type == ChangeType.Inserted ||
        line?.Type == ChangeType.Modified;

    protected static List<LineInfo> GetLinesWithContext(
        SideBySideDiffModel diffModel, int contextLines)
    {
        // If contextLines is -1, return all lines without separators
        if (contextLines == -1)
        {
            return Enumerable.Range(0, Math.Max(diffModel.OldText.Lines.Count, diffModel.NewText.Lines.Count))
                .Select(i => new LineInfo(
                    i < diffModel.OldText.Lines.Count ? diffModel.OldText.Lines[i] : null,
                    i < diffModel.NewText.Lines.Count ? diffModel.NewText.Lines[i] : null,
                    i < diffModel.OldText.Lines.Count ? i + 1 : null,
                    i < diffModel.NewText.Lines.Count ? i + 1 : null
                ))
                .ToList();
        }

        var result = new List<LineInfo>();
        var maxLines = Math.Max(diffModel.OldText.Lines.Count, diffModel.NewText.Lines.Count);
        var contextBuffer = new List<LineInfo>();
        var isInChange = false;
        var linesAfterChange = 0;

        for (int i = 0; i < maxLines; i++)
        {
            var oldLine = i < diffModel.OldText.Lines.Count ? diffModel.OldText.Lines[i] : null;
            var newLine = i < diffModel.NewText.Lines.Count ? diffModel.NewText.Lines[i] : null;
            var lineInfo = new LineInfo(oldLine, newLine, i + 1, i + 1);

            if (IsChangedLine(oldLine) || IsChangedLine(newLine))
            {
                if (!isInChange)
                {
                    // Add context before change
                    var contextStart = Math.Max(0, contextBuffer.Count - contextLines);
                    if (result.Count > 0 && contextStart > 0)
                        result.Add(new LineInfo(null, null, null, null)); // Separator
                    result.AddRange(contextBuffer.Skip(contextStart));
                    contextBuffer.Clear();
                }

                isInChange = true;
                linesAfterChange = contextLines;
                result.Add(lineInfo);
            }
            else
            {
                if (linesAfterChange > 0)
                {
                    result.Add(lineInfo);
                    linesAfterChange--;
                    if (linesAfterChange == 0)
                    {
                        isInChange = false;
                        contextBuffer.Clear();
                    }
                }
                else
                {
                    contextBuffer.Add(lineInfo);
                    if (contextBuffer.Count > contextLines)
                        contextBuffer.RemoveAt(0);
                }
            }
        }

        return result;
    }
}