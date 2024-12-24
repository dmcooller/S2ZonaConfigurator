using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using S2ZonaConfigurator.Enums;
using S2ZonaConfigurator.Interfaces.Services;
using S2ZonaConfigurator.Models;
using S2ZonaConfigurator.Services.DiffService.Formatters;
using System.Text;

namespace S2ZonaConfigurator.Services.DiffService;

public class DiffService : IDiffService
{
    private readonly ILogger<DiffService> _logger;
    private readonly DiffConfig _diffConfig;
    private readonly SideBySideDiffBuilder _diffBuilder;
    private readonly IDiffer _differ;
    private readonly IDictionary<DiffOutputFormat, IDiffOutputFormatter> _formatters;


    public DiffService(ILogger<DiffService> logger, IOptions<AppConfig> config)
    {
        _logger = logger;
        _diffConfig = config.Value.DiffConfig ?? new DiffConfig();
        _differ = new Differ();
        _diffBuilder = new SideBySideDiffBuilder(_differ);

        // Initialize formatters
        _formatters = new Dictionary<DiffOutputFormat, IDiffOutputFormatter>
        {
            [DiffOutputFormat.GitHubMarkdown] = new GitHubMarkdownFormatter(),
            [DiffOutputFormat.Unified] = new UnifiedDiffFormatter(),
            [DiffOutputFormat.SideBySideMarkdown] = new SideBySideMarkdownFormatter(),
            [DiffOutputFormat.Html] = new HtmlFormatter()
        };
    }

    public async Task<string> GenerateDiffReport(string originalPath, string modifiedPath, string outputPath)
    {
        try
        {
            if (!Directory.Exists(originalPath) || !Directory.Exists(modifiedPath))
            {
                throw new DirectoryNotFoundException("Cannot create a diff because original or modified directory not found");
            }

            var formatter = _formatters[_diffConfig.OutputFormat];
            var reportPath = Path.Combine(outputPath, $"diff_report{formatter.FileExtension}");
            await using var writer = new StreamWriter(reportPath, false, Encoding.UTF8);

            await writer.WriteLineAsync("# Modified Files Diff Report");
            await writer.WriteLineAsync($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            await writer.WriteLineAsync();

            // Write configuration summary
            await WriteDiffConfigSummary(writer);

            // Process existing files
            foreach (var originalFile in Directory.EnumerateFiles(originalPath, "*.*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(originalPath, originalFile);

                // Skip files with excluded extensions
                if (_diffConfig.SkipExtensions.Contains(Path.GetExtension(originalFile)))
                {
                    _logger.LogDebug("Skipping file with excluded extension: {Path}", relativePath);
                    continue;
                }

                var modifiedFile = Path.Combine(modifiedPath, relativePath);
                if (!File.Exists(modifiedFile))
                {
                    _logger.LogWarning("Modified file not found: {Path}", relativePath);
                    continue;
                }

                await ProcessFileDiff(originalFile, modifiedFile, relativePath, writer);
            }

            // Check for new files
            foreach (var modifiedFile in Directory.EnumerateFiles(modifiedPath, "*.*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(modifiedPath, modifiedFile);

                // Skip files with excluded extensions
                if (_diffConfig.SkipExtensions.Contains(Path.GetExtension(modifiedFile)))
                    continue;

                var vanillaFile = Path.Combine(originalPath, relativePath);
                if (!File.Exists(vanillaFile))
                {
                    await writer.WriteLineAsync($"## 📄 {relativePath}");
                    await writer.WriteLineAsync("```diff");
                    await writer.WriteLineAsync("+ New file added");
                    await writer.WriteLineAsync("```");
                    await writer.WriteLineAsync();
                }
            }

            return reportPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating diff report");
            throw;
        }
    }

    private async Task WriteDiffConfigSummary(StreamWriter writer)
    {
        await writer.WriteLineAsync("## Diff Configuration");
        await writer.WriteLineAsync("```");
        await writer.WriteLineAsync($"Output Format: {_diffConfig.OutputFormat}");
        await writer.WriteLineAsync($"Max File Size: {_diffConfig.MaxFileSize / 1024 / 1024}MB");
        await writer.WriteLineAsync($"Context Lines: {_diffConfig.ContextLines}");
        await writer.WriteLineAsync($"Skipped Extensions: {string.Join(", ", _diffConfig.SkipExtensions)}");
        await writer.WriteLineAsync("```");
        await writer.WriteLineAsync();
    }

    private async Task ProcessFileDiff(string originalFile, string modifiedFile, string relativePath, StreamWriter writer)
    {
        var fileInfo = new FileInfo(originalFile);
        if (fileInfo.Length > _diffConfig.MaxFileSize)
        {
            await writer.WriteLineAsync($"## 📄 {relativePath}");
            await writer.WriteLineAsync("```diff");
            await writer.WriteLineAsync($"! File too large to diff (>{_diffConfig.MaxFileSize / 1024 / 1024}MB)");
            await writer.WriteLineAsync("```");
            await writer.WriteLineAsync();
            return;
        }

        var originalText = await File.ReadAllTextAsync(originalFile);
        var modifiedText = await File.ReadAllTextAsync(modifiedFile);

        var diffResult = _diffBuilder.BuildDiffModel(originalText, modifiedText);

        // Skip if no changes
        if (!HasMeaningfulChanges(diffResult))
        {
            return;
        }

        var formatter = _formatters[_diffConfig.OutputFormat];
        await formatter.FormatDiff(writer, diffResult, relativePath, _diffConfig);
    }

    private static bool HasMeaningfulChanges(SideBySideDiffModel diffResult)
    {
        static bool HasChanges(DiffPaneModel model) => model.Lines.Any(l => IsChangedLine(l));
        return HasChanges(diffResult.OldText) || HasChanges(diffResult.NewText);
    }

    private static bool IsChangedLine(DiffPiece? line) =>
        line?.Type == ChangeType.Deleted ||
        line?.Type == ChangeType.Inserted ||
        line?.Type == ChangeType.Modified;
}