using S2ZonaConfigurator.Enums;

namespace S2ZonaConfigurator.Models;

public class DiffConfig
{
    /// <summary>
    /// Output format for the diff (default: GitHubMarkdown)
    /// </summary>
    public DiffOutputFormat OutputFormat { get; set; } = DiffOutputFormat.GitHubMarkdown;

    /// <summary>
    /// Maximum file size in bytes to process for diff (default: 10MB)
    /// </summary>
    public int MaxFileSize { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Number of context lines to show around changes (default: 3)
    /// </summary>
    public int ContextLines { get; set; } = 3;

    /// <summary>
    /// File extensions to skip when diffing (default: [".dll", ".exe", ".pdb"])
    /// </summary>
    public HashSet<string> SkipExtensions { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ".dll",
        ".exe",
        ".pdb"
    };
}