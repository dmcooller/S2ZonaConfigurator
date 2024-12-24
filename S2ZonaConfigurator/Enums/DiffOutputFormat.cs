namespace S2ZonaConfigurator.Enums;

public enum DiffOutputFormat
{
    /// <summary>
    /// GitHub-style markdown diff (default)
    /// Shows changes with +/- prefixes and code blocks
    /// </summary>
    GitHubMarkdown,

    /// <summary>
    /// Traditional unified diff format
    /// Standard patch format with @@ hunks
    /// </summary>
    Unified,

    /// <summary>
    /// Side by side diff format in markdown
    /// Shows old and new versions in a two-column table
    /// </summary>
    SideBySideMarkdown,

    /// <summary>
    /// HTML format for viewing in browsers
    /// Colored diff with syntax highlighting
    /// </summary>
    Html
}