using DiffPlex.DiffBuilder.Model;
using S2ZonaConfigurator.Interfaces.Services;
using S2ZonaConfigurator.Models;

namespace S2ZonaConfigurator.Services.DiffService.Formatters;
public abstract class BaseFormatter : IDiffOutputFormatter
{
    public abstract string FileExtension { get; }

    public abstract Task FormatDiff(StreamWriter writer, SideBySideDiffModel diffModel, string relativePath, DiffConfig config);

    protected static bool IsChangedLine(DiffPiece? line) =>
        line?.Type == ChangeType.Deleted ||
        line?.Type == ChangeType.Inserted ||
        line?.Type == ChangeType.Modified;

    protected static List<(DiffPiece? OldLine, DiffPiece? NewLine)> GetLinesWithContext(
        SideBySideDiffModel diffModel, int contextLines)
    {
        var result = new List<(DiffPiece? OldLine, DiffPiece? NewLine)>();
        var maxLines = Math.Max(diffModel.OldText.Lines.Count, diffModel.NewText.Lines.Count);
        var contextBuffer = new List<(DiffPiece? OldLine, DiffPiece? NewLine)>();
        var isInChange = false;
        var linesAfterChange = 0;

        for (int i = 0; i < maxLines; i++)
        {
            var oldLine = i < diffModel.OldText.Lines.Count ? diffModel.OldText.Lines[i] : null;
            var newLine = i < diffModel.NewText.Lines.Count ? diffModel.NewText.Lines[i] : null;

            if (IsChangedLine(oldLine) || IsChangedLine(newLine))
            {
                if (!isInChange)
                {
                    // Add context before change
                    var contextStart = Math.Max(0, contextBuffer.Count - contextLines);
                    if (result.Count > 0 && contextStart > 0)
                        result.Add((null, null)); // Add separator
                    result.AddRange(contextBuffer.Skip(contextStart));
                    contextBuffer.Clear();
                }

                isInChange = true;
                linesAfterChange = contextLines;
                result.Add((oldLine, newLine));
            }
            else
            {
                if (linesAfterChange > 0)
                {
                    result.Add((oldLine, newLine));
                    linesAfterChange--;
                    if (linesAfterChange == 0)
                    {
                        isInChange = false;
                        contextBuffer.Clear();
                    }
                }
                else
                {
                    contextBuffer.Add((oldLine, newLine));
                    if (contextBuffer.Count > contextLines)
                        contextBuffer.RemoveAt(0);
                }
            }
        }

        return result;
    }
}